# SLA Design — Support Ticketing Platform

## 1. Overview

The SLA (Service Level Agreement) subsystem tracks two time-based targets for each ticket:

| Target | Description |
|---|---|
| **Response Target** | Maximum time from ticket creation to first agent response/assignment |
| **Resolution Target** | Maximum time from ticket creation to resolution |

SLA targets are **server-calculated** (TICKET-R17) and **never freely set by a client**.

---

## 2. SlaPolicy Entity

```csharp
public class SlaPolicy
{
    public int Id { get; set; }
    public int? TicketCategoryId { get; set; }   // null = applies to all categories
    public TicketPriority Priority { get; set; }
    public int ResponseTargetMinutes { get; set; }
    public int ResolutionTargetMinutes { get; set; }
    public bool IsActive { get; set; }

    // Navigation
    public TicketCategory? Category { get; set; }
}
```

### Example SLA Matrix

| Priority | Response Target | Resolution Target |
|---|---|---|
| Critical | 30 minutes | 4 hours (240 min) |
| High | 2 hours (120 min) | 8 hours (480 min) |
| Medium | 4 hours (240 min) | 24 hours (1440 min) |
| Low | 8 hours (480 min) | 72 hours (4320 min) |

---

## 3. SLA Calculation on Ticket Creation

When `CreateTicketCommandHandler` runs:

```csharp
// 1. Find matching SLA policy (category-specific first, then fallback to global)
var policy = await _context.SlaPolicies
    .Where(p => p.IsActive
             && p.Priority == ticket.Priority
             && (p.TicketCategoryId == ticket.CategoryId || p.TicketCategoryId == null))
    .OrderByDescending(p => p.TicketCategoryId)  // category-specific wins
    .FirstOrDefaultAsync();

// 2. Calculate due dates (v1.0: wall-clock)
if (policy != null)
{
    ticket.SlaResponseDueAt    = ticket.CreatedAt.AddMinutes(policy.ResponseTargetMinutes);
    ticket.SlaResolutionDueAt  = ticket.CreatedAt.AddMinutes(policy.ResolutionTargetMinutes);
    ticket.SlaStatus           = SlaStatus.OnTrack;
}
// else: no SLA policy for this priority/category — fields remain null
```

---

## 4. SLA Status Derivation

SLA status is **derived**, not stored as a client-settable field.

### Thresholds

```
AtRisk threshold = SlaResolutionDueAt - (0.30 × total resolution window)

Total window = ResolutionTargetMinutes
AtRisk at    = SlaResolutionDueAt - (0.30 × ResolutionTargetMinutes) minutes

Example (High priority, 480 min window):
  AtRisk threshold = SlaResolutionDueAt - 144 minutes
  = ticket will show AtRisk when less than 144 minutes remain
```

### Derivation Code

```csharp
public static SlaStatus DeriveStatus(DateTime? dueAt, int? totalWindowMinutes, DateTime now)
{
    if (dueAt == null) return SlaStatus.OnTrack;  // No SLA policy
    if (now >= dueAt) return SlaStatus.Breached;

    if (totalWindowMinutes.HasValue)
    {
        var atRiskThreshold = dueAt.Value.AddMinutes(-(totalWindowMinutes.Value * 0.30));
        if (now >= atRiskThreshold) return SlaStatus.AtRisk;
    }

    return SlaStatus.OnTrack;
}
```

---

## 5. SLA Status Update Points

`SlaStatus` on `Ticket` is updated at:

| Event | Update Behavior |
|---|---|
| **Status transition** | Re-derive `SlaStatus` in `ChangeTicketStatusCommandHandler` |
| **SLA Risk Report query** | Always computed fresh from `UtcNow` vs `SlaResolutionDueAt` |
| **Background job** (stretch) | Hosted service runs every 15 minutes and stamps `Breached` tickets |

---

## 6. GetSlaRiskReport — Query Logic

```csharp
// GetSlaRiskReportQueryHandler
var now = DateTime.UtcNow;

var tickets = await _context.Tickets
    .Where(t => t.SlaResolutionDueAt != null
             && t.Status != TicketStatus.Closed
             && t.Status != TicketStatus.Cancelled
             && t.Status != TicketStatus.Resolved)
    .Select(t => new SlaRiskTicketDto
    {
        TicketId         = t.Id,
        Title            = t.Title,
        Priority         = t.Priority,
        SlaResolutionDue = t.SlaResolutionDueAt!.Value,
        MinutesRemaining = (int)(t.SlaResolutionDueAt!.Value - now).TotalMinutes,
        SlaStatus        = now >= t.SlaResolutionDueAt ? SlaStatus.Breached
                         : now >= t.SlaResolutionDueAt.Value.AddMinutes(-(t.SlaPolicy.ResolutionTargetMinutes * 0.30))
                           ? SlaStatus.AtRisk : SlaStatus.OnTrack,
        AssignedAgent    = t.ActiveAssignment != null ? t.ActiveAssignment.Agent.DisplayName : "Unassigned"
    })
    .Where(dto => dto.SlaStatus == SlaStatus.AtRisk || dto.SlaStatus == SlaStatus.Breached)
    .OrderBy(dto => dto.SlaResolutionDue)
    .ToListAsync();
```

---

## 7. Business Questions Answered

| Question | Answer Source |
|---|---|
| Which tickets are approaching SLA? | `GetSlaRiskReportQuery` filtering `AtRisk` and `Breached` |
| What was the SLA status at time of close? | `Ticket.SlaStatus` stamped at close event |
| Which priority has the most SLA breaches? | `GetTicketsByStatusReport` cross-referenced with `SlaStatus` |
| What is actual response time vs target? | `StartedAt - CreatedAt` vs `SlaResponseDueAt - CreatedAt` |

---

## 8. Stretch: Business-Hour SLA

**Deferred to stretch sprint** (see ADR-006). The `ISlaCalculationService` interface is already designed to support this swap:

```csharp
// Stretch implementation sketch:
public class BusinessHourSlaCalculationService : ISlaCalculationService
{
    // Skip non-working hours (weekends + 18:00–09:00)
    // Track "business minutes elapsed" instead of wall-clock minutes
    // Uses a configured timezone (e.g., "Eastern Standard Time")
}
```

---

*TechMaster Academy | Phase 05 Capstone — SLA Design v1.0*
