# Jira Project Setup — Support Ticketing Platform API

> **Project Key**: `TICKET`
> **Project Type**: Scrum
> **Team Size**: 3 Members
> **Total Sprints**: 3 (2-week sprints)

---

## 1. Team Roles

| Member | Role | Primary Ownership | Secondary |
|---|---|---|---|
| **Member A** | Backend Lead | Ticket Intake · Triage · Assignment · Architecture | Code Review, DB migrations |
| **Member B** | Full Stack Dev 1 | Conversation · Status Workflow · SLA & Escalation | Reports support |
| **Member C** | Full Stack Dev 2 | Customer Portal · Support Analytics · Audit | Auth, seeding |

> **Capstone Rule**: Each engineer owns at least one complete vertical use case, reviews code outside their own epic, and understands the shared architecture.

---

## 2. Epic Ownership Map

| Epic | Epic Key | Owner | Stories |
|---|---|---|---|
| 🎟️ Ticket Intake | EPIC-TI | **Member A** | TICKET-101, 102, 118 |
| 🔍 Triage | EPIC-TR | **Member A** | TICKET-103 |
| 📋 Assignment | EPIC-AS | **Member A** | TICKET-104, 105, 106 |
| 💬 Conversation | EPIC-CV | **Member B** | TICKET-107, 108, 109 |
| 🔄 Status Workflow | EPIC-SW | **Member B** | TICKET-110, 111, 112 |
| ⏱️ SLA & Escalation | EPIC-SLA | **Member B** | TICKET-113 |
| 🧑 Customer Portal | EPIC-CP | **Member C** | TICKET-102 (portal view), TICKET-118 |
| 📊 Support Analytics | EPIC-SA | **Member C** | TICKET-114, 115, 116 |
| 🔐 Audit | EPIC-AU | **Member C** | TICKET-117 |

---

## 3. Full Story Backlog with Assignments

### Sprint 1 — Foundation & Core Workflow

| Jira Key | Epic | Story | SP | Assignee | Dependencies |
|---|---|---|---|---|---|
| TICKET-101 | EPIC-TI | Create a ticket | 3 | **Member A** | Auth endpoints, DB migration |
| TICKET-102 | EPIC-TI | View my tickets (Customer) | 5 | **Member C** | TICKET-101 |
| TICKET-103 | EPIC-TR | Set category and priority | 3 | **Member A** | TICKET-101 |
| TICKET-104 | EPIC-AS | Assign a ticket to an active agent | 2 | **Member A** | TICKET-101, AgentProfile seed |
| TICKET-105 | EPIC-AS | Reassign a ticket | 5 | **Member A** | TICKET-104 |
| TICKET-106 | EPIC-AS | View my assigned queue (Agent) | 3 | **Member B** | TICKET-104 |

**Sprint 1 Total**: 21 Story Points  
**Sprint 1 Exit Demo**: Auth working · DB stable · 2+ epics with CQRS merged to `main`

---

### Sprint 2 — Business Rules & Conversation

| Jira Key | Epic | Story | SP | Assignee | Dependencies |
|---|---|---|---|---|---|
| TICKET-107 | EPIC-CV | Add public comment (Customer) | 5 | **Member B** | TICKET-101 |
| TICKET-108 | EPIC-CV | Add public reply (Agent) | 5 | **Member B** | TICKET-106 |
| TICKET-109 | EPIC-CV | Add internal note | 3 | **Member B** | TICKET-106 |
| TICKET-110 | EPIC-SW | Move ticket to InProgress | 3 | **Member B** | TICKET-104 |
| TICKET-111 | EPIC-SW | Resolve a ticket | 5 | **Member B** | TICKET-110 |
| TICKET-112 | EPIC-SW | Close or reopen | 2 | **Member C** | TICKET-111 |

**Sprint 2 Total**: 23 Story Points  
**Sprint 2 Exit Demo**: Full happy path works end-to-end · Role/ownership protection and tests passing

---

### Sprint 3 — Analytics, SLA, Audit, Hardening

| Jira Key | Epic | Story | SP | Assignee | Dependencies |
|---|---|---|---|---|---|
| TICKET-113 | EPIC-SLA | Identify SLA-at-risk tickets | 3 | **Member B** | TICKET-101, SLA seed data |
| TICKET-114 | EPIC-SA | View agent workload | 5 | **Member C** | TICKET-104 |
| TICKET-115 | EPIC-SA | View tickets by status/priority | 3 | **Member C** | TICKET-101 |
| TICKET-116 | EPIC-SA | View resolution time metrics | 2 | **Member C** | TICKET-111 |
| TICKET-117 | EPIC-AU | Audit assignment/status changes | 5 | **Member C** | TICKET-104, 110 |
| TICKET-118 | EPIC-TI | Cancel a ticket | 3 | **Member A** | TICKET-101 |

**Sprint 3 Total**: 21 Story Points  
**Sprint 3 Exit Demo**: All reports complete · Regression tests green · Deployment evidence · Final docs

---

## 4. Cross-Cutting Responsibilities

These tasks are shared and coordinated via Jira subtasks or Tech Tasks (not user stories):

| Task | Owner | Jira Type |
|---|---|---|
| DB Schema & Initial Migrations | **Member A** | Tech Task |
| JWT Auth + Role Seeding | **Member A** | Tech Task |
| MediatR + FluentValidation setup | **Member A** | Tech Task |
| AutoMapper profiles (Customer vs Staff) | **Member B** | Tech Task |
| SLA Calculation Service | **Member B** | Tech Task |
| Serilog / ActivityLog infrastructure | **Member C** | Tech Task |
| Seed Data (Categories, SLA Policies, Users) | **Member C** | Tech Task |
| Swagger/OpenAPI configuration | **Member C** | Tech Task |
| Postman collection + evidence | **All** | Tech Task |

---

## 5. Code Review Rules

| Rule | Detail |
|---|---|
| **PR must link Jira key** | PR title: `[TICKET-101] Create Ticket endpoint` |
| **No self-merge** | Every PR requires approval from at least 1 other member |
| **DB migration PRs** | Must be reviewed by Member A (Backend Lead) before merging |
| **Shared contract changes** | DTOs, interfaces, enums — require all 3 members to acknowledge |
| **Sprint branch merge gate** | All stories must be merged to `develop` before sprint demo |

---

## 6. Jira Board Configuration

### Columns (Scrum Board)

```
┌──────────┬──────────────────┬─────────────┬──────────────┬──────────┐
│ Backlog  │   In Progress    │  In Review  │    Testing   │   Done   │
│          │                  │  (PR Open)  │  (Postman)   │          │
└──────────┴──────────────────┴─────────────┴──────────────┴──────────┘
```

### WIP Limits (recommended)

| Column | Limit |
|---|---|
| In Progress | 2 per member (max 6 total) |
| In Review | 3 total |

---

## 7. Jira Custom Fields

Add these custom fields to your Jira stories for tracking:

| Field | Type | Values |
|---|---|---|
| `Business Rule` | Text | TICKET-R01, TICKET-R02, ... |
| `CQRS Type` | Select | Command, Query |
| `Risk Reference` | Text | RISK-01 through RISK-08 |
| `Test Coverage` | Select | Unit, Integration, Acceptance, Not Required |

---

## 8. Definition of Done (DoD)

A story is **Done** when:

- [ ] Code merged to `develop` via approved PR
- [ ] FluentValidation rules written and tested
- [ ] Unit test(s) for handler written (if applicable)
- [ ] Negative test(s) verified in Postman or xUnit
- [ ] Swagger endpoint documented (shows correct request/response)
- [ ] Business rule enforcement verified (see `business-rules.md`)
- [ ] No known regressions on previously passing tests
- [ ] Jira ticket moved to Done column

---

## 9. Sprint Ceremonies

| Ceremony | When | Duration | Facilitator |
|---|---|---|---|
| Sprint Planning | Start of Sprint | 1 hour | Member A (Lead) |
| Daily Standup | Every day | 15 min | Rotate |
| Sprint Review / Demo | End of Sprint | 30 min | Story Owner |
| Retrospective | End of Sprint | 30 min | Rotate |

### Standup Format

1. **What did I do yesterday?** (Jira key reference)
2. **What will I do today?** (Jira key reference)
3. **Any blockers?** (flag immediately, don't wait)

---

## 10. Risk Register — Jira Issue Links

Each Engineering Risk from the capstone spec maps to a Jira issue:

| Risk | Description | Jira Issue | Owner | Status |
|---|---|---|---|---|
| RISK-01 | Internal note leaks to customer | TICKET-RISK-01 | Member B | ADR-002 written |
| RISK-02 | Agent accesses unassigned ticket | TICKET-RISK-02 | Member A | TEST-06 covers this |
| RISK-03 | Assignment history lost on reassign | TICKET-RISK-03 | Member A | ADR-003 written |
| RISK-04 | Invalid status change bypasses workflow | TICKET-RISK-04 | Member B | ADR-004 written |
| RISK-05 | Resolution time metric inconsistent | TICKET-RISK-05 | Member C | ADR-005 written |
| RISK-06 | SLA ignores business hours | TICKET-RISK-06 | Member B | ADR-006 (wall-clock v1.0) |
| RISK-07 | Closed ticket still accepts comments | TICKET-RISK-07 | Member B | TEST-07 covers this |
| RISK-08 | Ticket list degrades without indexes | TICKET-RISK-08 | Member A | Indexes in ERD design |

---

## 11. Stretch Epic (Sprint 3+ only)

Only pull these stories after the mandatory D2 release criteria are stable:

| Story | Owner | Jira Label |
|---|---|---|
| Business-Hour SLA calculation | Member B | `stretch` |
| Background breach notifications | Member B | `stretch` |
| Customer satisfaction rating | Member C | `stretch` |
| Tags and advanced search | Member C | `stretch` |
| Attachment file storage | Member A | `stretch` |

---

*TechMaster Academy | Phase 05 Capstone — Jira Setup v1.0*
