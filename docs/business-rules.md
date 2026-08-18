# Business Rules Catalog — Support Ticketing Platform

> This document is the authoritative reference for all mandatory business rules.
> Every rule lists its **enforcement point** (where in the codebase it is checked) and the **negative test** that verifies it.

---

## Rule Reference Table

| ID | Rule Statement | Category |
|---|---|---|
| TICKET-R01 | Customer can access only own tickets | Access Control |
| TICKET-R02 | Agent can access only assigned tickets unless elevated scope | Access Control |
| TICKET-R03 | Internal notes are never returned to customer endpoints | Data Visibility |
| TICKET-R04 | Ticket title/description required within configured lengths | Validation |
| TICKET-R05 | Priority uses controlled values | Validation |
| TICKET-R06 | New ticket receives initial status automatically | Workflow |
| TICKET-R07 | Assignment requires active agent | Assignment |
| TICKET-R08 | Assignment/reassignment is historical, not silent overwrite | Assignment |
| TICKET-R09 | Invalid status transitions are rejected | Workflow |
| TICKET-R10 | Closed ticket cannot accept normal new comments unless reopened | Conversation |
| TICKET-R11 | Customer cannot add internal note | Data Visibility |
| TICKET-R12 | Agent cannot work ticket assigned to another agent without policy | Access Control |
| TICKET-R13 | High priority tickets sort before lower priority in default queue | Ordering |
| TICKET-R14 | ResolvedAt/ClosedAt timestamps are server-controlled | Integrity |
| TICKET-R15 | Reopen behavior is role-controlled and creates history | Workflow |
| TICKET-R16 | Ticket cancellation allowed only in early states | Workflow |
| TICKET-R17 | SLA dates are server-calculated from priority/policy | SLA |
| TICKET-R18 | Reports exclude cancelled tickets where metric requires | Reporting |
| TICKET-R19 | Customer-facing query must strip internal staff-only fields | Data Visibility |
| TICKET-R20 | Attachment metadata cannot reference another customer's ticket | Integrity |
| TICKET-R21 | Audit captures changes without leaking comment contents | Audit |
| TICKET-R22 | Inactive agent cannot receive new assignment | Assignment |
| TICKET-R23 | Deleting customer/agent never destroys historical ticket identity | Integrity |
| TICKET-R24 | Current-user endpoints do not accept arbitrary customer/agent IDs | Security |

---

## Detailed Rule Specifications

---

### TICKET-R01: Customer Ticket Isolation

**Statement**: A customer can only read, comment on, or cancel their own tickets.

**Enforcement Points**:
- `GetMyCustomerTicketsQuery`: filters by `CustomerId == currentUser.ProfileId`
- `GetTicketDetailsQuery`: checks `ticket.CustomerId == currentUser.ProfileId` before returning
- `AddPublicCommentCommand`: verifies ticket ownership
- `CancelTicketCommand`: verifies ticket ownership

**Code Pattern**:
```csharp
if (ticket.CustomerId != _currentUserService.CustomerProfileId)
    return Result.Failure("Access denied", ErrorType.Forbidden);
```

**Negative Test**: `TEST-05` — Customer requests `GET /api/tickets/{otherId}` → expects `403 Forbidden`

---

### TICKET-R02: Agent Scope Restriction

**Statement**: A support agent may only access (view, comment on, update status of) tickets explicitly assigned to them via an active assignment. SupportLead and Admin have broader scope.

**Enforcement Points**:
- `GetMyAgentQueueQuery`: filters by active assignment to `currentUser.AgentProfileId`
- `AddPublicCommentCommand` (agent path): checks active assignment
- `ChangeTicketStatusCommand`: verifies assignment before allowing status change

**Negative Test**: `TEST-06` — Agent requests a ticket assigned to a different agent → expects `403 Forbidden`

---

### TICKET-R03 & TICKET-R19: Internal Note Visibility

**Statement**: Internal notes (`CommentVisibility.Internal`) are **never** included in any response sent to a Customer role. Staff-only fields on Ticket are also excluded.

**Enforcement Points**:
- `GetTicketConversationQuery`: applies `.Where(c => c.Visibility == CommentVisibility.Public)` for Customer role
- `GetTicketDetailsQuery`: uses separate `CustomerTicketDto` (no `InternalNotes`, `AgentProfileId`) vs `StaffTicketDto`
- AutoMapper profile: separate mappings by role

**Negative Test**: `TEST-11` — Customer requests `GET /api/tickets/{id}/comments` → response must contain zero internal notes

---

### TICKET-R04: Title/Description Validation

**Statement**: Ticket title must be 5–200 characters. Description must be 10–5000 characters. Both are required.

**Enforcement Points**:
- `CreateTicketCommandValidator` (FluentValidation):
```csharp
RuleFor(x => x.Title)
    .NotEmpty().WithMessage("Title is required.")
    .Length(5, 200).WithMessage("Title must be between 5 and 200 characters.");

RuleFor(x => x.Description)
    .NotEmpty().WithMessage("Description is required.")
    .Length(10, 5000).WithMessage("Description must be between 10 and 5000 characters.");
```

---

### TICKET-R05: Controlled Priority Values

**Statement**: Priority must be one of the controlled enum values: `Low`, `Medium`, `High`, `Critical`.

**Enforcement Points**:
- `TicketPriority` enum in Domain
- `CreateTicketCommandValidator`: validates against enum
- `SetTicketPriorityCommand`: validates against enum

---

### TICKET-R06: Automatic Initial Status

**Statement**: When a ticket is created, its status is automatically set to `New`. The client cannot specify the initial status.

**Enforcement Points**:
- `CreateTicketCommandHandler`:
```csharp
var ticket = new Ticket { Status = TicketStatus.New, ... };
```
- The `status` field is not present in `CreateTicketRequest`

---

### TICKET-R07 & TICKET-R22: Active Agent Requirement

**Statement**: A ticket may only be assigned to an `AgentProfile` where `IsActive == true`.

**Enforcement Points**:
- `AssignTicketCommandHandler`:
```csharp
var agent = await _agentRepository.GetByIdAsync(command.AgentId);
if (agent == null || !agent.IsActive)
    return Result.Failure("Agent is not active.", ErrorType.BadRequest);
```
- `ReassignTicketCommandHandler`: same check

**Negative Test**: `TEST-10` — Assign to `IsActive = false` agent → expects `400 Bad Request`

---

### TICKET-R08: Assignment History Preservation

**Statement**: Every assignment event (new assignment or reassignment) creates a new `TicketAssignment` record. The previous active assignment is ended (not deleted).

**Enforcement Points**:
- `AssignTicketCommandHandler`: inserts new `TicketAssignment` row
- `ReassignTicketCommandHandler`:
```csharp
var currentAssignment = await _assignmentRepository.GetActiveAsync(ticketId);
if (currentAssignment != null)
{
    currentAssignment.EndedAt = DateTime.UtcNow;
    currentAssignment.IsActive = false;
}
var newAssignment = new TicketAssignment { ... IsActive = true };
await _assignmentRepository.AddAsync(newAssignment);
```

---

### TICKET-R09: Status Transition Enforcement

**Statement**: Only documented status transitions are allowed. Any other transition is rejected with a `409 Conflict`.

**Allowed Transitions**:
```
New        → Assigned   (system, on assignment)
New        → Cancelled  (Customer/Admin)
Assigned   → InProgress (Agent assigned/Lead/Admin)
InProgress → Resolved   (Agent assigned/Lead/Admin)
Resolved   → Closed     (Customer/Lead/Admin)
Closed     → Reopened   (Lead/Admin; Customer if policy)
Reopened   → InProgress (Agent assigned/Lead/Admin)
```

**Enforcement Points**:
- `TicketStatusTransitionValidator` static class in Domain:
```csharp
public static bool IsValidTransition(TicketStatus from, TicketStatus to, string role)
{
    return _allowedTransitions.TryGetValue((from, to), out var allowedRoles)
        && allowedRoles.Contains(role);
}
```

**Negative Test**: `TEST-08` — `PUT /api/tickets/{id}/status` with `newStatus = Closed` while current status is `New` → expects `409 Conflict`

---

### TICKET-R10: No Comments on Closed Tickets

**Statement**: A closed ticket cannot receive new comments unless it has been reopened.

**Enforcement Points**:
- `AddPublicCommentCommand`:
```csharp
if (ticket.Status == TicketStatus.Closed)
    return Result.Failure("Cannot add comments to a closed ticket.", ErrorType.Conflict);
```

**Negative Test**: `TEST-07` — Comment on a closed ticket → expects `409 Conflict`

---

### TICKET-R11: Customer Cannot Post Internal Notes

**Statement**: Only `SupportAgent`, `SupportLead`, and `Admin` roles may create internal notes.

**Enforcement Points**:
- `AddInternalNoteCommand` controller: `[Authorize(Roles = "SupportAgent,SupportLead,Admin")]`
- Route is `/api/tickets/{id}/internal-notes` — not accessible to Customer role

**Negative Test**: `TEST-09` — Customer sends `POST /api/tickets/{id}/internal-notes` → expects `403 Forbidden`

---

### TICKET-R13: Priority-Based Queue Ordering

**Statement**: In the default agent queue, `Critical` tickets appear before `High`, before `Medium`, before `Low`.

**Enforcement Points**:
- `GetMyAgentQueueQueryHandler`:
```csharp
.OrderByDescending(t => t.Priority)   // Enum ordered Critical=3, High=2, Medium=1, Low=0
.ThenBy(t => t.CreatedAt)
```

---

### TICKET-R14: Server-Controlled Timestamps

**Statement**: `StartedAt`, `ResolvedAt`, and `ClosedAt` are set exclusively by the server at the correct state transition. Clients cannot supply these values.

**Enforcement Points**:
- `ChangeTicketStatusCommandHandler`:
```csharp
if (command.NewStatus == TicketStatus.InProgress && ticket.StartedAt == null)
    ticket.StartedAt = DateTime.UtcNow;
if (command.NewStatus == TicketStatus.Resolved)
    ticket.ResolvedAt = DateTime.UtcNow;
if (command.NewStatus == TicketStatus.Closed)
    ticket.ClosedAt = DateTime.UtcNow;
```
- No DTO property accepts these timestamps from client requests

---

### TICKET-R15: Role-Controlled Reopen

**Statement**: Reopening a ticket creates a `TicketStatusHistory` entry. The default policy allows only `SupportLead` and `Admin` to reopen. If `SlaPolicy.AllowCustomerReopen = true`, customers may also reopen.

**Enforcement Points**:
- `ReopenTicketCommandHandler`: checks role and optional policy flag

**Negative Test**: `TEST-12` — Customer attempts reopen when policy is staff-only → expects `403 Forbidden`

---

### TICKET-R16: Cancellation Allowed in Early States Only

**Statement**: A customer may cancel a ticket only when it is in `New` or `Assigned` status. Cancellation from `InProgress` or later is rejected.

**Enforcement Points**:
- `CancelTicketCommandHandler`:
```csharp
var allowedStatuses = new[] { TicketStatus.New, TicketStatus.Assigned };
if (!allowedStatuses.Contains(ticket.Status))
    return Result.Failure("Ticket cannot be cancelled at this stage.", ErrorType.Conflict);
```

---

### TICKET-R17: Server-Calculated SLA Dates

**Statement**: `SlaResponseDueAt` and `SlaResolutionDueAt` are calculated by the server using `CreatedAt + SlaPolicy.ResponseTargetMinutes`. Clients cannot set SLA dates.

**Enforcement Points**:
- `CreateTicketCommandHandler` calls `ISlaCalculationService.CalculateTargets(ticket, policy)`
- No client DTO field for SLA dates

---

### TICKET-R18: Reports Exclude Cancelled Tickets

**Statement**: Business metrics (resolution time, workload, backlog counts) exclude tickets with `Status = Cancelled` unless explicitly asked for cancelled statistics.

**Enforcement Points**:
- All report query handlers apply `.Where(t => t.Status != TicketStatus.Cancelled)` by default

---

### TICKET-R21: Audit Without Comment Bodies

**Statement**: `ActivityLog` records assignment changes, status transitions, and priority changes, but does **not** store comment body text.

**Enforcement Points**:
- `ActivityLog` entity stores `ActionType`, `OldValues` (JSON of field changes), `NewValues` only
- Comments are stored separately in `TicketComment` — not duplicated in activity log

---

### TICKET-R23: Historical Identity Preservation

**Statement**: Soft-deleting or deactivating a `CustomerProfile` or `AgentProfile` must not destroy associated ticket records, comments, or assignment history.

**Enforcement Points**:
- `ApplicationUser` deletion: sets `IsDeleted = true`, does **not** cascade to tickets
- `AgentProfile`: `IsActive = false` rather than delete; FK in `Ticket.AssignedAgentId` retains value
- EF Core delete behavior on these relationships: `Restrict`

---

### TICKET-R24: No Arbitrary User ID in Current-User Endpoints

**Statement**: Endpoints prefixed with `/me/` (e.g., `/api/customers/me/tickets`, `/api/agents/me/tickets`) derive the user identity from the JWT token only. The request body or query string must not accept a `customerId` or `agentId` parameter.

**Enforcement Points**:
- `GetMyCustomerTicketsQuery` has no `CustomerId` field — handler reads from `ICurrentUserService`
- `GetMyAgentQueueQuery` has no `AgentId` field — handler reads from `ICurrentUserService`

---

*TechMaster Academy | Phase 05 Capstone — Business Rules v1.0*
