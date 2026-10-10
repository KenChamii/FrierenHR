# 2. Repositories over EF Core rather than DbContext in services

Date: 2026-10-10

Status: Accepted

## Context

`FrierenHR.Application` holds the feature services. They need data. Two options: inject `FrierenHRDbContext` directly, or go through repository interfaces defined in `Application` and implemented in `Infrastructure`.

Injecting the context directly is simpler and EF Core's `DbSet` is already a repository and a unit of work, so a thin wrapper over it is often criticised as a pointless layer.

## Decision

Feature services depend on repository interfaces declared in `FrierenHR.Application.Common.Interfaces`. `FrierenHR.Infrastructure.Repositories` implements them.

The interfaces are per aggregate and name the queries they need (`GetByCompanyAsync`, `GetActiveRulesAsync`, `GetPendingForApproverAsync`) rather than exposing `IQueryable`.

## Consequences

Good:

- `Application` does not reference EF Core, so the dependency direction stays inward and a storage change does not reach the feature services.
- Query intent is named. `GetActiveRulesAsync(companyId, ruleType)` says what it is for, and the fact that it orders by priority lives in one place instead of being repeated at each call site. The rules engine depends on that ordering.
- Company scoping is enforced in the repository rather than remembered at each call site, which matters in a multi-tenant system where a forgotten `WHERE CompanyId` is a data leak.

Costs:

- More files. Every new query is an interface method and an implementation.
- No `IQueryable` escape hatch, so composing a filter at the call site means a new repository method.
- The abstraction is incomplete. `SaveChangesAsync` sits on the repository rather than on a separate unit of work, so two repositories in one request share a context and a transaction implicitly rather than explicitly.

## Alternatives considered

**`DbContext` injected into services.** Fewer files, and honest about the fact that EF Core already is this abstraction. Rejected mainly for the tenant-scoping argument: with the context in hand, every query is one forgotten clause away from returning another company's rows.

**A generic repository only.** `Repository<T>` exists and the per-aggregate repositories extend it, but a generic repository alone pushes query composition back to the services and loses the named intent.
