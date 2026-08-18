# API Reference — Support Ticketing Platform

> **Base URL**: `https://localhost:7001/api`
> **Auth**: All endpoints (except `/auth/*`) require `Authorization: Bearer <token>`
> **Content-Type**: `application/json`

---

## Table of Contents

- [Authentication](#authentication)
- [Tickets](#tickets)
- [Comments & Notes](#comments--notes)
- [Assignments](#assignments)
- [Customer Portal](#customer-portal)
- [Agent Queue](#agent-queue)
- [Admin / Configuration](#admin--configuration)
- [Reports](#reports)
- [Error Responses](#error-responses)

---

## Authentication

### `POST /api/auth/register/customer`
Register a new customer account.

**Access**: Public

**Request Body**:
```json
{
  "email": "customer@example.com",
  "password": "P@ssw0rd!",
  "displayName": "Jane Smith",
  "phone": "+1-555-0100",
  "company": "Acme Corp"
}
```

**Response `201 Created`**:
```json
{
  "userId": "guid-string",
  "email": "customer@example.com",
  "token": "eyJhbGciOiJIUzI1NiIs..."
}
```

---

### `POST /api/auth/login`
Authenticate and receive a JWT token.

**Access**: Public

**Request Body**:
```json
{
  "email": "user@example.com",
  "password": "P@ssw0rd!"
}
```

**Response `200 OK`**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2026-08-18T08:00:00Z",
  "role": "Customer"
}
```

---

## Tickets

### `POST /api/tickets` — CreateTicket
Create a new support ticket.

**Access**: `Customer`
**CQRS**: `CreateTicketCommand`
**Rules**: TICKET-R04, TICKET-R05, TICKET-R06, TICKET-R24

**Request Body**:
```json
{
  "title": "Cannot log into the portal",
  "description": "I've been trying for 30 minutes. Error code: 401.",
  "categoryId": 3,
  "priority": "High"
}
```

| Field | Required | Validation |
|---|---|---|
| `title` | ✅ | 5–200 characters |
| `description` | ✅ | 10–5000 characters |
| `categoryId` | ✅ | Must reference an active category |
| `priority` | ❌ | Defaults to `Medium`. Allowed: `Low`, `Medium`, `High`, `Critical` |

**Response `201 Created`**:
```json
{
  "id": 42,
  "title": "Cannot log into the portal",
  "status": "New",
  "priority": "High",
  "category": "Access Issues",
  "createdAt": "2026-08-17T08:00:00Z",
  "slaResponseDueAt": "2026-08-17T10:00:00Z"
}
```

---

### `GET /api/tickets/{id}` — GetTicketDetails
Get full details of a specific ticket.

**Access**: `Customer` (own only), `SupportAgent` (assigned only), `SupportLead`, `Admin`
**CQRS**: `GetTicketDetailsQuery`
**Rules**: TICKET-R01, TICKET-R02, TICKET-R03, TICKET-R19

**Response `200 OK`** (Customer perspective — internal fields stripped):
```json
{
  "id": 42,
  "title": "Cannot log into the portal",
  "description": "I've been trying for 30 minutes. Error code: 401.",
  "status": "InProgress",
  "priority": "High",
  "category": "Access Issues",
  "assignedAgent": "Alex Johnson",
  "createdAt": "2026-08-17T08:00:00Z",
  "updatedAt": "2026-08-17T09:30:00Z",
  "slaResponseDueAt": "2026-08-17T10:00:00Z",
  "slaResolutionDueAt": "2026-08-18T08:00:00Z",
  "slaStatus": "OnTrack"
}
```

> **Security Note**: Agent/Admin response additionally includes `internalNotes`, `agentProfileId`, `assignmentHistory`. These are **never** returned to the Customer role.

---

### `PUT /api/tickets/{id}/priority` — SetTicketPriority
Set or change the priority of a ticket.

**Access**: `SupportLead`, `Admin`
**CQRS**: `SetTicketPriorityCommand`
**Rules**: TICKET-R05, TICKET-R21

**Request Body**:
```json
{
  "priority": "Critical",
  "reason": "Customer VIP — escalated by account manager"
}
```

**Response `200 OK`**:
```json
{
  "id": 42,
  "priority": "Critical",
  "updatedAt": "2026-08-17T09:45:00Z"
}
```

---

### `PUT /api/tickets/{id}/status` — ChangeTicketStatus
Transition a ticket to a new status.

**Access**: `SupportAgent` (limited transitions), `SupportLead`, `Admin`
**CQRS**: `ChangeTicketStatusCommand`
**Rules**: TICKET-R09, TICKET-R10, TICKET-R12, TICKET-R14, TICKET-R15

**Request Body**:
```json
{
  "newStatus": "InProgress",
  "reason": "Starting investigation"
}
```

**Valid Transitions by Role**:

| From | To | Allowed Roles |
|---|---|---|
| `New` | `Assigned` | System (on assignment) |
| `Assigned` | `InProgress` | `SupportAgent` (assigned), `SupportLead`, `Admin` |
| `InProgress` | `Resolved` | `SupportAgent` (assigned), `SupportLead`, `Admin` |
| `Resolved` | `Closed` | `Customer`, `SupportLead`, `Admin` |
| `New` | `Cancelled` | `Customer` (own), `Admin` |
| `Closed` | `Reopened` | `SupportLead`, `Admin` (Customer only if policy allows) |
| `Reopened` | `InProgress` | `SupportAgent` (assigned), `SupportLead`, `Admin` |

**Response `200 OK`**:
```json
{
  "id": 42,
  "status": "InProgress",
  "startedAt": "2026-08-17T09:50:00Z"
}
```

**Error `409 Conflict`** (invalid transition):
```json
{
  "title": "Invalid Status Transition",
  "detail": "Cannot transition from 'New' to 'Closed' directly.",
  "status": 409
}
```

---

### `PUT /api/tickets/{id}/reopen` — ReopenTicket
Reopen a closed ticket.

**Access**: `SupportLead`, `Admin` (Customer requires policy flag)
**CQRS**: `ReopenTicketCommand`
**Rules**: TICKET-R15

**Request Body**:
```json
{
  "reason": "Customer reports problem still exists after resolution"
}
```

**Response `200 OK`**:
```json
{
  "id": 42,
  "status": "Reopened",
  "updatedAt": "2026-08-17T11:00:00Z"
}
```

---

### `DELETE /api/tickets/{id}/cancel` — CancelTicket
Cancel a ticket in an early state.

**Access**: `Customer` (own ticket), `Admin`
**CQRS**: `CancelTicketCommand`
**Rules**: TICKET-R16, TICKET-R18

**Request Body**:
```json
{
  "cancellationReason": "Issue resolved itself"
}
```

**Response `200 OK`**

**Error `409 Conflict`**:
```json
{
  "title": "Cannot Cancel Ticket",
  "detail": "Ticket in status 'InProgress' cannot be cancelled.",
  "status": 409
}
```

---

## Comments & Notes

### `POST /api/tickets/{id}/comments` — AddPublicComment
Add a customer-visible comment to a ticket.

**Access**: `Customer` (own ticket), `SupportAgent` (assigned ticket), `SupportLead`, `Admin`
**CQRS**: `AddPublicCommentCommand`
**Rules**: TICKET-R01, TICKET-R10, TICKET-R11

**Request Body**:
```json
{
  "content": "I've attached the error log. Please review."
}
```

**Response `201 Created`**:
```json
{
  "id": 99,
  "ticketId": 42,
  "content": "I've attached the error log. Please review.",
  "visibility": "Public",
  "authorName": "Jane Smith",
  "createdAt": "2026-08-17T10:00:00Z"
}
```

**Error `409 Conflict`** (ticket is Closed):
```json
{
  "title": "Comment Not Allowed",
  "detail": "Cannot add comments to a closed ticket.",
  "status": 409
}
```

---

### `POST /api/tickets/{id}/internal-notes` — AddInternalNote
Add a staff-only internal note.

**Access**: `SupportAgent` (assigned), `SupportLead`, `Admin`
**CQRS**: `AddInternalNoteCommand`
**Rules**: TICKET-R03, TICKET-R11, TICKET-R12

**Request Body**:
```json
{
  "content": "Checked with DB team — this is a known bug in version 2.4. ETA fix: 2 days."
}
```

**Response `201 Created`**:
```json
{
  "id": 100,
  "ticketId": 42,
  "content": "Checked with DB team — this is a known bug in version 2.4. ETA fix: 2 days.",
  "visibility": "Internal",
  "authorName": "Alex Johnson",
  "createdAt": "2026-08-17T10:15:00Z"
}
```

---

### `GET /api/tickets/{id}/comments` — GetTicketConversation
Get comments for a ticket. Internal notes are stripped for Customer role.

**Access**: `Customer` (own; Public only), `Agent/Lead/Admin` (all visibility)
**CQRS**: `GetTicketConversationQuery`
**Rules**: TICKET-R03, TICKET-R19

**Response `200 OK`**:
```json
{
  "ticketId": 42,
  "comments": [
    {
      "id": 99,
      "content": "I've attached the error log. Please review.",
      "visibility": "Public",
      "authorName": "Jane Smith",
      "authorRole": "Customer",
      "createdAt": "2026-08-17T10:00:00Z"
    }
  ],
  "totalCount": 1
}
```

---

## Assignments

### `PUT /api/tickets/{id}/assign` — AssignTicket
Assign a ticket to an active agent.

**Access**: `SupportLead`, `Admin`
**CQRS**: `AssignTicketCommand`
**Rules**: TICKET-R07, TICKET-R08, TICKET-R22

**Request Body**:
```json
{
  "agentId": 7,
  "note": "Assigned to Alex — senior agent on access issues"
}
```

**Response `200 OK`**:
```json
{
  "ticketId": 42,
  "assignedAgent": "Alex Johnson",
  "assignedAt": "2026-08-17T08:30:00Z",
  "ticketStatus": "Assigned"
}
```

**Error `400 Bad Request`** (inactive agent):
```json
{
  "title": "Assignment Rejected",
  "detail": "Agent with ID 7 is not active and cannot receive assignments.",
  "status": 400
}
```

---

### `PUT /api/tickets/{id}/reassign` — ReassignTicket
Reassign a ticket to a different agent (closes prior assignment).

**Access**: `SupportLead`, `Admin`
**CQRS**: `ReassignTicketCommand`
**Rules**: TICKET-R07, TICKET-R08, TICKET-R22

**Request Body**:
```json
{
  "newAgentId": 12,
  "reassignmentReason": "Load balancing — Alex at capacity"
}
```

**Response `200 OK`**:
```json
{
  "ticketId": 42,
  "previousAgent": "Alex Johnson",
  "newAgent": "Maria Garcia",
  "reassignedAt": "2026-08-17T11:00:00Z"
}
```

---

## Customer Portal

### `GET /api/customers/me/tickets` — GetMyCustomerTickets
Paginated list of the current customer's tickets.

**Access**: `Customer`
**CQRS**: `GetMyCustomerTicketsQuery`
**Rules**: TICKET-R01, TICKET-R19, TICKET-R24

**Query Parameters**:

| Param | Type | Description |
|---|---|---|
| `status` | string | Filter: `New`, `InProgress`, `Resolved`, `Closed`, `Cancelled` |
| `priority` | string | Filter: `Low`, `Medium`, `High`, `Critical` |
| `page` | int | Default: 1 |
| `pageSize` | int | Default: 20, Max: 50 |
| `sort` | string | `createdAt_desc` (default), `priority_desc`, `updatedAt_desc` |

**Response `200 OK`**:
```json
{
  "data": [
    {
      "id": 42,
      "title": "Cannot log into the portal",
      "status": "InProgress",
      "priority": "High",
      "category": "Access Issues",
      "createdAt": "2026-08-17T08:00:00Z",
      "updatedAt": "2026-08-17T09:30:00Z"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20
}
```

---

### `GET /api/tickets/{id}/history` — GetTicketHistory
Status/assignment change history for a ticket.

**Access**: `Customer` (own ticket, public events only), `SupportLead`, `Admin`
**CQRS**: `GetTicketHistoryQuery`

**Response `200 OK`**:
```json
{
  "ticketId": 42,
  "history": [
    {
      "changedAt": "2026-08-17T08:00:00Z",
      "event": "Created",
      "changedBy": "Jane Smith",
      "details": "Status set to New"
    },
    {
      "changedAt": "2026-08-17T08:30:00Z",
      "event": "Assigned",
      "changedBy": "Lead User",
      "details": "Assigned to Alex Johnson"
    }
  ]
}
```

---

## Agent Queue

### `GET /api/agents/me/tickets` — GetMyAgentQueue
Get current agent's assigned tickets, ordered by priority.

**Access**: `SupportAgent`
**CQRS**: `GetMyAgentQueueQuery`
**Rules**: TICKET-R02, TICKET-R13, TICKET-R24

**Query Parameters**:

| Param | Type | Description |
|---|---|---|
| `status` | string | Filter by status |
| `slaStatus` | string | Filter: `AtRisk`, `Breached` |
| `page` | int | Default: 1 |
| `pageSize` | int | Default: 20 |

**Response `200 OK`**:
```json
{
  "agentName": "Alex Johnson",
  "data": [
    {
      "id": 42,
      "title": "Cannot log into the portal",
      "status": "InProgress",
      "priority": "High",
      "slaStatus": "AtRisk",
      "slaResolutionDueAt": "2026-08-17T16:00:00Z",
      "customerDisplayName": "Jane Smith",
      "createdAt": "2026-08-17T08:00:00Z"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20
}
```

---

## Admin / Configuration

### `POST /api/admin/sla-policies` — ConfigureSlaPolicy
Create or update an SLA policy.

**Access**: `Admin`
**CQRS**: `ConfigureSlaPolicyCommand`

**Request Body**:
```json
{
  "categoryId": 3,
  "priority": "High",
  "responseTargetMinutes": 120,
  "resolutionTargetMinutes": 480
}
```

**Response `201 Created`**:
```json
{
  "id": 5,
  "category": "Access Issues",
  "priority": "High",
  "responseTargetMinutes": 120,
  "resolutionTargetMinutes": 480,
  "isActive": true
}
```

---

### `GET /api/admin/tickets/unassigned` — GetUnassignedTickets
All tickets currently unassigned, ordered by age.

**Access**: `SupportLead`, `Admin`
**CQRS**: `GetUnassignedTicketsQuery`

**Response `200 OK`**:
```json
{
  "data": [
    {
      "id": 55,
      "title": "Billing discrepancy",
      "priority": "High",
      "ageHours": 4.5,
      "createdAt": "2026-08-17T04:00:00Z"
    }
  ],
  "totalCount": 1
}
```

---

## Reports

### `GET /api/reports/agent-workload` — GetAgentWorkloadReport
Open/active ticket counts per agent.

**Access**: `SupportLead`, `Admin`
**CQRS**: `GetAgentWorkloadReportQuery`

**Response `200 OK`**:
```json
{
  "generatedAt": "2026-08-17T08:15:00Z",
  "agents": [
    {
      "agentId": 7,
      "agentName": "Alex Johnson",
      "team": "Tier 1",
      "openTickets": 8,
      "inProgressTickets": 3,
      "criticalTickets": 1
    }
  ]
}
```

---

### `GET /api/reports/sla-risk` — GetSlaRiskReport
Tickets approaching or past SLA targets.

**Access**: `SupportLead`, `Admin`
**CQRS**: `GetSlaRiskReportQuery`
**Rules**: TICKET-R17

**Response `200 OK`**:
```json
{
  "generatedAt": "2026-08-17T08:15:00Z",
  "atRisk": [
    {
      "ticketId": 42,
      "title": "Cannot log into the portal",
      "priority": "High",
      "slaStatus": "AtRisk",
      "slaResolutionDueAt": "2026-08-17T16:00:00Z",
      "minutesRemaining": 225,
      "assignedAgent": "Alex Johnson"
    }
  ],
  "breached": []
}
```

---

### `GET /api/reports/tickets-by-status` — GetTicketsByStatusReport
Aggregate ticket counts by status and priority.

**Access**: `Admin`
**CQRS**: `GetTicketsByStatusReportQuery`
**Rules**: TICKET-R18 (cancelled excluded from metrics)

**Query Parameters**: `dateFrom`, `dateTo`

**Response `200 OK`**:
```json
{
  "dateRange": { "from": "2026-08-01", "to": "2026-08-17" },
  "byStatus": {
    "New": 12, "Assigned": 8, "InProgress": 15, "Resolved": 34, "Closed": 22
  },
  "byPriority": {
    "Critical": 3, "High": 18, "Medium": 42, "Low": 28
  }
}
```

---

### `GET /api/reports/resolution-time` — GetResolutionTimeReport
Average and median resolution time by agent and category.

**Access**: `Admin`, `SupportLead`
**CQRS**: `GetResolutionTimeReportQuery`

> **Metric Definition**: Resolution time = `ResolvedAt - CreatedAt` in minutes, for tickets with non-null `ResolvedAt` and `Status` of `Resolved` or `Closed`. Cancelled tickets are excluded (TICKET-R18).

**Response `200 OK`**:
```json
{
  "byAgent": [
    {
      "agentId": 7,
      "agentName": "Alex Johnson",
      "averageResolutionMinutes": 340,
      "medianResolutionMinutes": 290,
      "ticketCount": 23
    }
  ],
  "byCategory": [
    {
      "categoryId": 3,
      "categoryName": "Access Issues",
      "averageResolutionMinutes": 420,
      "medianResolutionMinutes": 380,
      "ticketCount": 45
    }
  ]
}
```

---

### `GET /api/reports/high-priority-open` — GetHighPriorityOpenReport
All open `High` and `Critical` tickets.

**Access**: `SupportLead`, `Admin`
**CQRS**: `GetHighPriorityOpenReportQuery`

---

### `GET /api/audit/activity-log` — ActivityLog
Paginated audit of all assignment/status/priority changes.

**Access**: `Admin`
**CQRS**: `GetAuditLogQuery`
**Rules**: TICKET-R21

**Query Parameters**: `entityName`, `entityId`, `actorUserId`, `from`, `to`, `page`, `pageSize`

---

## Error Responses

All errors follow RFC 7807 Problem Details format:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "You do not have permission to access this ticket.",
  "traceId": "00-abc123-def456-00"
}
```

| HTTP Status | Meaning |
|---|---|
| `200 OK` | Successful read |
| `201 Created` | Successful creation |
| `400 Bad Request` | Validation failure |
| `401 Unauthorized` | Missing or invalid JWT |
| `403 Forbidden` | Valid token, insufficient permission |
| `404 Not Found` | Resource does not exist |
| `409 Conflict` | Business rule violation (invalid transition, inactive agent, etc.) |
| `500 Internal Server Error` | Unexpected server error |

---

*TechMaster Academy | Phase 05 Capstone — API Reference v1.0*
