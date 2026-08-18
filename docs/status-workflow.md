# Status & Workflow Specification — Support Ticketing Platform

## 1. Ticket Status State Machine

```
                    ┌──────────────────────────────────┐
                    │                                  │
              ╔═════╧══════╗                           │
              ║    NEW     ║ ←── Ticket Created         │
              ╚═════╤══════╝                           │
                    │                                  │
           ┌────────┼────────┐                         │
           │                 │                         │
           ▼                 ▼                         │
    ╔════════════╗    ╔═════════════╗                  │
    ║ CANCELLED  ║    ║  ASSIGNED   ║ ←── Lead assigns  │
    ╚════════════╝    ╚══════╤══════╝                  │
   (terminal)               │                          │
                             ▼                          │
                    ╔═════════════════╗                 │
                    ║   IN PROGRESS   ║ ←── Agent starts │
                    ╚════════╤════════╝                 │
                             │                          │
                             ▼                          │
                    ╔════════════════╗                  │
                    ║   RESOLVED     ║ ←── Agent resolves│
                    ╚════════╤═══════╝                  │
                             │                          │
                             ▼                          │
                    ╔════════════════╗                  │
                    ║    CLOSED      ║ ←── Customer/Lead │
                    ╚════════╤═══════╝   confirms close  │
                             │                          │
                             ▼                          │
                    ╔════════════════╗                  │
                    ║   REOPENED     ╠──────────────────┘
                    ╚════════════════╝
                    Returns to ASSIGNED or IN PROGRESS
                    by policy (see Section 3)
```

---

## 2. Allowed Transitions Reference

| From | To | Allowed Actor | Trigger |
|---|---|---|---|
| `New` | `Assigned` | System (auto, on assignment) | `AssignTicketCommand` |
| `New` | `Cancelled` | `Customer` (own), `Admin` | `CancelTicketCommand` |
| `Assigned` | `InProgress` | `SupportAgent` (assigned), `SupportLead`, `Admin` | `ChangeTicketStatusCommand` |
| `Assigned` | `Cancelled` | `Admin` | `CancelTicketCommand` |
| `InProgress` | `Resolved` | `SupportAgent` (assigned), `SupportLead`, `Admin` | `ChangeTicketStatusCommand` |
| `Resolved` | `Closed` | `Customer` (own), `SupportLead`, `Admin` | `ChangeTicketStatusCommand` |
| `Resolved` | `InProgress` | `SupportLead`, `Admin` | `ChangeTicketStatusCommand` |
| `Closed` | `Reopened` | `SupportLead`, `Admin` (Customer if policy) | `ReopenTicketCommand` |
| `Reopened` | `InProgress` | `SupportAgent` (assigned), `SupportLead`, `Admin` | `ChangeTicketStatusCommand` |
| `Reopened` | `Assigned` | System (on reassignment after reopen) | `ReassignTicketCommand` |

### Invalid (Rejected) Transitions

| Attempt | Reason |
|---|---|
| `New` → `Closed` | Must go through assignment and resolution stages |
| `New` → `Resolved` | Cannot resolve without working the ticket |
| `Assigned` → `Closed` | Must be resolved first |
| `Cancelled` → anything | Terminal state |
| `Closed` → anything except `Reopened` | Terminal unless explicitly reopened |
| Any status → `New` | `New` is only an initial state |

---

## 3. Reopen Policy

When a `Closed` ticket is reopened:

1. `Status` transitions to `Reopened`
2. A `TicketStatusHistory` entry is created (actor, timestamp, reason)
3. The **reassignment behavior** depends on team policy:

| Policy Option | Behavior |
|---|---|
| **Return to last agent** (default) | If the last `TicketAssignment` has an active agent, a new assignment row is created for them |
| **Unassign on reopen** | Ticket goes to `Reopened` with no agent — lead must reassign |
| **Configurable per category** | `TicketCategory.ReopenPolicy` field drives the behavior |

> **Document which policy your team chose in ADR-04.**

4. SLA timestamps: `SlaResolutionDueAt` may be recalculated from the reopen time, or the original target may be preserved (document in ADR-06).

---

## 4. Timestamp Rules

| Timestamp | Set When | By Whom | Nullable? |
|---|---|---|---|
| `CreatedAt` | Ticket created | Server | No |
| `UpdatedAt` | Any field changes | Server | No |
| `StartedAt` | First transition to `InProgress` | Server | Yes — `null` until first start |
| `ResolvedAt` | Transition to `Resolved` | Server | Yes |
| `ClosedAt` | Transition to `Closed` | Server | Yes |

> `StartedAt` is set **once only** — subsequent `Reopened → InProgress` transitions do not overwrite it. This preserves the original "time to first response" metric.

---

## 5. Assignment Status Workflow

```
              ╔══════════╗
              ║  ACTIVE  ║ ←── New assignment created
              ╚════╤═════╝
                   │
         ┌─────────┴─────────┐
         │                   │
         ▼                   ▼
   ╔═══════════╗      ╔════════════╗
   ║ REASSIGNED║      ║   ENDED    ║
   ║ (ended,   ║      ║ (closed or ║
   ║  new one  ║      ║  cancelled)║
   ║  created) ║      ╚════════════╝
   ╚═══════════╝
```

**One-Active Constraint**: At most one `TicketAssignment` row with `IsActive = true` per `TicketId` at any time. Enforced by application logic in a database transaction. The database may optionally use a **filtered unique index**: `UNIQUE (TicketId) WHERE IsActive = 1`.

---

## 6. SLA Status Workflow

```
              ╔══════════╗
              ║ ON TRACK ║ ←── Ticket created with SLA policy
              ╚═════╤════╝
                    │
              (AtRisk threshold reached)
                    │
                    ▼
              ╔══════════╗
              ║  AT RISK ║
              ╚═════╤════╝
                    │
              (Resolution due passed)
                    │
                    ▼
              ╔══════════╗
              ║ BREACHED ║
              ╚══════════╝
```

**SLA Status Derivation Rules**:

```
AtRisk threshold = SlaResolutionDueAt - (0.30 × total resolution window)

if UtcNow >= SlaResolutionDueAt          → SlaStatus = Breached
else if UtcNow >= AtRisk threshold       → SlaStatus = AtRisk
else                                     → SlaStatus = OnTrack
```

**Update Timing**: SLA status is re-evaluated:
- On each status transition (in the `ChangeTicketStatusCommandHandler`)
- By a background job running every 15 minutes (stretch: Hangfire or hosted service)
- At query time for the `GetSlaRiskReportQuery`

> **Note**: SLA state is **derived** — it is never set directly by a client or agent.

---

## 7. Status Transition Validator (Domain Code)

```csharp
public static class TicketStatusTransitionValidator
{
    private static readonly Dictionary<(TicketStatus From, TicketStatus To), string[]> _allowed = new()
    {
        [(TicketStatus.New, TicketStatus.Assigned)]     = ["System"],
        [(TicketStatus.New, TicketStatus.Cancelled)]    = ["Customer", "Admin"],
        [(TicketStatus.Assigned, TicketStatus.InProgress)]  = ["SupportAgent", "SupportLead", "Admin"],
        [(TicketStatus.Assigned, TicketStatus.Cancelled)]   = ["Admin"],
        [(TicketStatus.InProgress, TicketStatus.Resolved)]  = ["SupportAgent", "SupportLead", "Admin"],
        [(TicketStatus.Resolved, TicketStatus.Closed)]      = ["Customer", "SupportLead", "Admin"],
        [(TicketStatus.Resolved, TicketStatus.InProgress)]  = ["SupportLead", "Admin"],
        [(TicketStatus.Closed, TicketStatus.Reopened)]      = ["SupportLead", "Admin"], // Customer conditionally
        [(TicketStatus.Reopened, TicketStatus.InProgress)]  = ["SupportAgent", "SupportLead", "Admin"],
        [(TicketStatus.Reopened, TicketStatus.Assigned)]    = ["System"],
    };

    public static bool IsAllowed(TicketStatus from, TicketStatus to, string role)
    {
        return _allowed.TryGetValue((from, to), out var roles) && roles.Contains(role);
    }
}
```

---

## 8. History Entry Requirements

A `TicketStatusHistory` row must be created for **every** status change, including:

| Event | FromStatus | ToStatus | Note Required? |
|---|---|---|---|
| Initial creation | — | `New` | No |
| Assignment | `New` | `Assigned` | Optional |
| Start work | `Assigned` | `InProgress` | Optional |
| Resolve | `InProgress` | `Resolved` | **Yes** — `ResolutionNote` required |
| Close | `Resolved` | `Closed` | Optional |
| Cancel | `New/Assigned` | `Cancelled` | **Yes** — `CancellationReason` required |
| Reopen | `Closed` | `Reopened` | **Yes** — reason required |

---

*TechMaster Academy | Phase 05 Capstone — Status Workflow v1.0*
