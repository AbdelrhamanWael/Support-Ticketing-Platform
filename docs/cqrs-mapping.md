# CQRS Command & Query Catalog — Support Ticketing Platform

> All commands and queries are dispatched via **MediatR**. Controllers are thin — they accept a request, build the command/query, call `_mediator.Send()`, and map the result to an HTTP response.

---

## Commands (State-Changing)

| # | Command | HTTP Endpoint | Role | Rules |
|---|---|---|---|---|
| 01 | `CreateTicketCommand` | `POST /api/tickets` | Customer | R04, R05, R06, R17, R24 |
| 02 | `CancelTicketCommand` | `DELETE /api/tickets/{id}/cancel` | Customer, Admin | R01, R16, R18 |
| 03 | `SetTicketPriorityCommand` | `PUT /api/tickets/{id}/priority` | SupportLead, Admin | R05, R21 |
| 04 | `AssignTicketCommand` | `PUT /api/tickets/{id}/assign` | SupportLead, Admin | R07, R08, R22 |
| 05 | `ReassignTicketCommand` | `PUT /api/tickets/{id}/reassign` | SupportLead, Admin | R07, R08, R22 |
| 06 | `AddPublicCommentCommand` | `POST /api/tickets/{id}/comments` | Customer, Agent, Lead, Admin | R01, R02, R10, R11 |
| 07 | `AddInternalNoteCommand` | `POST /api/tickets/{id}/internal-notes` | Agent, Lead, Admin | R03, R11, R12 |
| 08 | `ChangeTicketStatusCommand` | `PUT /api/tickets/{id}/status` | Role-based | R09, R10, R14, R15 |
| 09 | `ReopenTicketCommand` | `PUT /api/tickets/{id}/reopen` | Lead, Admin (Customer by policy) | R15 |
| 10 | `ConfigureSlaPolicyCommand` | `POST /api/admin/sla-policies` | Admin | R17 |

---

## Queries (Read-Only)

| # | Query | HTTP Endpoint | Role | Rules |
|---|---|---|---|---|
| 01 | `GetMyCustomerTicketsQuery` | `GET /api/customers/me/tickets` | Customer | R01, R19, R24 |
| 02 | `GetMyAgentQueueQuery` | `GET /api/agents/me/tickets` | SupportAgent | R02, R13, R24 |
| 03 | `SearchTicketsQuery` | `GET /api/tickets/search` | Lead, Admin | R18 |
| 04 | `GetTicketDetailsQuery` | `GET /api/tickets/{id}` | Customer (own), Agent (assigned), Lead, Admin | R01, R02, R03, R19 |
| 05 | `GetTicketConversationQuery` | `GET /api/tickets/{id}/comments` | Customer (own), Agent (assigned), Lead, Admin | R03, R19 |
| 06 | `GetTicketHistoryQuery` | `GET /api/tickets/{id}/history` | Customer (own), Lead, Admin | R01 |
| 07 | `GetUnassignedTicketsQuery` | `GET /api/admin/tickets/unassigned` | Lead, Admin | — |
| 08 | `GetTicketsByStatusReportQuery` | `GET /api/reports/tickets-by-status` | Admin | R18 |
| 09 | `GetAgentWorkloadReportQuery` | `GET /api/reports/agent-workload` | Lead, Admin | — |
| 10 | `GetHighPriorityOpenReportQuery` | `GET /api/reports/high-priority-open` | Lead, Admin | R13 |
| 11 | `GetResolutionTimeReportQuery` | `GET /api/reports/resolution-time` | Admin, Lead | R18 |
| 12 | `GetSlaRiskReportQuery` | `GET /api/reports/sla-risk` | Lead, Admin | R17 |
| 13 | `GetCustomerTicketHistoryQuery` | `GET /api/admin/customers/{id}/tickets` | Admin | R01 |

---

## Command Detail Specifications

### `CreateTicketCommand`

```csharp
public record CreateTicketCommand(
    string Title,
    string Description,
    int CategoryId,
    TicketPriority Priority
) : IRequest<Result<TicketDto>>;
```

**Handler Responsibilities**:
1. Validate title/description length (FluentValidation pipeline)
2. Verify `CategoryId` references an active category
3. Resolve `CustomerId` from `ICurrentUserService`
4. Set `Status = TicketStatus.New`
5. Calculate SLA due dates via `ISlaCalculationService`
6. Persist ticket and log activity
7. Return `TicketDto`

**Transaction**: Required (ticket insert + SLA calculation + activity log)

---

### `AssignTicketCommand`

```csharp
public record AssignTicketCommand(
    int TicketId,
    int AgentId,
    string? Note
) : IRequest<Result<AssignmentDto>>;
```

**Handler Responsibilities**:
1. Verify agent exists and `IsActive == true`
2. Check ticket exists and is not in a terminal status (`Closed`, `Cancelled`)
3. Fetch current active `TicketAssignment` if any — do **not** delete it
4. Create new `TicketAssignment` with `IsActive = true`
5. Update `Ticket.AssignedAgentId` and `Ticket.Status = Assigned`
6. Insert `TicketStatusHistory` for the status change
7. Log to `ActivityLog`

**Transaction**: Required — multiple tables in one atomic operation (RISK-03, RISK-04)

---

### `ReassignTicketCommand`

```csharp
public record ReassignTicketCommand(
    int TicketId,
    int NewAgentId,
    string ReassignmentReason
) : IRequest<Result<AssignmentDto>>;
```

**Handler Responsibilities**:
1. Same active agent check as `AssignTicketCommand`
2. End current assignment: set `EndedAt = UtcNow`, `IsActive = false`
3. Create new assignment
4. Update `Ticket.AssignedAgentId`
5. Optionally reset status to `Assigned` (document behavior in ADR-04)
6. Activity log entry

**Transaction**: Required

---

### `ChangeTicketStatusCommand`

```csharp
public record ChangeTicketStatusCommand(
    int TicketId,
    TicketStatus NewStatus,
    string? Reason
) : IRequest<Result<StatusChangeDto>>;
```

**Handler Responsibilities**:
1. Load ticket and current status
2. Validate transition via `TicketStatusTransitionValidator.IsValidTransition(from, to, role)`
3. Set server timestamps: `StartedAt`, `ResolvedAt`, `ClosedAt` as appropriate
4. Insert `TicketStatusHistory` row
5. Update `Ticket.Status`
6. If `Resolved` and `SlaResolutionDueAt` is set, derive final `SlaStatus`
7. Activity log

---

### `ReopenTicketCommand`

```csharp
public record ReopenTicketCommand(
    int TicketId,
    string Reason
) : IRequest<Result<StatusChangeDto>>;
```

**Handler Responsibilities**:
1. Verify ticket is in `Closed` status
2. Check actor role — Customer allowed only if `SlaPolicy.AllowCustomerReopen` (configurable)
3. Transition to `Reopened`
4. Insert `TicketStatusHistory`
5. Determine new assignment behavior (see `status-workflow.md`)

---

### `AddInternalNoteCommand`

```csharp
public record AddInternalNoteCommand(
    int TicketId,
    string Content
) : IRequest<Result<CommentDto>>;
```

**Handler Responsibilities**:
1. Role check: reject if role is `Customer`
2. Verify agent has an active assignment (or is Lead/Admin)
3. Create `TicketComment` with `Visibility = CommentVisibility.Internal`
4. **Never** expose in customer-facing queries

---

## Query Detail Specifications

### `GetMyAgentQueueQuery` (Projection Query)

This query is designed as a **database-level projection** — it does not load full entity graphs. It uses EF Core `Select()` + `ProjectTo<TicketDto>` to pull only the fields needed for the queue view.

```csharp
// Handler uses projection, NOT full entity load:
var results = await _context.Tickets
    .Where(t => t.ActiveAssignment.AgentId == _currentUser.AgentProfileId
             && t.Status != TicketStatus.Closed
             && t.Status != TicketStatus.Cancelled)
    .OrderByDescending(t => t.Priority)
    .ThenBy(t => t.SlaResolutionDueAt)
    .ProjectTo<AgentQueueTicketDto>(_mapper.ConfigurationProvider)
    .ToPagedListAsync(query.Page, query.PageSize);
```

**Why projection?**: Full entity load fetches all navigation properties (comments, attachments, history), causing N+1 problems and unnecessary data transfer. The queue view needs only: id, title, status, priority, SLA due date, customer name, created date.

---

### `GetTicketConversationQuery`

```csharp
public record GetTicketConversationQuery(
    int TicketId,
    bool IncludeInternal   // resolved from current user's role, not from request
) : IRequest<Result<ConversationDto>>;
```

The `IncludeInternal` flag is **set by the handler** based on the current user's role — it is **not** a parameter accepted from the HTTP request. This enforces TICKET-R03 structurally.

---

### `GetResolutionTimeReportQuery` — Metric Definition

**Metric**: Resolution time (minutes) = `ticket.ResolvedAt - ticket.CreatedAt`

**Inclusion criteria**:
- `ticket.ResolvedAt IS NOT NULL`
- `ticket.Status IN ('Resolved', 'Closed')`
- `ticket.Status != 'Cancelled'` (TICKET-R18)

**Statistical outputs**: Average and Median (use `PERCENTILE_CONT(0.5)` or sort/middle-value approach in LINQ)

---

### `GetSlaRiskReportQuery` — SLA State Logic

```
SlaStatus = OnTrack  when: now < SlaResolutionDueAt - (30% of total window)
SlaStatus = AtRisk   when: now >= SlaResolutionDueAt - (30% of total window)
SlaStatus = Breached when: now >= SlaResolutionDueAt
```

> **Server-only**: SLA status is derived and never freely set by a client. When calculated at report time, the query compares `UtcNow` against `SlaResolutionDueAt`. The `SlaStatus` column on `Ticket` may also be updated by a background job or on each status transition for real-time accuracy.

---

## MediatR Handler Template

```csharp
public class CreateTicketCommandHandler
    : IRequestHandler<CreateTicketCommand, Result<TicketDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISlaCalculationService _slaService;
    private readonly IMapper _mapper;

    public CreateTicketCommandHandler(
        IAppDbContext context,
        ICurrentUserService currentUser,
        ISlaCalculationService slaService,
        IMapper mapper)
    {
        _context = context;
        _currentUser = currentUser;
        _slaService = slaService;
        _mapper = mapper;
    }

    public async Task<Result<TicketDto>> Handle(
        CreateTicketCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Validate category
        var category = await _context.TicketCategories
            .FirstOrDefaultAsync(c => c.Id == command.CategoryId && c.IsActive, cancellationToken);
        if (category == null)
            return Result<TicketDto>.Failure("Category not found or inactive.", ErrorType.NotFound);

        // 2. Resolve customer profile
        var customerProfileId = _currentUser.CustomerProfileId
            ?? throw new UnauthorizedAccessException("No customer profile found.");

        // 3. Create ticket
        var ticket = new Ticket
        {
            Title = command.Title,
            Description = command.Description,
            CategoryId = command.CategoryId,
            CustomerId = customerProfileId,
            Priority = command.Priority,
            Status = TicketStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        // 4. Calculate SLA
        var slaPolicy = await _slaService.GetPolicyAsync(command.CategoryId, command.Priority);
        if (slaPolicy != null)
        {
            ticket.SlaResponseDueAt = ticket.CreatedAt.AddMinutes(slaPolicy.ResponseTargetMinutes);
            ticket.SlaResolutionDueAt = ticket.CreatedAt.AddMinutes(slaPolicy.ResolutionTargetMinutes);
            ticket.SlaStatus = SlaStatus.OnTrack;
        }

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<TicketDto>.Success(_mapper.Map<TicketDto>(ticket));
    }
}
```

---

*TechMaster Academy | Phase 05 Capstone — CQRS Mapping v1.0*
