# ADR-001: Use Clean Architecture with CQRS

**Date**: 2026-08-17  
**Status**: Accepted  
**Decision Makers**: Team

---

## Context

We need to design the backend architecture for the Support Ticketing Platform API. The system has complex business rules (status workflows, assignment boundaries, SLA calculations, role-based access), multiple reporting requirements, and a mandate from the capstone spec to implement CQRS.

## Options Considered

| Option | Pros | Cons |
|---|---|---|
| **A) Clean Architecture + CQRS with MediatR** | Clear separation of concerns; handlers are testable in isolation; read/write separation enables projection optimizations | Higher initial setup cost; more files/folders than a simple layered architecture |
| **B) Simple N-Layer (Controllers → Services → Repositories)** | Familiar, faster to scaffold | Business logic tends to leak into controllers or services; harder to test in isolation |
| **C) Vertical Slice Architecture** | Each feature is self-contained | Less structure for shared concerns (auth, SLA, audit); harder to enforce architectural rules for a team |

## Decision

**Option A — Clean Architecture + CQRS with MediatR.**

## Rationale

- The capstone explicitly requires CQRS. MediatR is the standard .NET implementation.
- Clean Architecture's dependency inversion rule prevents Infrastructure (EF Core) from being referenced by Application or Domain, making domain logic and handlers unit-testable without a database.
- The Result pattern replaces exception-based control flow for business failures, making handler behavior explicit and testable.
- The separation of Commands (write) and Queries (read) allows query handlers to use projections (`Select` / `ProjectTo`) without loading full entity graphs, solving TICKET-R13's sorting and RISK-08's performance concerns.

## Consequences

- All business logic lives in Application layer handlers. Controllers are thin dispatchers.
- Adding a new use case means adding a Command or Query class, a Validator, and a Handler — a predictable, repeatable pattern the whole team can follow.
- Infrastructure (EF Core) can be swapped without touching Domain or Application.
