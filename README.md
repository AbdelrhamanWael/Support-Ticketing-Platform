# Support Ticketing Platform

An enterprise-grade ticketing platform API built with Clean Architecture, CQRS (MediatR), and ASP.NET Core.

## Project Management
* **Jira Board (Scrum):** [View Jira Backlog & Active Sprints](https://abdelrhamanwael8.atlassian.net/jira/software/projects/KAN/list?jql=project+%3D+KAN+ORDER+BY+cf%5B10019%5D+ASC&atlOrigin=eyJpIjoiY2I2MjNjMDcwYmJiNGI4ZDgzMWFhNDZjNzg0YmExNjciLCJwIjoiaiJ9)

## Entity Relationship Diagram
![Entity Relationship Diagram](docs/erd-diagram.png)

*For the full database schema rules and relationships, see [docs/erd-design.md](docs/erd-design.md).*

## Features (Sprint 1 Release - Task 03)
- **Clean Architecture:** Domain, Application, Infrastructure, and API layers strictly separated.
- **CQRS Pattern:** MediatR implementation for business logic separation (`RegisterCustomerCommand`, `LoginQuery`, `CreateTicketCommand`).
- **Authentication & Security:** ASP.NET Core Identity integrated with JWT Bearer tokens.
- **Global Exception Handling:** Custom middleware ensuring consistent JSON error responses across the API.
- **Modern API Docs:** Scalar API Reference utilized for interactive endpoint testing instead of Swagger.

## Getting Started
1. Set `SupportTicketingPlatform.API` as the startup project.
2. Run `dotnet restore` and `dotnet build`.
3. Apply Entity Framework migrations (if not already applied) or ensure MSSQL is running locally.
4. Run the API and navigate to `http://localhost:<port>/scalar/v1` to access the interactive API documentation.
