# ADR-004: PostgreSQL hosting — Neon

**Status:** Accepted · **Date:** 2026-09

## Context

Spec §2 mandates PostgreSQL and §14 requires it deployed securely with migrations, restricted
credentials and initialisation instructions. Critically, §14 also states the assignment must be
completable using institution-provided or **no-cost** services, and that the deployment must stay
accessible to evaluators until 21 October 2026 — three weeks after submission.

## Options considered

**Supabase.** Also free Postgres, with extras we do not need (auth, storage, realtime). Its free
tier pauses a project after a week of inactivity, which is a genuine risk for a database that must
still answer an evaluator in three weeks' time.

**Azure Database for PostgreSQL.** Available through student credit, but the credit expires, and an
expired subscription during the evaluation window is exactly the failure §14 warns about.

**A container on the API host.** Cheapest, but storage on free tiers is usually ephemeral, so a
redeploy would wipe the demo data.

**Neon.** Chosen.

## Decision

Neon serverless PostgreSQL, free tier, connected over TLS with `SSL Mode=Require`.

Schema is owned by EF Core migrations, applied automatically at API startup by `DbSeeder`, which is
idempotent. Seeding is likewise idempotent — it inserts only what is missing.

## Consequences

**Good.** Real PostgreSQL, so `jsonb`, `text[]`, check constraints and partial indexes all behave
exactly as they do locally. No card required and no expiry. Database branching is available if we
ever want a separate branch per feature.

**Bad, and mitigated.** Neon scales an idle compute to zero, so the first request after a quiet
period pays a cold-start of a second or two. Two consequences follow, and both are already handled
in the code:

1. `EnableRetryOnFailure(3)` is configured on the Npgsql provider, so a dropped idle connection is
   retried rather than surfacing as a 500.
2. Because a retrying execution strategy refuses a hand-rolled transaction, the approval-execution
   path wraps its transaction in `Database.CreateExecutionStrategy().ExecuteAsync(...)`. This is not
   incidental — it is a direct consequence of choosing a serverless database, and it is worth being
   able to explain at the viva.

Network latency is higher than a local database, which is why the k6 performance report should state
whether it was run against Neon or locally.

**Operational note.** Create a dedicated role for the application rather than using the owner role,
and never commit the connection string — it lives only in `DATABASE_CONNECTION_STRING`.
