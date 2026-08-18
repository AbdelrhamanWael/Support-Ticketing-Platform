# Architecture Guide — Support Ticketing Platform API

## 1. Overview

The solution is built on **Clean Architecture** (also called Onion Architecture), ensuring that business rules are completely independent of frameworks, databases, and UI concerns. Dependencies point **inward only** — outer layers depend on inner layers, never the reverse.

```
┌─────────────────────────────────────────────────────────────────┐
│                          API Layer                              │
│          Controllers · Middleware · Swagger · Program.cs        │
├─────────────────────────────────────────────────────────────────┤
│                    Infrastructure Layer                         │
│     EF Core · Repositories · Identity · Email · Serilog        │
├─────────────────────────────────────────────────────────────────┤
│                     Application Layer                           │
│      CQRS Handlers · DTOs · Validators · Interfaces            │
├─────────────────────────────────────────────────────────────────┤
│                       Domain Layer                              │
│      Entities · Enums · Domain Events · Value Objects          │
└─────────────────────────────────────────────────────────────────┘
          Dependency Direction: Outer → Inner ONLY
```

---

## 2. Project Structure

```
TechMasterCapstone.sln
│
├── src/
│   ├── TechMaster.Domain/
│   │   ├── Entities/
│   │   │   ├── ApplicationUser.cs
│   │   │   ├── CustomerProfile.cs
│   │   │   ├── AgentProfile.cs
│   │   │   ├── SupportTeam.cs
│   │   │   ├── Ticket.cs
│   │   │   ├── TicketCategory.cs
│   │   │   ├── TicketComment.cs
│   │   │   ├── TicketAttachmentMetadata.cs
│   │   │   ├── TicketAssignment.cs
│   │   │   ├── TicketStatusHistory.cs
│   │   │   ├── SlaPolicy.cs
│   │   │   ├── TicketTag.cs
│   │   │   └── ActivityLog.cs
│   │   ├── Enums/
│   │   │   ├── TicketStatus.cs
│   │   │   ├── TicketPriority.cs
│   │   │   ├── CommentVisibility.cs
│   │   │   └── SlaStatus.cs
│   │   └── Common/
│   │       └── BaseEntity.cs
│   │
│   ├── TechMaster.Application/
│   │   ├── Commands/
│   │   │   ├── CreateTicket/
│   │   │   │   ├── CreateTicketCommand.cs
│   │   │   │   ├── CreateTicketCommandHandler.cs
│   │   │   │   └── CreateTicketCommandValidator.cs
│   │   │   ├── CancelTicket/
│   │   │   ├── SetTicketPriority/
│   │   │   ├── AssignTicket/
│   │   │   ├── ReassignTicket/
│   │   │   ├── AddPublicComment/
│   │   │   ├── AddInternalNote/
│   │   │   ├── ChangeTicketStatus/
│   │   │   ├── ReopenTicket/
│   │   │   └── ConfigureSlaPolicy/
│   │   ├── Queries/
│   │   │   ├── GetMyCustomerTickets/
│   │   │   ├── GetMyAgentQueue/
│   │   │   ├── SearchTickets/
│   │   │   ├── GetTicketDetails/
│   │   │   ├── GetTicketConversation/
│   │   │   ├── GetTicketHistory/
│   │   │   ├── GetUnassignedTickets/
│   │   │   ├── GetTicketsByStatusReport/
│   │   │   ├── GetAgentWorkloadReport/
│   │   │   ├── GetHighPriorityOpenReport/
│   │   │   ├── GetResolutionTimeReport/
│   │   │   ├── GetSlaRiskReport/
│   │   │   └── GetCustomerTicketHistory/
│   │   ├── DTOs/
│   │   ├── Interfaces/
│   │   │   ├── ITicketRepository.cs
│   │   │   ├── ICommentRepository.cs
│   │   │   ├── IAssignmentRepository.cs
│   │   │   ├── ICurrentUserService.cs
│   │   │   └── IUnitOfWork.cs
│   │   └── Common/
│   │       ├── PaginatedResult.cs
│   │       └── Result.cs
│   │
│   ├── TechMaster.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/          # IEntityTypeConfiguration<T> per entity
│   │   │   ├── Migrations/
│   │   │   └── Repositories/
│   │   ├── Identity/
│   │   │   ├── IdentityService.cs
│   │   │   └── CurrentUserService.cs
│   │   └── Services/
│   │       └── SlaCalculationService.cs
│   │
│   └── TechMaster.API/
│       ├── Controllers/
│       │   ├── AuthController.cs
│       │   ├── TicketsController.cs
│       │   ├── CommentsController.cs
│       │   ├── AssignmentsController.cs
│       │   ├── ReportsController.cs
│       │   └── AdminController.cs
│       ├── Middleware/
│       │   ├── ExceptionHandlingMiddleware.cs
│       │   └── RequestLoggingMiddleware.cs
│       ├── Extensions/
│       │   └── ServiceCollectionExtensions.cs
│       └── Program.cs
│
└── tests/
    ├── TechMaster.Domain.Tests/
    ├── TechMaster.Application.Tests/
    └── TechMaster.Integration.Tests/
```

---

## 3. Clean Architecture Rules

| Rule | Detail |
|---|---|
| **Domain has zero dependencies** | No NuGet references except primitives. No EF Core, no ASP.NET. |
| **Application depends only on Domain** | Defines interfaces (ports) that Infrastructure implements. |
| **Infrastructure implements Application interfaces** | EF Core `DbContext`, Identity, external services. |
| **API depends on Application and Infrastructure** | Only for DI registration. No business logic in controllers. |

### Dependency Injection — Registration Flow

```
Program.cs
  → AddDomainServices()
  → AddApplicationServices()        // MediatR, FluentValidation, AutoMapper
  → AddInfrastructureServices()     // EF Core, Identity, JWT
  → AddApiServices()                // Swagger, CORS, Rate Limiting
```

---

## 4. CQRS with MediatR

Every state-changing operation is a **Command**; every read is a **Query**. Controllers are thin dispatchers.

### Command Flow

```
HTTP POST /api/tickets
    ↓
TicketsController.CreateTicket(request)
    ↓
_mediator.Send(new CreateTicketCommand(request, currentUserId))
    ↓
CreateTicketCommandValidator.Validate()        ← FluentValidation pipeline behavior
    ↓
CreateTicketCommandHandler.Handle()
    ↓
ITicketRepository.AddAsync(ticket)
    ↓
IUnitOfWork.SaveChangesAsync()
    ↓
return TicketDto (201 Created)
```

### Query Flow

```
HTTP GET /api/agents/me/tickets
    ↓
AgentsController.GetMyQueue(queryParams)
    ↓
_mediator.Send(new GetMyAgentQueueQuery(agentId, filters))
    ↓
GetMyAgentQueueQueryHandler.Handle()
    ↓
DbContext.Tickets
    .Where(t => t.ActiveAssignment.AgentId == agentId)
    .OrderByDescending(t => t.Priority)
    .ProjectTo<TicketDto>(mapper)   ← Projection, not entity load
    ↓
return PaginatedResult<TicketDto> (200 OK)
```

### MediatR Pipeline Behaviors

```
Request
  → LoggingBehavior<TRequest, TResponse>
  → ValidationBehavior<TRequest, TResponse>   (FluentValidation)
  → TransactionBehavior<TRequest, TResponse>  (for Commands)
  → Handler
```

---

## 5. Result Pattern

All handlers return a `Result<T>` or `Result` to avoid throwing exceptions for business failures:

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public ErrorType Type { get; }   // NotFound, Forbidden, Validation, Conflict

    public static Result<T> Success(T value) => ...
    public static Result<T> Failure(string error, ErrorType type) => ...
}
```

Controllers map `Result` → HTTP status codes:

| Result Type | HTTP Status |
|---|---|
| Success | 200 OK / 201 Created |
| NotFound | 404 Not Found |
| Forbidden | 403 Forbidden |
| Validation | 400 Bad Request |
| Conflict | 409 Conflict |

---

## 6. Current User Resolution

The `ICurrentUserService` interface (implemented in Infrastructure) extracts the authenticated user from `IHttpContextAccessor`:

```csharp
public interface ICurrentUserService
{
    string UserId { get; }
    string Role { get; }
    bool IsAuthenticated { get; }
}
```

> **Security**: `TICKET-R24` — Current-user endpoints **never** accept arbitrary customer/agent IDs in the request body. The user identity comes exclusively from the validated JWT token.

---

## 7. Error Handling

A global `ExceptionHandlingMiddleware` catches unhandled exceptions and returns a standard problem detail response:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Ticket title is required.",
  "traceId": "00-abc123-def456-00"
}
```

---

*TechMaster Academy | Phase 05 Capstone — Architecture Guide v1.0*
