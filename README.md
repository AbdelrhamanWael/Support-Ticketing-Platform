# TechMaster Support Ticketing Platform

An enterprise-grade support ticketing API built with Clean Architecture, CQRS (MediatR), and ASP.NET Core.

## ðŸ“‹ Project Overview
The Support Ticketing Platform is a SaaS backend designed to move customer support out of scattered emails into a structured, auditable system. It supports strict Service Level Agreements (SLAs), role-based ownership, and complete lifecycle tracking.

## âš™ï¸ Technology Stack
- **Framework:** .NET 8 / ASP.NET Core Web API
- **Architecture:** Clean Architecture + CQRS
- **Database:** Entity Framework Core (SQL Server)
- **Authentication:** ASP.NET Core Identity + JWT Bearer
- **Validation:** FluentValidation
- **Docs:** Scalar OpenAPI

## ðŸ”’ Roles & Security
| Role | Permissions |
|---|---|
| **Admin** | Manage users, configure SLA policies, access all reports, modify any ticket. |
| **SupportLead** | Assign tickets, view team performance, escalate issues. |
| **Agent** | View assigned queue, add internal notes, change ticket status. |
| **Customer** | Create tickets, view own history, add public comments. |

## ðŸ“ˆ API Endpoints Summary
| Module | Endpoints |
|---|---|
| **Auth** | POST /api/auth/register, POST /api/auth/login |
| **Tickets (Customer)** | POST /api/tickets, GET /api/customers/me/history |
| **Tickets (Agent)** | GET /api/agents/me/tickets, PUT /api/tickets/{id}/status, POST /api/comments/internal |
| **Admin/Lead** | PUT /api/tickets/{id}/assign, POST /api/admin/sla-policies |
| **Reports** | GET /api/reports/sla-risk, GET /api/reports/agent-workload, etc. |

## ðŸš€ How to Run Locally
1. Clone the repository and navigate to the root directory.
2. Ensure you have .NET 8 SDK installed.
3. Update the ppsettings.json connection string if necessary (defaults to localdb).
4. Run migrations: dotnet ef database update --project src\SupportTicketingPlatform.Infrastructure --startup-project src\SupportTicketingPlatform.API
5. Run the API: dotnet run --project src\SupportTicketingPlatform.API
6. Access Scalar Docs at: http://localhost:<port>/scalar/v1

## âœ… Running Tests
The project includes a comprehensive suite of Unit and Integration tests.
`ash
# Run all tests
dotnet test
`

## ðŸ“‚ Documentation Index
- [Entity Relationship Diagram (ERD)](docs/erd-design.md)
- [Architecture Guide](docs/architecture-guide.md)
- [Release Notes](docs/release-notes.md)
- [Postman Collection](postman-collection.json)

## ðŸŒ Deployment
[Live API Documentation (Scalar)](http://support-ticketing-api.runasp.net/scalar/v1)