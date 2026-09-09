# ADR-006: Data access — IAppDbContext, not a repository per entity

**Status:** Accepted · **Date:** 2026-09

## Context

Spec §5 requires "controllers, DTOs, service/application layer, **suitable data-access abstraction**
and dependency injection". The standard textbook reading of that phrase is a repository interface
per aggregate — `ITicketRepository`, `IKnowledgeRepository`, and so on — usually with a unit-of-work
on top.

There is also a structural constraint. Our services live in `SmartDesk.Application` and the
`DbContext` lives in `SmartDesk.Infrastructure`, which references Application. A service cannot
depend on the concrete `AppDbContext` without inverting that dependency.

## Options considered

**A repository per entity, plus a unit of work.** The conventional answer. Our objection is that
`DbSet<T>` is already a repository and `DbContext` is already a unit of work; wrapping them
reproduces that API with fewer capabilities. Concretely, our ticket query applies search, five
filters, allow-listed sorting and pagination — expressing that behind a repository means either a
method per combination, or leaking `IQueryable` back out, at which point the abstraction has stopped
abstracting. It would also add roughly ten interfaces and ten implementations that four students
would have to read before finding the actual logic.

**Services depending on `AppDbContext` directly.** Simplest, but it forces `Application` to
reference `Infrastructure`, inverting the layering, and makes the services harder to test in
isolation.

**An `IAppDbContext` interface in Application, implemented by `AppDbContext` in Infrastructure.**
Chosen.

## Decision

`IAppDbContext` declares the `DbSet<T>` properties the services use, plus `SaveChangesAsync`,
`BeginTransactionAsync` and `Database`. `AppDbContext` implements it. Services depend on the
interface.

## Consequences

**Good.** The dependency arrow points the right way — Application knows nothing about Npgsql or
migrations. Services are testable against the EF Core in-memory provider with no mocking framework,
which is how the 39 business-rule and 18 ticket-service tests run in about two seconds. LINQ
composition stays available, so the search-filter-sort-paginate query is one readable method rather
than a repository API. There is one obvious place to look for data access.

**Bad.** `IAppDbContext` is an EF Core-shaped interface, so swapping to a non-EF persistence layer
would mean rewriting the services, not just the implementation. We judged that acceptable: the spec
mandates EF Core with the PostgreSQL provider, so provider-swapping is not a requirement we are
being marked against, and designing for a migration that cannot happen is speculative.

**How to defend this at the viva.** The requirement is a *suitable* abstraction, not specifically a
repository pattern. `IAppDbContext` is an abstraction: it is an interface, it is injected, it
inverts the dependency, and it is substituted in tests. Adding `ITicketRepository` on top of
`DbSet<Ticket>` would be an abstraction over an abstraction, and we should be able to say why we
chose not to.
