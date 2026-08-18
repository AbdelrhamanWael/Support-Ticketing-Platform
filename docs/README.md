# 🎫 TechMaster Support Ticketing Platform API

> **Phase 05 — Enterprise Team Capstone**
> A production-grade SaaS support backend built with **ASP.NET Core**, **Clean Architecture**, and **CQRS**.

---

## 📋 Table of Contents

- [Project Overview](#project-overview)
- [Technology Stack](#technology-stack)
- [Architecture Summary](#architecture-summary)
- [Roles & Security Boundaries](#roles--security-boundaries)
- [Quick Start](#quick-start)
- [Documentation Index](#documentation-index)
- [Sprint Roadmap](#sprint-roadmap)
- [Team Conventions](#team-conventions)

---

## Project Overview

The **Support Ticketing Platform API** moves customer support out of scattered chat messages into a structured, auditable system. It enables:

- **Customers** to open tickets, track their issues, and communicate with support agents.
- **Support Agents** to manage their assigned queue, add comments, and update ticket status.
- **Support Leads** to assign/reassign tickets, monitor team workload, and escalate issues.
- **Admins** to manage users, configure SLA policies, and generate operational reports.

### Core Business Value

| Capability | Description |
|---|---|
| **Ticket Lifecycle** | Structured `New → Assigned → InProgress → Resolved → Closed` workflow with strict transition rules |
| **Role Enforcement** | Every endpoint enforces ownership boundaries — customers see only their data |
| **SLA Tracking** | Configurable response/resolution targets with `OnTrack → AtRisk → Breached` states |
| **Audit Trail** | Every status/assignment/priority change is recorded with actor identity and timestamp |
| **Reporting** | Backlog, workload, resolution time, and SLA-at-risk reports for leadership |

---

## Technology Stack

| Layer | Technology |
|---|---|
| **Runtime** | .NET 8 / ASP.NET Core Web API |
| **Architecture** | Clean Architecture (Domain → Application → Infrastructure → API) |
| **Pattern** | CQRS with MediatR |
| **ORM** | Entity Framework Core 8 |
| **Database** | SQL Server (LocalDB for dev, Azure SQL / Docker for staging) |
| **Auth** | ASP.NET Core Identity + JWT Bearer Tokens |
| **Validation** | FluentValidation |
| **Mapping** | AutoMapper |
| **API Docs** | Swashbuckle (Swagger/OpenAPI 3.0) |
| **Testing** | xUnit + Moq + FluentAssertions |
| **Logging** | Serilog |

---

## Architecture Summary

The solution follows **Clean Architecture** with four layers:

```
TechMasterCapstone/
├── src/
│   ├── Domain/                  # Entities, Enums, Domain Events, Value Objects
│   ├── Application/             # CQRS Commands, Queries, DTOs, Interfaces
│   ├── Infrastructure/          # EF Core, Repositories, Identity, Services
│   └── API/                     # Controllers, Middleware, Program.cs
├── tests/
│   ├── Domain.Tests/
│   ├── Application.Tests/
│   └── Integration.Tests/
├── docs/                        # This documentation folder
└── TechMasterCapstone.sln
```

> See [`architecture-guide.md`](./architecture-guide.md) for the full layered breakdown and dependency rules.

---

## Roles & Security Boundaries

| Role | Business Responsibility | Security Scope |
|---|---|---|
| **Admin** | Manage users, configuration, all tickets, all reports | Global — unrestricted |
| **SupportLead** | Assign/reassign tickets, review team queues, escalations | Team-wide — cannot administer system users |
| **SupportAgent** | Work assigned tickets, add comments, update allowed statuses | Assigned resources only |
| **Customer** | Create tickets, view own tickets, add public comments | Own tickets only — never sees internal notes |

---

## Quick Start

### Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB or Docker)
- Visual Studio 2022 / VS Code / Rider

### 1. Clone & Restore

```bash
git clone <repo-url>
cd TechMasterCapstone
dotnet restore
```

### 2. Configure the Database

Edit `src/API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TechMasterTicketing;Trusted_Connection=True;"
  },
  "JwtSettings": {
    "SecretKey": "your-256-bit-secret-key-here",
    "Issuer": "TechMasterTicketing",
    "Audience": "TechMasterTicketingClient",
    "ExpiryMinutes": 60
  }
}
```

### 3. Run Migrations & Seed Data

```bash
cd src/API
dotnet ef database update
```

### 4. Launch the API

```bash
dotnet run --project src/API
```

Swagger UI will be available at: **`https://localhost:7001/swagger`**

---

## Documentation Index

| Document | Description |
|---|---|
| [`architecture-guide.md`](./architecture-guide.md) | Layer structure, dependency rules, CQRS pattern |
| [`erd-design.md`](./erd-design.md) | Entity Relationship Diagram, constraints, indexes |
| [`api-reference.md`](./api-reference.md) | All endpoints, request/response shapes, auth |
| [`business-rules.md`](./business-rules.md) | TICKET-R01 through TICKET-R24 with enforcement points |
| [`cqrs-mapping.md`](./cqrs-mapping.md) | Command/Query catalog with handler ownership |
| [`user-stories.md`](./user-stories.md) | Full backlog with acceptance criteria |
| [`status-workflow.md`](./status-workflow.md) | State machine diagrams and transition rules |
| [`testing-guide.md`](./testing-guide.md) | Unit, integration, and acceptance tests |
| [`adr/`](./adr/) | Architecture Decision Records |
| [`sla-design.md`](./sla-design.md) | SLA policy engine, calculation logic, at-risk report |

---

## Sprint Roadmap

| Sprint | Focus | Exit Demo Criteria |
|---|---|---|
| **Sprint 1** | Ticket Intake, Triage, Assignment | Auth working; DB stable; 2+ epics with end-to-end CQRS merged to `main` |
| **Sprint 2** | Conversation, Status Workflow, SLA | Main business workflow with role/ownership protection and tests passing |
| **Sprint 3** | Customer Portal, Analytics, Hardening | Reports complete; regression tests green; deployment evidence; final docs |

---

## Team Conventions

### Git Flow

```
main          ← production-ready only
develop       ← integration branch
feature/TICKET-xxx-short-description   ← per story
```

### Pull Request Rules

- Every PR must link a Jira ticket key (e.g., `TICKET-101`)
- Migrations must be coordinated through Jira and reviewed by the whole team before merging
- Shared contracts (DTOs, interfaces, enums) require team sign-off

### Naming Conventions

| Artifact | Convention | Example |
|---|---|---|
| Command | `VerbNounCommand` | `CreateTicketCommand` |
| Query | `GetNounQuery` | `GetMyAgentQueueQuery` |
| Handler | `CommandName/QueryNameHandler` | `CreateTicketCommandHandler` |
| DTO | `NounDto` / `NounRequest` | `TicketDto`, `CreateTicketRequest` |
| Controller | `ResourceController` | `TicketsController` |

---

*TechMaster Academy | ASP.NET Backend Career Training | Phase 05 Enterprise Capstone*
