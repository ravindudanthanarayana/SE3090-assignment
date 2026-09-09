# SmartDesk AI — System Architecture

## 1. One-paragraph summary
SmartDesk AI is a **modular monolith**. A single ASP.NET Core process exposes the public REST API, owns
all business rules and authorization, talks to Neon PostgreSQL through EF Core, and *hosts the Agentic AI
orchestrator in-process*. React is a pure client. Flutter (built later) will be a second pure client on the
same API. There is no second deployable, no message bus, no cache, no container orchestration.

## 2. Runtime topology

```
                    ┌────────────────────────┐
   React (Vercel)   │                        │
   staff / manager  │                        │
   / admin / AI     ├──HTTPS + JWT──────────►│
   approval console │                        │
                    │                        │   ASP.NET Core Web API  (single process)
   Flutter (later)  │                        │   ┌──────────────────────────────────┐
   employee self-   ├──HTTPS + JWT──────────►│   │ Controllers                      │
   service          │                        │   │   ↓  DTOs + validation           │
                    └────────────────────────┘   │ Application Services             │
                                                 │   ↓  business rules (pure C#)    │
                                                 │ Agent Orchestrator ── Agents ────┼──► LLM provider (HTTPS)
                                                 │   ↓        ↑ allow-listed tools  │
                                                 │ EF Core DbContext                │──► Email/notification API
                                                 └──────────────┬───────────────────┘
                                                                ▼
                                                     Neon PostgreSQL (TLS)
```

**Key rule from §2 of the spec:** clients may only talk to ASP.NET Core. Because the orchestrator is a set of
C# classes inside the same process, there is physically no AI endpoint a client could call directly. This is
the simplest possible way to satisfy that rule.

## 3. Projects

```
backend/
  SmartDesk.Domain/          entities, enums, domain constants. No dependencies.
  SmartDesk.Application/     IAppDbContext, DTOs, services, business rules,
                             agents, agent tools, orchestrator, validators. Depends on Domain.
  SmartDesk.Infrastructure/  AppDbContext, EF configurations, migrations, seeding,
                             JWT + password hashing, LLM client, notification client.
  SmartDesk.Api/             controllers, middleware, DI wiring, Swagger, CORS, Program.cs.
  SmartDesk.Tests/           xUnit: unit, integration, agent evaluation, E2E.
frontend/                    Vite + React + TypeScript + Tailwind.
docs/                        this folder + ADRs.
perf/                        k6 script.
.github/workflows/           CI.
```

Five projects. There is **no repository per entity**: `SmartDesk.Application` defines an
`IAppDbContext` interface (the `DbSet<T>`s the services use, plus `SaveChangesAsync`,
`BeginTransactionAsync` and `Database`), and `AppDbContext` in Infrastructure implements it. That
interface *is* the "suitable data-access abstraction" the spec asks for: it is injected, it inverts
the layer dependency, and it is substituted by the EF Core in-memory provider in tests. Adding
`ITicketRepository` over `DbSet<Ticket>` would be an abstraction over an abstraction — recorded and
defended in ADR-006.

## 4. Request pipeline
1. **CORS** policy (explicit allowed origins from config).
2. **Exception middleware** → RFC 7807 `ProblemDetails`, correlation id, logged; never leaks stack traces in Production.
3. **Authentication** — JWT bearer, validated issuer/audience/lifetime/signing key.
4. **Authorization** — `[Authorize(Roles=...)]` for coarse checks, plus explicit *resource ownership* checks in the service layer (an Employee may only read their own tickets — a role attribute cannot express that).
5. **Controller** → validate DTO → call service → map to response DTO → correct status code.
6. **Service** → business rules → EF Core → `AuditLog` row → optional notification.

## 5. Cross-cutting concerns
| Concern | Approach |
|---|---|
| Validation | DataAnnotations on DTOs + explicit guard clauses in services that throw typed domain exceptions |
| Errors | `ValidationException` → 400, `NotFoundException` → 404, `ForbiddenException` → 403, `ConflictException` → 409, everything else → 500 |
| Logging | Built-in `ILogger` with structured scopes (`WorkflowId`, `TicketId`, `UserId`) |
| Audit | `IAuditService.LogAsync(entityType, entityId, action, actor, details)` called from services |
| Config/secrets | `appsettings.json` holds **no secrets**; `JWT_SECRET`, `DATABASE_CONNECTION_STRING`, `AI_API_KEY`, `NOTIFICATION_API_KEY` come from env vars / user-secrets. `.env` is git-ignored. |
| Time | `IClock` interface so SLA and timeout logic is deterministic in tests |

## 6. Why this architecture (viva answers)
- **Why a modular monolith?** One codebase, one deploy, one database, one transaction boundary. Four students can still own four folders. Microservices would add network failure modes we get no marks for.
- **Why is the AI in-process?** The spec forbids clients calling the AI service directly. In-process makes that structurally impossible, removes a deployment, and lets the orchestrator use the same EF transaction and the same authorization context as the rest of the app.
- **What happens when the LLM fails?** The orchestrator catches, retries with a bounded limit, then marks the workflow `Failed` with a recorded reason — a *safe, auditable failure*, which §9.1 explicitly asks for. Ticket creation itself never fails because of the AI.
