# User Stories & Jira Backlog — Support Ticketing Platform

> Each story is linked to its Epic, Sprint, Acceptance Criteria, and Definition of Ready.

---

## Epic Map

| Epic | Business Outcome | Jira Epic Key |
|---|---|---|
| Ticket Intake | Customer creates and tracks issues | EPIC-TI |
| Triage | Classify category/priority and validate data | EPIC-TR |
| Assignment | Assign/reassign agents with protection | EPIC-AS |
| Conversation | Customer/agent comments and internal notes | EPIC-CV |
| Status Workflow | New to closed/reopened with transitions | EPIC-SW |
| SLA & Escalation | Track response/resolution targets | EPIC-SLA |
| Customer Portal | Own ticket history and status | EPIC-CP |
| Support Analytics | Backlog, workload, priority, resolution reports | EPIC-SA |

---

## Sprint 1 Stories

### TICKET-101 — Create a Ticket
**Epic**: Ticket Intake | **Sprint**: 1 | **Story Points**: 3

**As a** Customer,  
**I want to** create a support ticket,  
**So that** I can report a problem and get help.

**Acceptance Criteria**:
- [ ] Customer must be authenticated (JWT token with Customer role)
- [ ] `title` (5–200 chars) and `description` (10–5000 chars) are required
- [ ] `categoryId` must reference an active `TicketCategory`
- [ ] `priority` defaults to `Medium` if not provided; allowed values: `Low`, `Medium`, `High`, `Critical`
- [ ] Ticket is created with `Status = New` automatically
- [ ] `SlaResponseDueAt` and `SlaResolutionDueAt` are calculated if an SLA policy matches
- [ ] Response is `201 Created` with ticket ID and SLA due dates
- [ ] Customer ID is resolved from JWT token, not request body (TICKET-R24)

**Definition of Ready**:
- [ ] `TicketCategory` seed data exists
- [ ] `SlaPolicy` seed data exists (at least one active policy)
- [ ] Auth endpoints working
- [ ] Database schema migrated

---

### TICKET-102 — View My Tickets
**Epic**: Ticket Intake | **Sprint**: 1 | **Story Points**: 5

**As a** Customer,  
**I want to** view my support tickets,  
**So that** I can track the progress of my issues.

**Acceptance Criteria**:
- [ ] Returns only the current customer's tickets (TICKET-R01)
- [ ] Supports pagination (`page`, `pageSize`, max 50)
- [ ] Supports filtering by `status` and `priority`
- [ ] Sorted by `createdAt` descending by default
- [ ] Internal notes and agent-only fields are NOT included in response (TICKET-R03, TICKET-R19)
- [ ] Returns `200 OK` with paginated result set

---

### TICKET-103 — Set Category and Priority
**Epic**: Triage | **Sprint**: 1 | **Story Points**: 3

**As a** SupportLead or Admin,  
**I want to** set/update the category and priority of a ticket,  
**So that** urgent issues receive appropriate handling.

**Acceptance Criteria**:
- [ ] Only `SupportLead` or `Admin` roles can access this endpoint
- [ ] Priority must be a valid controlled value (TICKET-R05)
- [ ] Priority change creates a `TicketStatusHistory` or `ActivityLog` entry (TICKET-R21)
- [ ] `UpdatedAt` is refreshed on the ticket
- [ ] Response is `200 OK` with updated ticket fields

---

### TICKET-104 — Assign a Ticket
**Epic**: Assignment | **Sprint**: 1 | **Story Points**: 2

**As a** SupportLead or Admin,  
**I want to** assign a ticket to a specific agent,  
**So that** ownership and accountability are clear.

**Acceptance Criteria**:
- [ ] Target agent must exist and have `IsActive = true` (TICKET-R07, TICKET-R22)
- [ ] Ticket must not be in a terminal status (`Closed`, `Cancelled`)
- [ ] A `TicketAssignment` row is inserted with `IsActive = true` (TICKET-R08)
- [ ] Ticket `Status` transitions to `Assigned`
- [ ] `TicketStatusHistory` entry is created
- [ ] `ActivityLog` entry is created
- [ ] Rejecting inactive agent returns `400 Bad Request`

---

### TICKET-105 — Reassign a Ticket
**Epic**: Assignment | **Sprint**: 1 | **Story Points**: 5

**As a** SupportLead,  
**I want to** reassign a ticket to a different agent,  
**So that** workload can be balanced.

**Acceptance Criteria**:
- [ ] Previous active `TicketAssignment` is ended (`EndedAt`, `IsActive = false`) — NOT deleted (TICKET-R08)
- [ ] New `TicketAssignment` is created for the new agent
- [ ] New agent must be active (TICKET-R22)
- [ ] `ActivityLog` records old and new agent IDs
- [ ] Status behavior on reassignment is documented (see `status-workflow.md`)
- [ ] Response includes old and new agent names

---

### TICKET-106 — View My Agent Queue
**Epic**: Assignment | **Sprint**: 1 | **Story Points**: 3

**As a** SupportAgent,  
**I want to** view my assigned tickets,  
**So that** I know what I need to work on.

**Acceptance Criteria**:
- [ ] Returns only tickets with an active assignment to the current agent (TICKET-R02, TICKET-R24)
- [ ] Default sort: `Critical` first, then `High`, `Medium`, `Low` (TICKET-R13), then by `SlaResolutionDueAt`
- [ ] Supports filtering by `status` and `slaStatus`
- [ ] `Closed` and `Cancelled` tickets excluded by default
- [ ] Does NOT include internal notes in queue summary (TICKET-R03)
- [ ] Paginated result

---

## Sprint 2 Stories

### TICKET-107 — Add Public Comment (Customer)
**Epic**: Conversation | **Sprint**: 2 | **Story Points**: 5

**As a** Customer,  
**I want to** add a public comment to my open ticket,  
**So that** I can provide more information to the support team.

**Acceptance Criteria**:
- [ ] Customer can only comment on their own tickets (TICKET-R01)
- [ ] Cannot comment on `Closed` or `Cancelled` tickets (TICKET-R10)
- [ ] `Visibility` is automatically set to `Public`
- [ ] `AuthorId` is resolved from JWT (not request body)
- [ ] Response is `201 Created` with comment ID, content, and timestamp

---

### TICKET-108 — Add Public Reply (Agent)
**Epic**: Conversation | **Sprint**: 2 | **Story Points**: 5

**As a** SupportAgent,  
**I want to** add a public reply to an assigned ticket,  
**So that** the customer receives support and guidance.

**Acceptance Criteria**:
- [ ] Agent must have an active assignment to the ticket (TICKET-R02, TICKET-R12)
- [ ] Cannot reply on `Closed` or `Cancelled` tickets (TICKET-R10)
- [ ] `Visibility` is `Public`
- [ ] `AuthorId` resolved from JWT
- [ ] `SupportLead` and `Admin` can also add public replies without assignment check

---

### TICKET-109 — Add Internal Note
**Epic**: Conversation | **Sprint**: 2 | **Story Points**: 3

**As a** SupportAgent or SupportLead,  
**I want to** add an internal note to a ticket,  
**So that** the team can coordinate privately without the customer seeing it.

**Acceptance Criteria**:
- [ ] Only `SupportAgent`, `SupportLead`, `Admin` roles allowed (TICKET-R11)
- [ ] `Visibility` is `Internal`
- [ ] Internal notes are NEVER returned in any customer-facing query (TICKET-R03)
- [ ] Customer attempting to post an internal note receives `403 Forbidden`

---

### TICKET-110 — Move Ticket to InProgress
**Epic**: Status Workflow | **Sprint**: 2 | **Story Points**: 3

**As a** SupportAgent,  
**I want to** mark my assigned ticket as InProgress,  
**So that** active work is visible to the team and customer.

**Acceptance Criteria**:
- [ ] Agent must be the assigned agent (TICKET-R02)
- [ ] Only valid from `Assigned` status
- [ ] `StartedAt` is set once by the server if null (TICKET-R14)
- [ ] `TicketStatusHistory` entry created

---

### TICKET-111 — Resolve a Ticket
**Epic**: Status Workflow | **Sprint**: 2 | **Story Points**: 5

**As a** SupportAgent,  
**I want to** mark my ticket as Resolved,  
**So that** the customer can review and confirm the solution.

**Acceptance Criteria**:
- [ ] Agent must be the assigned agent
- [ ] Transition from `InProgress` only
- [ ] `ResolutionNote` required (configurable enforcement)
- [ ] `ResolvedAt` set by server (TICKET-R14)
- [ ] `SlaStatus` evaluated against `SlaResolutionDueAt`
- [ ] `TicketStatusHistory` entry created

---

### TICKET-112 — Close or Reopen
**Epic**: Status Workflow | **Sprint**: 2 | **Story Points**: 2

**As a** Customer or SupportLead,  
**I want to** close a resolved ticket or reopen a closed one,  
**So that** the ticket lifecycle reflects reality.

**Acceptance Criteria**:
- [ ] `Closed`: Customer closes their own resolved ticket; Lead/Admin can also close
- [ ] `ClosedAt` set by server (TICKET-R14)
- [ ] Reopen: Only by `SupportLead`/`Admin` by default (TICKET-R15)
- [ ] Reopen creates `TicketStatusHistory` with reason required
- [ ] Unauthorized customer reopen returns `403 Forbidden`

---

## Sprint 3 Stories

### TICKET-113 — Identify SLA-At-Risk Tickets
**Epic**: SLA & Escalation | **Sprint**: 3 | **Story Points**: 3

**As a** SupportLead (via system identification),  
**I want to** identify tickets approaching their SLA target,  
**So that** the team can prioritize before a breach.

**Acceptance Criteria**:
- [ ] Only unresolved tickets are included
- [ ] SLA target calculated from policy/priority (TICKET-R17)
- [ ] Returns tickets with `SlaStatus = AtRisk` and `SlaStatus = Breached`
- [ ] Sorted by `SlaResolutionDueAt` ascending (most urgent first)
- [ ] Includes minutes remaining until breach

---

### TICKET-114 — View Agent Workload
**Epic**: Support Analytics | **Sprint**: 3 | **Story Points**: 5

**As a** SupportLead,  
**I want to** view the current workload of each agent,  
**So that** I can balance assignments effectively.

**Acceptance Criteria**:
- [ ] Shows open/active ticket counts per agent
- [ ] Breaks down by `New`, `Assigned`, `InProgress`
- [ ] Includes count of `Critical` and `High` priority tickets per agent
- [ ] Scoped to the team if applicable
- [ ] Excludes `Closed` and `Cancelled` tickets

---

### TICKET-115 — View Tickets by Status/Priority
**Epic**: Support Analytics | **Sprint**: 3 | **Story Points**: 3

**As an** Admin,  
**I want to** view aggregate ticket counts by status and priority,  
**So that** I can monitor the support backlog.

**Acceptance Criteria**:
- [ ] Aggregated server-side (not raw data dump)
- [ ] Date range filter (`dateFrom`, `dateTo`)
- [ ] Excludes cancelled tickets from metrics (TICKET-R18)
- [ ] Returns counts per status and per priority

---

### TICKET-116 — View Resolution Time Metrics
**Epic**: Support Analytics | **Sprint**: 3 | **Story Points**: 2

**As an** Admin or SupportLead,  
**I want to** view resolution time metrics,  
**So that** I can review service performance.

**Acceptance Criteria**:
- [ ] Metric: `ResolvedAt - CreatedAt` in minutes
- [ ] Only includes tickets with non-null `ResolvedAt` and `Status` of `Resolved` or `Closed`
- [ ] Excludes cancelled tickets (TICKET-R18)
- [ ] Reports average and median by agent and by category
- [ ] Date range filter

---

### TICKET-117 — Audit Assignment/Status Changes
**Epic**: Audit | **Sprint**: 3 | **Story Points**: 5

**As an** Admin,  
**I want to** review all assignment and status changes,  
**So that** sensitive actions are traceable.

**Acceptance Criteria**:
- [ ] Filterable by `entityName`, `entityId`, `actorUserId`, date range
- [ ] Paginated
- [ ] Does NOT include comment body text (TICKET-R21)
- [ ] Shows actor identity, timestamp, old value, new value

---

### TICKET-118 — Cancel a Ticket
**Epic**: Ticket Intake | **Sprint**: 3 | **Story Points**: 3

**As a** Customer,  
**I want to** cancel a ticket that no longer needs support,  
**So that** the queue stays clean and agents aren't working stale issues.

**Acceptance Criteria**:
- [ ] Only allowed from `New` or `Assigned` status (TICKET-R16)
- [ ] `cancellationReason` required
- [ ] `TicketStatusHistory` entry created
- [ ] Cancelled tickets excluded from metrics (TICKET-R18)
- [ ] Attempting to cancel from `InProgress` returns `409 Conflict`

---

## Definition of Ready Checklist

Before pulling a story into a sprint, confirm:

- [ ] Story has clear, testable acceptance criteria
- [ ] All dependent stories/database changes are merged or planned
- [ ] The team has estimated story points
- [ ] Risk register has been reviewed for applicable risks
- [ ] API contract for this story is defined in `api-reference.md`
- [ ] CQRS handler is listed in `cqrs-mapping.md`

---

*TechMaster Academy | Phase 05 Capstone — User Stories v1.0*
