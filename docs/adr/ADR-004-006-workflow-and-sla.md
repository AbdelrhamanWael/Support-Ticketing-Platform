# ADR-004: Status Transition Enforcement (RISK-04)

**Date**: 2026-08-17  
**Status**: Accepted  
**Addresses Risk**: RISK-04 — Invalid status changes bypass workflow

---

## Context

Ticket status transitions must follow a defined state machine. Allowing arbitrary status changes would corrupt business metrics (SLA, resolution time), violate role-based access rules, and undermine the audit trail.

## Decision

**A static `TicketStatusTransitionValidator` class in the Domain layer** acts as the single authority for transition validity.

```csharp
// Domain/Workflow/TicketStatusTransitionValidator.cs
public static class TicketStatusTransitionValidator
{
    private static readonly Dictionary<(TicketStatus, TicketStatus), string[]> _allowed = ...;

    public static bool IsAllowed(TicketStatus from, TicketStatus to, string role) { ... }
}
```

**All `ChangeTicketStatusCommand` handlers call this validator before any database write.** A failed validation returns `Result.Failure("Invalid transition", ErrorType.Conflict)` which the controller maps to `409 Conflict`.

## Rationale

- Placing this logic in Domain (not Application or API) means it is reachable without any Infrastructure dependencies — fully unit testable.
- The validator is the **single source of truth** for allowed transitions, preventing different handlers from implementing conflicting logic.
- TEST-08 specifically covers the `New → Closed` invalid transition to catch regressions.

## Consequences

- Adding a new allowed transition requires updating `TicketStatusTransitionValidator` only — one place.
- The unit test suite for the validator becomes the living specification of the state machine.

---

# ADR-005: Resolution Time Metric Definition (RISK-05)

**Date**: 2026-08-17  
**Status**: Accepted  
**Addresses Risk**: RISK-05 — Resolution-time metric calculated inconsistently

---

## Context

The `GetResolutionTimeReport` must define its metric unambiguously so that different team members compute the same number.

## Decision

**Resolution Time = `ResolvedAt - CreatedAt` in minutes.**

| Requirement | Value |
|---|---|
| **Formula** | `ResolvedAt - CreatedAt` |
| **Unit** | Minutes |
| **Inclusion** | Tickets where `ResolvedAt IS NOT NULL` AND `Status IN ('Resolved', 'Closed')` |
| **Exclusion** | `Status = Cancelled` (TICKET-R18); tickets missing `ResolvedAt` |
| **Statistics** | Average and Median per agent, per category |

## Rationale

- `ResolvedAt` is a server-set timestamp (TICKET-R14), so it is reliable.
- Starting the clock at `CreatedAt` (not `StartedAt`) measures total customer waiting time — the metric the business cares about.
- Cancelled tickets are explicitly excluded (TICKET-R18) to avoid skewing averages with artificially short "resolutions."

## Consequences

- `GetResolutionTimeReportQuery` must always filter `Status != Cancelled` and `ResolvedAt != null` before computing metrics.
- The report endpoint documentation clearly states this definition.

---

# ADR-006: SLA Calculation Strategy (RISK-06)

**Date**: 2026-08-17  
**Status**: Accepted  
**Addresses Risk**: RISK-06 — SLA target calculation ignores business-hour policy

---

## Context

SLA targets need to be calculated when a ticket is created. The full implementation would consider business hours (e.g., 9 AM–5 PM Mon–Fri). However, this adds significant complexity and is listed as a stretch goal.

## Decision

**v1.0: Wall-clock SLA calculation.** `SlaResponseDueAt = CreatedAt + ResponseTargetMinutes`. Business-hour SLA is deferred to a stretch sprint.

The `ISlaCalculationService` interface abstracts this so the implementation can be swapped:

```csharp
public interface ISlaCalculationService
{
    (DateTime ResponseDue, DateTime ResolutionDue) Calculate(
        DateTime createdAt, SlaPolicy policy);
}
```

v1.0 implementation: `WallClockSlaCalculationService`  
Stretch implementation: `BusinessHourSlaCalculationService`

## Consequences

- The interface is defined now; swapping the implementation in a future sprint requires no changes to any handler.
- The API response and reports clearly label SLA dates as "calendar time" so customers and admins are not misled.
- The risk (RISK-06) is documented and tracked in Jira as a known limitation until the stretch sprint delivers business-hour support.
