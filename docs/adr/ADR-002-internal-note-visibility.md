# ADR-002: Internal Note Visibility Enforcement (RISK-01)

**Date**: 2026-08-17  
**Status**: Accepted  
**Addresses Risk**: RISK-01 — Internal note leaks to customer response

---

## Context

Internal notes (`CommentVisibility.Internal`) must never be returned to customers. This is the highest-priority security rule (TICKET-R03, TICKET-R11). A bug here would directly expose confidential support coordination to end customers.

## Problem

How do we structurally prevent internal notes from appearing in customer-facing API responses, even if a developer makes a mistake?

## Options Considered

| Option | Description | Risk |
|---|---|---|
| **A) Filter in query handler by role** | Handler checks current user's role; applies `.Where(c => c.Visibility == Public)` | Developer must remember to add the filter every time; easy to forget |
| **B) Separate DTOs per role + AutoMapper profiles** | `CustomerConversationDto` never has an `Visibility` field; `StaffConversationDto` includes all | Type system prevents the field from appearing — structural safety |
| **C) Separate API endpoints** | `/api/customers/tickets/{id}/comments` vs `/api/staff/tickets/{id}/comments` | Clearest URL intent, but duplicates logic |

## Decision

**Option B — Role-based DTO separation with Option A as defense-in-depth.**

- `GetTicketConversationQuery` takes a boolean `IncludeInternal` field **set by the handler** based on `ICurrentUserService.Role`, not by the HTTP caller.
- The handler applies `.Where(c => c.Visibility == Public)` when `IncludeInternal == false`.
- AutoMapper maps to `CustomerCommentDto` (no `Visibility` field) for Customer responses, so even if the filter accidentally missed a note, the field would not be serialized.

## Rationale

Defense-in-depth: two independent mechanisms must both fail for a leak to occur. This is appropriate for the highest-risk rule in the domain.

## Verification

TEST-11 covers this: Customer calls `GET /api/tickets/{id}/comments` and asserts that zero internal notes appear in the response body.

## Consequences

Two AutoMapper profiles must be maintained (Customer view, Staff view). When adding new comment fields, developers must consciously decide which profile gets the new field.
