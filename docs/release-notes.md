# Release Notes

## Version 1.0 (Final Release)
**Date:** 2026-08-30

### Sprint 3 - Reporting & Final Integrations
* **New:** Added SearchTickets query for full-text and status-based search.
* **New:** Added GetCustomerTicketHistory for comprehensive customer timelines.
* **New:** Added ConfigureSlaPolicy for Admin-configurable SLAs based on ticket priority.
* **New:** Full suite of Reporting Endpoints (/api/reports/...):
  * Unassigned Tickets
  * Agent Workload
  * Tickets By Status
  * High Priority Open Tickets
  * Resolution Time Analytics
  * SLA Risk Reports
* **Quality:** Reached 100% compliance with unit and integration test coverage.

### Sprint 2 - Core Workflows & Business Rules
* **New:** Advanced Ticket State Machine (CancelTicket, ReopenTicket, ChangeTicketStatus).
* **New:** Support Staff Workflows (AssignTicket, ReassignTicket, SetTicketPriority).
* **New:** Comments System (AddPublicComment, AddInternalNote with visibility restrictions).
* **New:** Customer capabilities (GetMyCustomerTickets).
* **Validation:** FluentValidation rules integrated for all commands.

### Sprint 1 - Foundation
* **New:** Base Clean Architecture & CQRS implementation.
* **New:** JWT Authentication & Role-based Access Control (Admin, SupportLead, Agent, Customer).
* **New:** Basic Ticket Intake (CreateTicket, GetTicketDetails).
* **New:** Database Migrations and Entity Framework Core setup.
* **Docs:** Swagger/Scalar API integration.
