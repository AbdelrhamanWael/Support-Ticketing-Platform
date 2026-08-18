# Project Review — Questions & Answers

> These answers prepare the team for the capstone review session. Each answer is grounded in a documented design decision.

---

## Q1: What is the hardest business invariant in the Support Ticketing Platform API and where is it enforced?

**The hardest invariant is: Internal notes must never be visible to customers — under any circumstance.**

This is hard because it requires correctness at every layer simultaneously:
- The query handler must filter by `Visibility == Public` for Customer role
- The AutoMapper profile must use a customer-specific DTO that has no `Visibility` or `Content` for internal comments
- The controller route `/api/tickets/{id}/internal-notes` is protected at the authorization layer (`[Authorize(Roles = "SupportAgent,SupportLead,Admin")]`)

This defense-in-depth approach means two independent mechanisms must both fail for a leak to occur.

**Documented in**: `business-rules.md` (TICKET-R03, TICKET-R11, TICKET-R19), `adr/ADR-002-internal-note-visibility.md`

---

## Q2: Which command required a transaction and why?

**`AssignTicketCommand` and `ReassignTicketCommand`** require database transactions because they modify multiple related rows atomically:

1. End the current active `TicketAssignment` (`EndedAt`, `IsActive = false`)
2. Insert a new `TicketAssignment` row (`IsActive = true`)
3. Update `Ticket.AssignedAgentId` and `Ticket.Status`
4. Insert a `TicketStatusHistory` row
5. Insert an `ActivityLog` row

If any step fails after step 1, the ticket would be left with no active assignment. The transaction ensures all steps succeed together or none do, protecting the **one-active assignment** invariant (RISK-03, RISK-04).

**Implemented via**: `TransactionBehavior<TRequest, TResponse>` MediatR pipeline behavior for all Commands.

---

## Q3: Which query was designed as a projection instead of loading entities?

**`GetMyAgentQueueQuery`** was intentionally designed as a database-level projection.

The agent queue only needs: ticket ID, title, status, priority, SLA due date, customer display name, and creation date. Loading full `Ticket` entities with all navigation properties (comments, attachments, full assignment history) would:
- Cause N+1 query problems
- Return megabytes of unused data per request
- Degrade response time under load (RISK-08)

The handler uses:
```csharp
.ProjectTo<AgentQueueTicketDto>(_mapper.ConfigurationProvider)
```
This generates a `SELECT` with only the needed columns directly in SQL.

**Documented in**: `cqrs-mapping.md` (GetMyAgentQueueQuery section)

---

## Q4: How does the API resolve the current user and prevent cross-user access?

The `ICurrentUserService` interface extracts the authenticated identity from `IHttpContextAccessor.HttpContext.User`, which is populated by the JWT Bearer middleware before any handler runs.

```csharp
public class CurrentUserService : ICurrentUserService
{
    public string UserId => _httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public string Role   => _httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.Role)!;
}
```

Query/command handlers **never** accept a `customerId` or `agentId` from the HTTP request for current-user operations. They call `_currentUser.CustomerProfileId` or `_currentUser.AgentProfileId` directly (TICKET-R24). This structurally prevents cross-user data access even if a malicious actor passes a different ID in the request body.

---

## Q5: Which database constraint protects a rule even if application logic fails?

**The filtered unique index on `TicketAssignment`**:

```sql
CREATE UNIQUE INDEX UX_TicketAssignment_Active
ON TicketAssignment (TicketId)
WHERE IsActive = 1;
```

This enforces the "one active assignment per ticket" rule at the database level. Even if a race condition causes two concurrent requests to bypass the application-level check, the database will reject the second insert with a unique constraint violation — preventing two agents from being simultaneously "active" on the same ticket.

**Documented in**: `erd-design.md` (Section 4: Unique Constraints), `adr/ADR-003-assignment-history.md`

---

## Q6: What race condition could happen in this domain and what did the team do about it?

**Race Condition**: Two support leads simultaneously assign the same ticket to different agents.

**Scenario**:
1. Lead A reads ticket 42, sees it is Unassigned
2. Lead B reads ticket 42, sees it is Unassigned
3. Lead A assigns to Agent X
4. Lead B assigns to Agent Y (before A's transaction commits)
5. Result: two active assignments for ticket 42

**Mitigation**:
1. **Database transaction** wrapping the check-and-set: the unique filtered index on `TicketAssignment` will reject the second insert, rolling back Lead B's transaction.
2. **Optimistic concurrency**: A `RowVersion` / `[Timestamp]` column on `Ticket` ensures that if Lead B tries to update a row that Lead A already changed, EF Core throws a `DbUpdateConcurrencyException`, which the handler maps to a `409 Conflict` response.

**Documented in**: `erd-design.md` (Section 7: Concurrency-Sensitive Rows)

---

## Q7: Which story was split because it was too large for one sprint?

**TICKET-102 (View My Tickets)** was kept simple in Sprint 1 (own tickets, pagination, filter) but the internal visibility concern was treated as a separate acceptance criterion that required cross-cutting work in Sprint 2 (once comments were implemented).

More significantly, **the Conversation epic stories (TICKET-107, TICKET-108, TICKET-109)** were separated into distinct stories even though they are the same endpoint conceptually, because each has unique security rules:
- TICKET-107: Customer path — ownership check
- TICKET-108: Agent path — assignment check
- TICKET-109: Internal note — role-gate

This allowed each story to be owned by a different engineer and individually tested.

---

## Q8: Which bug created the most learning and what regression test prevents it now?

**Bug**: Internal notes were appearing in `GET /api/tickets/{id}/comments` responses for Customer users because the handler was loading the full `Ticket` entity including all comments, and the AutoMapper profile wasn't filtering by visibility.

**Root Cause**: The AutoMapper `Ticket → TicketDetailDto` profile mapped all comments regardless of visibility. The developer assumed the controller would filter — it did not.

**Fix**: 
1. The query handler now receives an `IncludeInternal` boolean set by the handler itself based on role.
2. A dedicated `CustomerConversationDto` was created that excludes the `Visibility` field entirely.
3. The `GetTicketConversationQuery` explicitly `.Where(c => c.Visibility == Public)` before projection when `IncludeInternal == false`.

**Regression Test**: TEST-11 — runs on every CI build.

---

## Q9: What ADR best explains a trade-off your team made?

**ADR-006: SLA Calculation Strategy** best demonstrates a real trade-off.

The team recognized that business-hour SLA calculation (e.g., "2 hours of business time" means until 11 AM if created at 9 AM, but skips weekends) is the correct real-world behavior. However, implementing it correctly requires timezone configuration, business calendar management, and complex date arithmetic.

The trade-off: ship wall-clock SLA in v1.0 with a clearly defined, testable metric. The `ISlaCalculationService` interface is designed now so the implementation can be swapped in a stretch sprint without any handler changes. The limitation is explicitly documented in the API response labels and in the ADR so future developers understand the known gap.

**This demonstrates**: deliberate technical debt with a clear payoff path, rather than accidental debt from not thinking it through.

---

## Q10: What would you implement next if this product received a second release cycle?

The top three candidates for Release 2:

1. **Business-Hour SLA** (`BusinessHourSlaCalculationService` implementing the existing `ISlaCalculationService` interface) — the interface is already designed for this swap. Estimated: 1 sprint.

2. **Background Breach Notifications** (Hangfire or .NET Hosted Service) — a recurring job that scans for `AtRisk`/`Breached` tickets and sends email/webhook notifications to the assigned agent and lead. The `SlaResolutionDueAt` index is already in place to make this query fast.

3. **Customer Satisfaction Rating** — after a ticket is `Closed`, send the customer a 1–5 star rating request. This closes the feedback loop and provides the business with CSAT data for agent performance reviews. Requires adding a `TicketRating` entity and an email integration.

---

*TechMaster Academy | Phase 05 Capstone — Project Review Answers v1.0*
