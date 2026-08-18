# Entity Relationship Design — Support Ticketing Platform

## 1. ERD Diagram (Mermaid)

```mermaid
erDiagram
    ApplicationUser {
        string Id PK
        string Email
        string UserName
        string NormalizedEmail
        string PasswordHash
    }

    CustomerProfile {
        int Id PK
        string UserId FK
        string DisplayName
        string Phone
        string Company
        DateTime CreatedAt
    }

    AgentProfile {
        int Id PK
        string UserId FK
        int SupportTeamId FK "nullable"
        string DisplayName
        bool IsActive
        DateTime CreatedAt
    }

    SupportTeam {
        int Id PK
        string Name
        string Description
        DateTime CreatedAt
    }

    TicketCategory {
        int Id PK
        string Name
        string Description
        bool IsActive
    }

    SlaPolicy {
        int Id PK
        int TicketCategoryId FK "nullable"
        string Priority
        int ResponseTargetMinutes
        int ResolutionTargetMinutes
        bool IsActive
    }

    Ticket {
        int Id PK
        string CustomerId FK
        int CategoryId FK
        string AssignedAgentId FK "nullable"
        int? SupportTeamId FK "nullable"
        string Title
        string Description
        TicketPriority Priority
        TicketStatus Status
        SlaStatus SlaStatus
        DateTime CreatedAt
        DateTime UpdatedAt
        DateTime? StartedAt
        DateTime? ResolvedAt
        DateTime? ClosedAt
        DateTime? SlaResponseDueAt
        DateTime? SlaResolutionDueAt
        string? CancellationReason
        string? ResolutionNote
    }

    TicketComment {
        int Id PK
        int TicketId FK
        string AuthorId FK
        string Content
        CommentVisibility Visibility
        DateTime CreatedAt
    }

    TicketAttachmentMetadata {
        int Id PK
        int TicketId FK
        string UploadedByUserId FK
        string FileName
        string StoragePath
        long FileSizeBytes
        string ContentType
        DateTime UploadedAt
    }

    TicketAssignment {
        int Id PK
        int TicketId FK
        string AgentId FK
        string? AssignedByUserId FK
        DateTime AssignedAt
        DateTime? EndedAt
        bool IsActive
        string? AssignmentNote
    }

    TicketStatusHistory {
        int Id PK
        int TicketId FK
        string ChangedByUserId FK
        TicketStatus FromStatus
        TicketStatus ToStatus
        string? Reason
        DateTime ChangedAt
    }

    TicketTag {
        int Id PK
        int TicketId FK
        string TagName
    }

    ActivityLog {
        int Id PK
        string ActorUserId FK
        string EntityName
        string EntityId
        string ActionType
        string? OldValues
        string? NewValues
        DateTime OccurredAt
        string? IpAddress
    }

    ApplicationUser ||--o| CustomerProfile : "has"
    ApplicationUser ||--o| AgentProfile : "has"
    SupportTeam ||--o{ AgentProfile : "contains"
    CustomerProfile ||--o{ Ticket : "opens"
    TicketCategory ||--o{ Ticket : "classifies"
    TicketCategory ||--o{ SlaPolicy : "governs"
    AgentProfile ||--o{ Ticket : "assigned to"
    Ticket ||--o{ TicketComment : "has"
    Ticket ||--o{ TicketAttachmentMetadata : "has"
    Ticket ||--o{ TicketAssignment : "history"
    Ticket ||--o{ TicketStatusHistory : "transitions"
    Ticket ||--o{ TicketTag : "tagged with"
    ApplicationUser ||--o{ TicketAssignment : "assigned by"
    ApplicationUser ||--o{ TicketStatusHistory : "changed by"
    ApplicationUser ||--o{ ActivityLog : "performed by"
```

---

## 2. Entity Descriptions

### `ApplicationUser`
ASP.NET Core Identity user. Extended with `CustomerProfile` or `AgentProfile` via one-to-one navigation. Soft-deletes only — **TICKET-R23**: Deleting a user never destroys historical ticket identity.

### `CustomerProfile`
Holds customer-specific detail (display name, company, phone). Linked to `Ticket.CustomerId`. `Company` is nullable — valid for individual end-users.

### `AgentProfile`
Holds agent-specific data. `IsActive` enforces **TICKET-R07** and **TICKET-R22** — assignment to an inactive agent is rejected. `SupportTeamId` is nullable — agents may be unattached to a team.

### `SupportTeam`
Optional grouping. Tickets can be assigned to a team or directly to an agent. Relationship carries no additional business data; justified as a simple grouping entity.

### `TicketCategory`
Configurable by Admin. `IsActive` allows soft-disabling categories without breaking historical tickets.

### `SlaPolicy`
> **The relationship carries business data** — this is the justified join-adjacent entity. SLA targets (response minutes, resolution minutes) depend on both Priority and Category. Justification: SLA configuration is a policy concern, not a property of Category or Priority alone; it belongs in its own entity to support future matrix configurations (e.g., High-priority Software tickets get 2 h response vs. High-priority Hardware tickets get 4 h).

### `Ticket`
The central aggregate. Key nullable fields:

| Field | Nullable? | Business Reason |
|---|---|---|
| `AssignedAgentId` | ✅ | Ticket starts Unassigned |
| `SupportTeamId` | ✅ | Optional team context |
| `StartedAt` | ✅ | Set once when first moved to InProgress |
| `ResolvedAt` | ✅ | Set by server when resolved — TICKET-R14 |
| `ClosedAt` | ✅ | Set by server when closed — TICKET-R14 |
| `SlaResponseDueAt` | ✅ | Only set when SLA policy exists |
| `SlaResolutionDueAt` | ✅ | Only set when SLA policy exists |
| `CancellationReason` | ✅ | Only populated on cancellation |
| `ResolutionNote` | ✅ | May be required by policy (TICKET-R11) |

### `TicketComment`
`CommentVisibility` enum (`Public` / `Internal`) enforces **TICKET-R03** — internal notes are never returned to customer queries. The author identity comes from the JWT token, never from the request body.

### `TicketAttachmentMetadata`
Stores metadata only (file path, size, type). Actual bytes go to blob storage (stretch goal). **TICKET-R20**: `TicketId` FK ensures an attachment cannot reference a different customer's ticket.

### `TicketAssignment`
> **Carries business data** — `AssignedAt`, `EndedAt`, `IsActive`, and `AssignmentNote` are business-significant. This is the primary justification for using a dedicated join entity rather than a simple FK on `Ticket`. Supports **TICKET-R08** (assignment history preserved) and provides the complete audit of who was assigned and for how long.

**One-active constraint**: Enforced at application layer before each new assignment — the previous active assignment is `EndedAt`-stamped and `IsActive = false` before inserting a new one.

### `TicketStatusHistory`
Immutable append-only log. Answers Business Question #9 ("who changed status/priority and when?"). `FromStatus` + `ToStatus` together validate the transition was legal at point of capture.

### `ActivityLog`
Generic audit table. Captures entity name, entity ID, action type, old/new values (JSON). **TICKET-R21**: Does **not** store comment body text — only structural changes (assignment, status, priority).

---

## 3. Status & Audit Fields Summary

| Entity | Status Field | Audit Fields |
|---|---|---|
| `Ticket` | `Status`, `SlaStatus` | `CreatedAt`, `UpdatedAt`, `StartedAt`, `ResolvedAt`, `ClosedAt` |
| `AgentProfile` | `IsActive` | `CreatedAt` |
| `TicketAssignment` | `IsActive` | `AssignedAt`, `EndedAt` |
| `TicketCategory` | `IsActive` | — |
| `SlaPolicy` | `IsActive` | — |

---

## 4. Unique Constraints

| Table | Constraint | Reason |
|---|---|---|
| `CustomerProfile` | `UNIQUE(UserId)` | One profile per identity user |
| `AgentProfile` | `UNIQUE(UserId)` | One profile per identity user |
| `TicketAssignment` | One row with `IsActive = true` per `TicketId` | Application-enforced |
| `TicketCategory` | `UNIQUE(Name)` | Prevents duplicate category names |
| `SlaPolicy` | `UNIQUE(Priority, TicketCategoryId)` | Single policy per priority/category combination |

---

## 5. Required Indexes

| # | Table | Index Columns | Business Query Served |
|---|---|---|---|
| 1 | `Ticket` | `(CustomerId, CreatedAt)` | TICKET-S02: Customer's own tickets |
| 2 | `Ticket` | `(Status, Priority, CreatedAt)` | TICKET-S06: Agent queue; reports by status/priority |
| 3 | `Ticket` | `(AssignedAgentId)` | TICKET-S06: Assigned ticket lookup |
| 4 | `TicketComment` | `(TicketId, CreatedAt, Visibility)` | TICKET-S07/S08: Comment queries with visibility filter |
| 5 | `TicketAssignment` | `(TicketId, AssignedAt)` | Assignment history ordered by time |
| 6 | `TicketStatusHistory` | `(TicketId, ChangedAt)` | Status audit trail |
| 7 | `Ticket` | `(SlaResponseDueAt, SlaResolutionDueAt)` | TICKET-S13: SLA-at-risk report |
| 8 | `ActivityLog` | `(EntityName, EntityId, OccurredAt)` | TICKET-S17: Filterable audit log |

---

## 6. Delete Behavior

| Relationship | Delete Behavior | Justification |
|---|---|---|
| `ApplicationUser` → `CustomerProfile` | Restrict / Soft delete user | TICKET-R23: preserve historical identity |
| `ApplicationUser` → `AgentProfile` | Restrict / Soft delete user | TICKET-R23: preserve historical identity |
| `Ticket` → `TicketComment` | Cascade (if ticket hard-deleted) | Comments have no meaning without the ticket |
| `Ticket` → `TicketStatusHistory` | Cascade (if ticket hard-deleted) | Audit log tied to ticket lifetime |
| `Ticket` → `TicketAssignment` | Cascade (if ticket hard-deleted) | Assignment history tied to ticket |
| `TicketCategory` → `Ticket` | Restrict | Cannot delete a category with active tickets |
| `AgentProfile` → `TicketAssignment` | Restrict / Deactivate agent | TICKET-R23: preserve assignment history |

> **Note**: In practice, hard deletes are not performed on business entities. Soft-delete (`IsActive = false`, `IsDeleted` flag) is preferred for all workflow entities.

---

## 7. Concurrency-Sensitive Rows

| Row | Risk | Mitigation |
|---|---|---|
| `Ticket` (assignment + status) | Two leads assign the same ticket simultaneously | Optimistic concurrency via `RowVersion` / `xmin` on `Ticket`; application-level check |
| `TicketAssignment` (IsActive) | Race condition creating two active assignments | Atomic check-and-set in `AssignTicketCommandHandler` using a database transaction |

---

*TechMaster Academy | Phase 05 Capstone — ERD Design v1.0*
