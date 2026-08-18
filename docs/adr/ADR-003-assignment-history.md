# ADR-003: Assignment History Design (RISK-03)

**Date**: 2026-08-17  
**Status**: Accepted  
**Addresses Risk**: RISK-03 — Assignment history lost when reassigning

---

## Context

When a ticket is reassigned from Agent A to Agent B, the business requires that the historical record of Agent A's assignment is preserved. This supports the workload reporting (who held a ticket and for how long) and the audit trail (TICKET-S17).

## Problem

The simplest implementation would be to update `Ticket.AssignedAgentId` in place — but this silently erases the prior assignment, violating TICKET-R08.

## Options Considered

| Option | Description | Trade-off |
|---|---|---|
| **A) FK only on Ticket** | `Ticket.AssignedAgentId` updated in place on each assignment | Fast to implement; loses history |
| **B) `TicketAssignment` entity with `IsActive` flag** | New row per assignment; old row gets `EndedAt` timestamp; only one row with `IsActive = true` at a time | Full history; slightly more complex assignment query |
| **C) `TicketAssignment` + soft-delete** | Similar to B but uses `IsDeleted` flag instead of `EndedAt` | `EndedAt` is more semantically meaningful for time-boxed assignments |

## Decision

**Option B — `TicketAssignment` entity with `IsActive` flag and `EndedAt` timestamp.**

## Rationale

- `TicketAssignment` is a relationship entity that **carries business data** (`AssignedAt`, `EndedAt`, `AssignedByUserId`, `AssignmentNote`). This makes a join-table entity the correct design.
- `EndedAt` is semantically correct: we want to know *when* an assignment ended, not just that it was deleted.
- The filtered unique index `UNIQUE (TicketId) WHERE IsActive = 1` (SQL Server: partial index) enforces the one-active constraint at the database level even if the application check fails (RISK-03 mitigation at DB layer).
- Reporting queries can ask "how long did each agent hold this ticket?" using `EndedAt - AssignedAt`.

## Consequences

- Every `AssignTicketCommand` and `ReassignTicketCommand` must be wrapped in a transaction that:
  1. Fetches the current active assignment
  2. Sets `EndedAt` and `IsActive = false`
  3. Inserts a new assignment row
  4. All in one `SaveChangesAsync` call
- The "current assignee" query is `Ticket.TicketAssignments.Single(a => a.IsActive)` or via the denormalized `Ticket.AssignedAgentId` FK (both are maintained).
