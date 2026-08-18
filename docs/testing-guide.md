# Testing Guide — Support Ticketing Platform

## 1. Testing Strategy

The project uses a three-tier testing approach:

| Tier | Type | Scope | Tools |
|---|---|---|---|
| **Unit** | Domain logic, validators, handlers in isolation | Single class | xUnit, Moq, FluentAssertions |
| **Integration** | Handler → EF Core → Database | Application + Infrastructure | xUnit, EF Core InMemory or TestContainers |
| **Acceptance** | Full HTTP request → response | API → DB | Postman / xUnit Web Application Factory |

---

## 2. Acceptance Test Matrix

| Test ID | Scenario | Expected Outcome | Priority |
|---|---|---|---|
| TEST-01 | Happy path: Create and Triage | Customer creates ticket → Lead sets priority → category assigned → ticket appears in agent queue | 🔴 Critical |
| TEST-02 | Happy path: Assign and Work | Lead assigns to agent → agent sees ticket → moves to InProgress → adds comments | 🔴 Critical |
| TEST-03 | Happy path: Resolve and Close | Agent resolves → `ResolvedAt` set → customer closes → `ClosedAt` set → history complete | 🔴 Critical |
| TEST-04 | Happy path: Reopen | Lead reopens closed ticket → history entry created → new work begins | 🔴 Critical |
| TEST-05 | Customer views another customer's ticket | `403 Forbidden` | 🔴 Critical |
| TEST-06 | Agent accesses unassigned ticket | `403 Forbidden` | 🔴 Critical |
| TEST-07 | Closed ticket receives comment | `409 Conflict` | 🔴 Critical |
| TEST-08 | Invalid New → Closed transition | `409 Conflict` | 🔴 Critical |
| TEST-09 | Customer posts internal note | `403 Forbidden` | 🔴 Critical |
| TEST-10 | Assign to inactive agent | `400 Bad Request` | 🔴 Critical |
| TEST-11 | Customer sees internal note in ticket details | Internal notes absent from response | 🔴 Critical |
| TEST-12 | Unauthorized customer reopen | `403 Forbidden` | 🔴 Critical |

---

## 3. Unit Test Specifications

### 3.1 `TicketStatusTransitionValidator` Tests

```csharp
public class TicketStatusTransitionValidatorTests
{
    [Theory]
    [InlineData(TicketStatus.Assigned, TicketStatus.InProgress, "SupportAgent", true)]
    [InlineData(TicketStatus.Assigned, TicketStatus.InProgress, "Customer", false)]
    [InlineData(TicketStatus.New, TicketStatus.Closed, "Admin", false)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Resolved, "SupportAgent", true)]
    [InlineData(TicketStatus.Closed, TicketStatus.Reopened, "SupportLead", true)]
    [InlineData(TicketStatus.Closed, TicketStatus.Reopened, "Customer", false)]
    [InlineData(TicketStatus.Cancelled, TicketStatus.InProgress, "Admin", false)]
    public void IsAllowed_ReturnsExpectedResult(
        TicketStatus from, TicketStatus to, string role, bool expected)
    {
        var result = TicketStatusTransitionValidator.IsAllowed(from, to, role);
        result.Should().Be(expected);
    }
}
```

---

### 3.2 `CreateTicketCommandValidator` Tests

```csharp
public class CreateTicketCommandValidatorTests
{
    private readonly CreateTicketCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenTitleTooShort_ReturnsFail()
    {
        var command = new CreateTicketCommand("Hi", "Valid long description here", 1, TicketPriority.Medium);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_WhenDescriptionEmpty_ReturnsFail()
    {
        var command = new CreateTicketCommand("Valid Title", "", 1, TicketPriority.Medium);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WhenAllFieldsValid_ReturnsSuccess()
    {
        var command = new CreateTicketCommand(
            "I cannot login to the portal",
            "Tried multiple times, getting 401 Unauthorized error on login page.",
            1,
            TicketPriority.High);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
```

---

### 3.3 `AssignTicketCommandHandler` Tests

```csharp
public class AssignTicketCommandHandlerTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();

    [Fact]
    public async Task Handle_WhenAgentInactive_ReturnsFailure()
    {
        // Arrange
        var inactiveAgent = new AgentProfile { Id = 5, IsActive = false };
        // ... setup mock context
        var handler = new AssignTicketCommandHandler(_contextMock.Object, _currentUserMock.Object);
        var command = new AssignTicketCommand(TicketId: 1, AgentId: 5, Note: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ErrorType.BadRequest);
        result.Error.Should().Contain("not active");
    }

    [Fact]
    public async Task Handle_WhenAgentActive_CreatesAssignmentRow()
    {
        // ... Verify TicketAssignment is inserted with IsActive = true
        // ... Verify Ticket.Status becomes Assigned
        // ... Verify TicketStatusHistory row is inserted
    }

    [Fact]
    public async Task Handle_WhenReassigning_EndsCurrentAssignment()
    {
        // ... Verify previous assignment gets EndedAt and IsActive = false
        // ... Verify new assignment created
    }
}
```

---

### 3.4 `GetTicketConversationQueryHandler` Tests

```csharp
[Fact]
public async Task Handle_WhenCallerIsCustomer_ExcludesInternalNotes()
{
    // Arrange: ticket has 1 public comment and 1 internal note
    _currentUserMock.Setup(x => x.Role).Returns("Customer");
    _currentUserMock.Setup(x => x.CustomerProfileId).Returns(customerId);

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    result.Value.Comments.Should().HaveCount(1);
    result.Value.Comments.Should().NotContain(c => c.Visibility == CommentVisibility.Internal);
}

[Fact]
public async Task Handle_WhenCallerIsAgent_IncludesInternalNotes()
{
    _currentUserMock.Setup(x => x.Role).Returns("SupportAgent");
    // ...
    result.Value.Comments.Should().HaveCount(2);
}
```

---

## 4. Integration Test Specifications

### 4.1 Full Create → Assign → Work → Resolve → Close Flow

```csharp
[Fact]
public async Task FullTicketLifecycle_HappyPath()
{
    // 1. Customer creates ticket
    var createResponse = await _client.PostAsJsonAsync("/api/tickets", new
    {
        title = "Integration test ticket",
        description = "This is an integration test for the full lifecycle.",
        categoryId = 1,
        priority = "High"
    });
    createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    var ticket = await createResponse.Content.ReadFromJsonAsync<TicketDto>();

    // 2. Lead assigns to agent
    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", _leadToken);
    var assignResponse = await _client.PutAsJsonAsync($"/api/tickets/{ticket.Id}/assign", new
    {
        agentId = _activeAgentId
    });
    assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    // 3. Agent moves to InProgress
    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", _agentToken);
    var statusResponse = await _client.PutAsJsonAsync($"/api/tickets/{ticket.Id}/status", new
    {
        newStatus = "InProgress"
    });
    statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    // 4. Agent resolves
    var resolveResponse = await _client.PutAsJsonAsync($"/api/tickets/{ticket.Id}/status", new
    {
        newStatus = "Resolved",
        reason = "Fixed by resetting password"
    });
    resolveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    // 5. Customer closes
    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", _customerToken);
    var closeResponse = await _client.PutAsJsonAsync($"/api/tickets/{ticket.Id}/status", new
    {
        newStatus = "Closed"
    });
    closeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    // 6. Verify history has 5 entries (Created, Assigned, InProgress, Resolved, Closed)
    var historyResponse = await _client.GetAsync($"/api/tickets/{ticket.Id}/history");
    var history = await historyResponse.Content.ReadFromJsonAsync<TicketHistoryDto>();
    history.History.Should().HaveCount(5);
}
```

---

## 5. Negative Test Specifications (Security Enforcement)

### TEST-05: Cross-Customer Access

```csharp
[Fact]
public async Task GetTicketDetails_WhenCustomerRequestsOtherTicket_Returns403()
{
    // Arrange: ticket belongs to Customer A; caller is authenticated as Customer B
    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", _customerBToken);

    // Act
    var response = await _client.GetAsync($"/api/tickets/{_customerATicketId}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

### TEST-06: Agent Accesses Unassigned Ticket

```csharp
[Fact]
public async Task GetTicketDetails_WhenAgentNotAssigned_Returns403()
{
    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", _differentAgentToken);
    var response = await _client.GetAsync($"/api/tickets/{_ticketAssignedToOtherAgent}");
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

### TEST-07: Comment on Closed Ticket

```csharp
[Fact]
public async Task AddComment_WhenTicketClosed_Returns409()
{
    // Assume ticket is Closed
    var response = await _client.PostAsJsonAsync($"/api/tickets/{_closedTicketId}/comments", new
    {
        content = "Is this still being worked on?"
    });
    response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
    problem.Detail.Should().Contain("closed");
}
```

### TEST-08: Invalid Status Transition

```csharp
[Fact]
public async Task ChangeStatus_WhenNewToClosedTransition_Returns409()
{
    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", _adminToken);
    var response = await _client.PutAsJsonAsync($"/api/tickets/{_newTicketId}/status", new
    {
        newStatus = "Closed"
    });
    response.StatusCode.Should().Be(HttpStatusCode.Conflict);
}
```

### TEST-09: Customer Posts Internal Note

```csharp
[Fact]
public async Task AddInternalNote_WhenCallerIsCustomer_Returns403()
{
    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", _customerToken);
    var response = await _client.PostAsJsonAsync($"/api/tickets/{_ticketId}/internal-notes", new
    {
        content = "Trying to post an internal note as customer"
    });
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

### TEST-10: Assign Inactive Agent

```csharp
[Fact]
public async Task AssignTicket_WhenAgentInactive_Returns400()
{
    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", _leadToken);
    var response = await _client.PutAsJsonAsync($"/api/tickets/{_ticketId}/assign", new
    {
        agentId = _inactiveAgentId
    });
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}
```

### TEST-11: Internal Note Invisible to Customer

```csharp
[Fact]
public async Task GetConversation_WhenCustomer_NeverContainsInternalNotes()
{
    // Seed: ticket with 1 public + 1 internal comment
    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", _customerToken);
    var response = await _client.GetAsync($"/api/tickets/{_ticketId}/comments");
    var body = await response.Content.ReadFromJsonAsync<ConversationDto>();
    body.Comments.Should().NotContain(c => c.Visibility == "Internal");
    body.Comments.Should().HaveCount(1);
}
```

### TEST-12: Unauthorized Reopen

```csharp
[Fact]
public async Task ReopenTicket_WhenCustomerAndPolicyIsStaffOnly_Returns403()
{
    // Policy: AllowCustomerReopen = false
    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", _customerToken);
    var response = await _client.PutAsJsonAsync($"/api/tickets/{_closedTicketId}/reopen", new
    {
        reason = "Still broken"
    });
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

---

## 6. Postman Collection Structure

The Postman collection is organized by flow:

```
📁 TechMaster Support Ticketing API
├── 📁 Auth
│   ├── Register Customer
│   ├── Login as Customer
│   ├── Login as Agent
│   ├── Login as Lead
│   └── Login as Admin
├── 📁 Happy Paths
│   ├── 🟢 Create & Triage Flow
│   ├── 🟢 Assign & Work Flow
│   ├── 🟢 Resolve & Close Flow
│   └── 🟢 Reopen Flow
├── 📁 Negative Tests
│   ├── 🔴 Cross-Customer Access (403)
│   ├── 🔴 Unassigned Agent Access (403)
│   ├── 🔴 Comment on Closed (409)
│   ├── 🔴 Invalid Transition (409)
│   ├── 🔴 Customer Internal Note (403)
│   ├── 🔴 Inactive Agent Assign (400)
│   ├── 🔴 Internal Note Visible to Customer (verify absence)
│   └── 🔴 Unauthorized Reopen (403)
└── 📁 Reports
    ├── Agent Workload
    ├── SLA Risk
    ├── Tickets by Status
    └── Resolution Time
```

> **Evidence Tip**: Run the full Postman collection with the Newman CLI reporter to generate a timestamped HTML report for your submission.

```bash
newman run TechMasterCapstone.postman_collection.json \
  --environment TechMasterCapstone.postman_environment.json \
  --reporters htmlextra \
  --reporter-htmlextra-export ./test-evidence/report.html
```

---

## 7. Test Coverage Targets

| Area | Target Coverage |
|---|---|
| Domain (entities, enums, validators) | ≥ 90% |
| Application (command handlers) | ≥ 80% |
| Application (query handlers) | ≥ 70% |
| Integration (critical paths) | All 12 acceptance tests passing |

---

*TechMaster Academy | Phase 05 Capstone — Testing Guide v1.0*
