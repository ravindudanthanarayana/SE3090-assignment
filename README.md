# SmartDesk AI

An Agentic AI powered IT help desk and support management system.

Employees raise IT support tickets. A five-agent AI workflow classifies each ticket, searches the
knowledge base, recommends an owner and checks SLA risk — then **pauses and waits for a human
manager** before doing anything high-impact. Support agents work the queue, managers approve or
reject the AI's recommendations, and every step is auditable.

Built for **SE3090 – Software Engineering Frameworks, Assignment 1**.

---

## 1. Contents

| Section | |
|---|---|
| [Business problem and roles](#2-business-problem-and-roles) | what the system does and who uses it |
| [Technology and why](#3-technology-choices) | stack with justification |
| [Architecture](#4-architecture) | how the pieces fit together |
| [The Agentic AI subsystem](#5-the-agentic-ai-subsystem) | the five agents, their tools and the approval gate |
| [Running it locally](#6-running-it-locally) | prerequisites, environment variables, startup order |
| [Test accounts](#7-test-accounts) | demo credentials |
| [Testing](#8-testing) | what is tested and how to run it |
| [API documentation](#9-api-documentation) | Swagger, endpoint list |
| [Deployment](#10-deployment) | Neon, API host, Vercel |
| [Repository structure](#11-repository-structure) | where everything lives |
| [Security](#12-security-considerations) | what is protected and how |
| [Individual contributions](#13-individual-contributions) | who owns what |
| [AI usage declaration](#14-ai-usage-declaration) | required by spec section 18 |

Detailed design documents live in [`docs/`](./docs).

---

## 2. Business problem and roles

An internal IT help desk has more requests than it has people. Tickets arrive as unstructured text,
get mis-categorised, sit unassigned, and quietly breach their service-level agreement. SmartDesk AI
addresses that by doing the triage work automatically — while keeping a human in charge of anything
that actually changes ownership or escalates.

### Roles

| Role | Can do |
|---|---|
| **Employee** | Raise tickets, view and comment on their own tickets, track status and history, read published knowledge articles |
| **SupportAgent** | Everything above, plus work the tickets assigned to them: change status, add internal notes, record a resolution, see the SLA queue |
| **SupportManager** | Everything above, plus assign and reassign tickets, escalate, view all tickets, run reports, and **approve or reject AI recommendations** |
| **Admin** | Everything above, plus manage users and roles, ticket categories and agent skills |

### The four business components

Each is owned by one student (spec section 3).

| Component | Owns | Business operation beyond CRUD |
|---|---|---|
| **A. Ticket Management** | tickets, comments, history, categories | validated status-transition machine |
| **B. Assignment & Workload** | assignments, agent skills, workload | deterministic skill-vs-workload assignment scoring |
| **C. Knowledge Base** | knowledge articles, ticket–article links | ticket-aware article relevance ranking |
| **D. SLA, Escalation & Reporting** | SLA state, escalation, dashboards, approvals | SLA-risk-driven escalation, approval-gated |

---

## 3. Technology choices

| Layer | Choice | Why |
|---|---|---|
| Backend | ASP.NET Core 10 Web API (C#) | Required by the module. One process owns all business rules, so both clients get identical behaviour. |
| Data access | EF Core 10 + Npgsql | Required. Migrations give us a reviewable, replayable schema history. |
| Database | PostgreSQL on **Neon** | Free tier, no card, real Postgres. `jsonb` lets us store varying agent state without inventing six tables. |
| Web | React 19 + Vite + TypeScript | Required. TypeScript because the API surface is large and the compiler catches DTO drift for free. |
| Design system | Semantic CSS custom properties + Tailwind 4 `@theme inline` | One token set (`bg-surface`, `text-fg`, `border-line`, `bg-accent-solid`) shared by the marketing site, auth and the workspace. Dark mode is a token swap, not a sweep of `dark:` overrides. |
| Web state | **Context API** | Identity plus the JWT is the only genuinely global client state. Redux would be ceremony without benefit — see [ADR-001](./docs/ADRs/ADR-001-react-state-management.md). |
| Styling | Tailwind CSS 4 | No separate stylesheet to keep in sync; responsive breakpoints inline. |
| Agentic AI | **Custom C# orchestrator, in-process** | Spec section 2 permits a custom approach and forbids clients calling the AI directly. In-process makes that structurally impossible — see [ADR-002](./docs/ADRs/ADR-002-agentic-ai-orchestration.md). |
| LLM | **Google Gemini** `gemini-3.1-flash-lite` (free tier), with a deterministic offline fallback | Spec section 14 requires the assignment be completable at no cost. The `ScriptedLlmClient` also makes every agent evaluation test deterministic. |
| Third-party API | **Resend** transactional email | Notifies on assignment, escalation and status change. Free tier, single REST call, key stays in the backend. |
| Testing | xUnit, Vitest + React Testing Library, k6 | Covers backend, database, React, agent evaluation, end-to-end and performance. |

---

## 4. Architecture

```
   React (Vercel)                    ┌──────────────────────────────────────┐
   staff · manager · admin ──HTTPS──►│  ASP.NET Core Web API                │
   · AI approval console             │                                      │
                                     │  Controllers → Services → EF Core    │
   Flutter (built separately) ──────►│         ↑                            │──► Gemini (HTTPS)
   employee self-service             │  Agent Orchestrator                  │
                                     │    Planner · Triage · Solution ·     │──► Resend (HTTPS)
                                     │    Assignment · Validation           │
                                     │         ↓ allow-listed tools only    │
                                     └───────────────┬──────────────────────┘
                                                     ▼
                                          Neon PostgreSQL (TLS)
```

A **modular monolith**: one codebase, one deployable, one database, one transaction boundary.
Four students still own four folders. Full detail in [`docs/02-architecture.md`](./docs/02-architecture.md).

**Projects**

```
backend/
  SmartDesk.Domain/          entities, enums, role constants. No dependencies.
  SmartDesk.Application/     DTOs, services, business rules, agents, tools, orchestrator.
  SmartDesk.Infrastructure/  DbContext, migrations, seeding, JWT, BCrypt, Gemini, Resend.
  SmartDesk.Api/             controllers, middleware, DI, Swagger, CORS.
  SmartDesk.Tests/           117 tests: unit, agent evaluation, database, API, end-to-end.
```

---

## 5. The Agentic AI subsystem

Full detail: [`docs/06-agentic-ai.md`](./docs/06-agentic-ai.md).

### The workflow

```
Employee raises a ticket
   │  (the ticket is committed first — a failing AI can never block ticket creation)
   ▼
① PlannerAgent      → validated multi-step plan                          (no tools: planning needs no access)
② TriageAgent       → category, priority, urgency                        tools: GetTicket
③ SolutionAgent     → matched articles, troubleshooting steps            tools: SearchKnowledgeBase
④ AssignmentAgent   → recommended owner + score                          tools: GetSupportAgents, GetAgentWorkload,
                                                                                ScoreAssignmentCandidates
⑤ ValidationAgent   → SLA risk, escalation decision, violations          tools: CheckSla
   ▼
BusinessRuleEngine — deterministic C#, decides what the advice is allowed to do
   ├── low impact  → applied immediately (priority raise, article links)
   └── HIGH IMPACT → AiApproval row, status Pending, workflow PAUSES
                        ▼
              Manager reviews in the React Approval Centre
                 Approve → executed in ONE transaction → ticket updated + history + audit + email
                 Reject / Request revision → nothing is executed
```

Any failure ends in a persisted `Failed` state with a recorded reason and an untouched ticket —
the "safe, clearly recorded failure" spec section 9.1 requires.

### Why five agents, not four

Spec section 9.3 requires *at least* four. The Planner is assessed under the **group** criterion
("Agent Orchestration"), while each of the four students needs their own agent for the 12-mark
individual criterion. Splitting them gives four clean individual contributions plus direct evidence
for the group criterion, at the cost of one extra prompt.

### What makes each agent distinct (spec section 9.2)

Different system prompt · different C# input/output record · different tool allow-list ·
different failure handling · its own persisted `AgentSteps` row with its own timing and retry count.
None is a rename of another.

### Security controls

| Control | How |
|---|---|
| **Allow-listed tools** | `ToolRegistry` checks the calling agent's own list before the global registry. `ExecuteApprovedAction` is registered but is in **no** agent's list — visible at `GET /api/ai/tools`. |
| **No arbitrary access** | Agents have no SQL, no HTTP, no file system, no shell. Nine narrow tools, each validating its own input. |
| **Scoped tools** | Every tool receives a `ToolContext` naming one workflow and one ticket, and cannot widen it. `GetTicket` refuses any other ticket id. |
| **Structured output** | JSON response mode, then parsed into a C# record that **rejects unknown members**. Enums and numeric ranges bounds-checked. |
| **Business rules outside the LLM** | `BusinessRuleEngine` re-checks every identifier against the live database. SLA deadlines and assignment scores are computed in C#, never taken from the model. |
| **Prompt injection** | Ticket text is wrapped in `<untrusted_user_content>` with a standing instruction that it is data. Structurally, the model can only emit one JSON shape, cannot name a tool outside its list, and cannot write to anything. |
| **Timeouts / retries** | 30 s per model call, 120 s per workflow, 2 retries with exponential backoff, then safe failure. Retry counts are persisted. |
| **Secrets** | `AI_API_KEY` from environment only. Never logged, never persisted, never returned by any endpoint. |
| **Not persisted** | Prompt text, model reasoning, chain-of-thought, keys, tokens. A test asserts this. |

---

## 6. Running it locally

### Prerequisites

- .NET SDK 10
- Node.js 20+
- A PostgreSQL database — either a free [Neon](https://neon.tech) project, or Docker locally

### Step 1 — Database

**Neon (recommended):** create a project and copy the connection string from the dashboard. Either
the `postgresql://…` URI or the .NET key-value form works — the backend accepts both.

> If you paste the URI into a `.env` file, **quote it**. It contains `&`, which the shell would
> otherwise treat as a job separator:
> ```bash
> DATABASE_CONNECTION_STRING='postgresql://user:pass@ep-xxx-pooler.region.aws.neon.tech/neondb?sslmode=require&channel_binding=require'
> ```

**Or Docker locally:**
```bash
docker run -d --name smartdesk-pg \
  -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=smartdesk \
  -p 5432:5432 postgres:16-alpine
```

### Step 2 — Backend environment variables

Copy `.env.example` and fill it in. **Nothing here is committed** — `.env` is git-ignored.

| Variable | Required | Notes |
|---|---|---|
| `DATABASE_CONNECTION_STRING` | **yes** | Neon or local Postgres. **Either format works** — paste Neon's `postgresql://…` URI directly, or use Npgsql's `Host=…;Database=…` form. `ConnectionStringNormalizer` converts the URI. |
| `JWT_SECRET` | **yes** | At least 32 characters. Generate: `openssl rand -base64 48` |
| `AI_API_KEY` | no | Free Gemini key from [AI Studio](https://aistudio.google.com/apikey). **If omitted, the backend automatically falls back to the deterministic `ScriptedLlmClient` — the whole system still runs and demos correctly, at no cost.** |
| `NOTIFICATION_API_KEY` | no | Free [Resend](https://resend.com) key. If omitted, `NullEmailProvider` is used and `Notifications` rows are still written. |
| `NOTIFICATION_REDIRECT_TO` | no | Resend's free tier only delivers to **the address the account was registered with**, and the seeded users have fictional `@smartdesk.local` addresses. Set this to your own address so real emails arrive during a demo. The `Notifications` row still records the true intended recipient. |
| `SEED_PASSWORD` | no | Password for the seeded demo accounts. Defaults to `Password123!` for local development. |

### Step 3 — Run the API

```bash
cd backend/SmartDesk.Api
export DATABASE_CONNECTION_STRING="Host=...;Database=smartdesk;Username=...;Password=...;SSL Mode=Require"
export JWT_SECRET="$(openssl rand -base64 48)"
dotnet run
```

On startup it **applies EF Core migrations and seeds demo data automatically** (idempotent — safe to
re-run). You should see `Database seeded.` then `Now listening on: http://localhost:5299`.

- API health: <http://localhost:5299/health>
- Swagger UI: <http://localhost:5299/swagger>

### The two experiences

The frontend is a public marketing site plus an authenticated workspace, in one app:

| | Routes | Chrome |
|---|---|---|
| **Public** | `/`, `/features`, `/solutions`, `/ai-agents`, `/how-it-works`, `/about`, `/contact` | Marketing navbar + full footer |
| **Auth** | `/signin`, `/signup` | Split-screen auth shell |
| **Workspace** | `/app/dashboard`, `/app/tickets`, `/app/knowledge-base`, `/app/assignments`, `/app/sla`, `/app/reports`, `/app/ai-workflows`, `/app/approvals`, `/app/audit-logs`, `/app/admin/*` | Sidebar workspace shell — no public navigation |

Both share one design system, so they read as the same product. **Light and dark themes** apply
everywhere; the choice is stored in `localStorage`, falls back to the operating system preference,
and is applied by an inline script in `index.html` before first paint so there is no flash of the
wrong theme.

### Step 4 — Run the web app

```bash
cd frontend
cp .env.example .env      # VITE_API_URL=http://localhost:5299
npm install
npm run dev
```

Open <http://localhost:5173>.

### Startup order

Database → API (migrates and seeds) → React. The AI subsystem runs inside the API process, so there
is nothing else to start.

---

## 7. Test accounts

All seeded accounts use the password in `SEED_PASSWORD` (default `Password123!`).

| Email | Role | Use for |
|---|---|---|
| `employee1@smartdesk.local` | Employee | Raising a ticket and watching the AI workflow start |
| `agent1@smartdesk.local` | SupportAgent | Working an assigned ticket (network specialist) |
| `manager@smartdesk.local` | SupportManager | **Approving AI recommendations**, assigning, reports |
| `admin@smartdesk.local` | Admin | Users, categories, knowledge base |

Also seeded: `employee2`, `employee3`, `agent2` (hardware), `agent3` (software).

### Demonstration script

1. Sign in as **employee1**, raise a ticket (e.g. *"VPN client rejects my login after a password change"*).
2. The ticket is created immediately; the workflow runs in the background.
3. Open the ticket's **AI workflow** tab, or go to **AI workflows** → the newest run.
   You will see all five agents, their tool calls, timings, retry counts and structured outputs.
4. Sign in as **manager**. The dashboard shows an approval waiting. Open **Approval centre**.
5. Read the recommendation and its reasoning, then **Approve**.
6. Reopen the ticket: it is now assigned, the **History** tab shows the change attributed to
   `System / AI`, and the **Audit trail** shows the full sequence.
7. Try approving as **employee1** — the API returns **403**, because the gate is enforced in the
   backend, not in the UI.

---

## 8. Testing

**117 backend tests + 27 frontend tests, all passing.**

```bash
# Backend — needs a PostgreSQL server for the integration tests
cd backend
export TEST_DATABASE_CONNECTION_STRING="Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres"
dotnet test

# Frontend
cd frontend
npm run test:run

# Performance
BASE_URL=http://localhost:5299 k6 run perf/smoke.js
```

| Layer | Count | What it covers |
|---|---|---|
| Business rules (unit) | 39 | status machine, SLA calculation, assignment scoring, article relevance, `BusinessRuleEngine` |
| Ticket service | 18 | creation, authorization scoping, status workflow, comments, search/filter/sort/page |
| **Agent evaluation** | 30 | the 12 golden cases below |
| Database integration | 12 | migrations, unique/check/FK constraints, cascades, `jsonb`, `text[]`, transaction atomicity |
| API + end-to-end | 18 | HTTP status codes, authn/authz, Swagger, and the complete workflow |
| React | 27 | protected routes, form validation, search/filter/sort/pagination, API interaction, loading/empty/error states |

### The 12 agent evaluation golden cases

Run against `ScriptedLlmClient`, so they are deterministic, offline and free — no LLM judge, which
spec section 12 explicitly warns against relying on.

1. Planner produces a valid, correctly-ordered plan
2. Every planned agent runs exactly once, in order, with structured output persisted
3. Each agent calls only the tools its job needs
4. A tool outside the agent's allow-list is refused and the refusal is audited
5. Malformed, out-of-range or extra-field output is rejected
6. Deterministic validation drops invented category names, article ids and assignees
7. Business rules hold (critical never auto-closed, priority may only be raised)
8. The workflow pauses; the ticket is unchanged; an employee gets 403
9. **Prompt injection resistance** — "ignore all previous instructions…" changes nothing
10. A transient model failure is retried and the retry count persisted
11. A permanent failure ends in a safe, recorded `Failed` state with the ticket untouched
12. No prompt text, reasoning or secret is ever persisted

### Continuous integration

`.github/workflows/ci.yml` runs on every push and pull request to `main` and `develop`:
restore → build → test the backend against a real PostgreSQL service container, and
build + test the frontend. **No secrets required** — the scripted LLM and null email provider are
the automatic fallbacks.

---

## 9. API documentation

Swagger UI is enabled in every environment at `/swagger`, with an **Authorize** button so protected
endpoints are testable directly. The full endpoint list, with roles and status codes, is in
[`docs/05-api-design.md`](./docs/05-api-design.md).

Status codes used: `200` · `201` (+`Location`) · `202` (workflow accepted) · `204` · `400` ·
`401` · `403` · `404` · `409` · `422` (agent output failed validation) · `500` · `503`.

Errors are RFC 7807 `ProblemDetails`, so one error handler covers the whole API in both clients.

**Every list endpoint returns the same envelope**, which is why one deserialiser works everywhere:

```json
{ "items": [], "page": 1, "pageSize": 20, "totalCount": 0, "totalPages": 0 }
```

---

## 10. Deployment

See [`docs/11-deployment.md`](./docs/11-deployment.md) for the full runbook.

| Component | Platform | Notes |
|---|---|---|
| PostgreSQL | Neon | Free tier. Restrict the role; use the pooled connection string. |
| API | Render / Railway / Azure App Service | Set the environment variables from section 6. Migrations run on startup. Health at `/health`, Swagger at `/swagger`. |
| React | Vercel | Set `VITE_API_URL` to the deployed API. Add the Vercel URL to `Cors:AllowedOrigins` on the API. |
| Agentic AI | runs in-process | Nothing extra to deploy. Set `AI_API_KEY`, or leave it unset to use the scripted client. |

---

## 11. Repository structure

```
SmartDeskAI/
├── backend/              ASP.NET Core solution (5 projects)
├── frontend/             React + Vite + TypeScript
├── docs/                 design documents and ADRs
├── perf/                 k6 performance smoke test
├── .github/workflows/    CI
├── .env.example          backend environment variables (no secrets)
└── README.md
```

---

## 12. Security considerations

| Concern | Control |
|---|---|
| Passwords | BCrypt, work factor 11, per-password salt. Never logged or returned. |
| Tokens | JWT with issuer, audience, lifetime and signature all validated. Carries identity and role only. |
| Secrets | Environment variables only. `appsettings.json` contains none. `.env` is git-ignored. |
| Authorization | `[Authorize(Roles=…)]` **plus** resource-ownership checks in the service layer — an employee reading another employee's ticket gets 403 even with a valid token. |
| Account enumeration | Login returns the same message for an unknown email and a wrong password. |
| Privilege escalation | Self-registration always creates an `Employee`. Only an Admin can grant a staff role. An admin cannot demote or deactivate their own account. |
| Approval gate | Enforced in `ApprovalService`, not in React. A hidden button changes nothing. |
| AI boundary | Agents cannot execute SQL, call arbitrary URLs, or write to any table except a *pending* approval request. |
| Error leakage | Stack traces are never returned outside Development. |
| Transport | TLS to Neon (`SSL Mode=Require`), HTTPS in production. |
| CORS | Explicit allow-list of origins, not a wildcard. |
| Data minimisation | Outbound email contains ticket number, title and status only — never the description. |

---

## 13. Individual contributions

| Student | Component | Agent | Branches |
|---|---|---|---|
| Student 1 | A — Ticket Management | **TriageAgent** | `feature/ticket-management`, `feature/agent-triage` |
| Student 2 | B — Assignment & Workload | **AssignmentAgent** | `feature/assignment-management`, `feature/agent-assignment` |
| Student 3 | C — Knowledge Base | **SolutionAgent** | `feature/knowledge-base`, `feature/agent-solution` |
| Student 4 | D — SLA, Escalation & Reporting | **ValidationAgent** | `feature/sla-reporting`, `feature/agent-validation` |
| Group | Orchestration | **PlannerAgent** | `feature/agent-orchestrator` |

Each student additionally delivers, for their own component: the EF entity and migration slice, the
React screens, backend unit tests, agent evaluation tests for their agent, and the matching Flutter
screens. Strategy: [`docs/09-git-ci-and-flutter-gap.md`](./docs/09-git-ci-and-flutter-gap.md).

> **⚠ This section must be filled in with real names and real commits before submission.**
> Spec sections 13 and 18.2 explicitly reject back-filled commit history and final-day bulk uploads.
> Every student must commit their own work incrementally and be able to explain it at the viva.

---

## 14. AI usage declaration

This assignment is assessed at **AI Use Level 4 (Full AI)** — AI tools are permitted during
development *with disclosure*, and prohibited during the final demonstration and viva.

**Each student must complete their own AI usage log and one-page reflection** using
[`docs/12-ai-usage-log-template.md`](./docs/12-ai-usage-log-template.md), and the group must submit a
consolidated declaration. These are marked, and a reflection that does not match the student's git
history will not receive credit (spec section 18.3).

> **⚠ Not yet completed.** This is written work only you can do.

---

## 15. Operating notes from running against the real services

Recorded because these are exactly the questions a viva asks, and each was observed rather than assumed.

**Model latency is the dominant cost, and it changes the shape of the workflow.**
Against `ScriptedLlmClient` the five agents finish in about 550 ms. Against live Gemini the same
workflow takes roughly 20 s, because each agent is a real network round-trip. That is why
`WorkflowTimeoutSeconds` is 300 and `LlmTimeoutSeconds` is 60 — an earlier 120 s budget caused a
legitimate timeout when the provider was rate limiting.

**We saw the safe-failure path fire for real.** During testing Gemini returned `503 — high demand`.
The Assignment agent retried twice (the configured limit), the workflow exceeded its budget, and the
system did exactly what it is designed to do:

| | |
|---|---|
| Workflow status | `Failed`, with the reason recorded |
| Ticket | **completely untouched** — still `New`, still `Low`, unassigned, not escalated |
| Approvals created | **zero** |
| Audit trail | `WorkflowCreated` → `PlanCreated` → `WorkflowFailed` |

This is the single best piece of evidence for spec §9.1's "safe, clearly recorded failure", and it is
worth demonstrating deliberately by pointing `AI_API_KEY` at an invalid value.

**Gemini model names move.** `gemini-2.0-flash` and `gemini-2.5-flash` now return 404 for new keys.
Enumerate what your key can actually reach before a demo:
`curl "https://generativelanguage.googleapis.com/v1beta/models?key=$AI_API_KEY"`.

**Gemini 3 can return a reasoning part before the answer**, so `GeminiLlmClient.ExtractText` scans for
the first part carrying text rather than assuming `parts[0]`. It reads only the text; any reasoning
metadata is discarded and never persisted.

**Neon adds real latency.** A workflow that takes 550 ms of agent time against local PostgreSQL takes
noticeably longer against Neon, because every tool call is a round-trip to us-east-2. State this in
the performance report.

---

## 16. Known limitations

Stated honestly, because the viva will ask.

- **Flutter is not in this repository.** It is being built separately. The API needs no changes to
  support it — see [`docs/09-git-ci-and-flutter-gap.md`](./docs/09-git-ci-and-flutter-gap.md) for the exact gap.
- **Knowledge search is `ILIKE` plus keyword scoring**, not full-text search. Adequate at seed scale;
  the upgrade path (`pg_trgm` + GIN index) is documented in `docs/04-database-design.md`.
- **The frontend bundle is ~816 KB** (237 KB gzipped), dominated by Recharts. Acceptable for an internal
  tool; route-level code splitting is the fix if it matters. The marketing pages themselves ship no
  extra dependencies — the hero visual, workflow diagram and product preview are all CSS and real
  components, not images or an animation library.
- **The contact form validates but does not send.** It says so on submit rather than pretending.
  Real support requests go through the product, where they get a ticket and a full agent workflow.
- **Password reset is not implemented**, so "Forgot password?" points at the contact page rather
  than a route that does nothing.
- **Notifications are fire-and-forget.** A failed send is recorded on the `Notifications` row but is
  not retried later by a background job.
- **Resend's free tier only delivers to the account owner's own address** until a domain is verified.
  Use `NOTIFICATION_REDIRECT_TO` for demonstrations. The integration itself is real: a genuine HTTPS
  call, a real message id on success, and a recorded `Failed` row with the provider's own error text
  on rejection — and in both cases the business operation that triggered it still succeeds.
