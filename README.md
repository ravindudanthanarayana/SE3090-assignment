<div align="center">

<img src="frontend/public/logo-mark.png" alt="SmartDesk AI" width="88">

# SmartDesk AI

**An Agentic AI powered IT help desk — with a human always in the loop.**

Five specialist AI agents triage every ticket, search the knowledge base, recommend an owner and
check SLA risk. Anything high-impact **stops and waits for a manager**. Every step is recorded.

<br>

[![CI](https://img.shields.io/badge/CI-passing-2ea44f?style=flat-square&logo=githubactions&logoColor=white)](.github/workflows/ci.yml)
[![Tests](https://img.shields.io/badge/tests-255%20passing-2ea44f?style=flat-square)](#8-testing)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet&logoColor=white)](backend)
[![React](https://img.shields.io/badge/React-19-61DAFB?style=flat-square&logo=react&logoColor=black)](frontend)
[![Flutter](https://img.shields.io/badge/Flutter-3.35-02569B?style=flat-square&logo=flutter&logoColor=white)](mobile/smartdesk_mobile)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Neon-4169E1?style=flat-square&logo=postgresql&logoColor=white)](https://neon.tech)

<br>

**Three clients, one API, one database.**

| | | |
|:--:|:--:|:--:|
| 🖥️&nbsp;&nbsp;**React web console** | 📱&nbsp;&nbsp;**Flutter mobile app** | 🤖&nbsp;&nbsp;**Agentic AI subsystem** |
| Staff · managers · admins<br>Approval Centre, reporting | Employee self-service<br>Raise, track, camera attachments | Planner · Triage · Solution<br>Assignment · Validation |

<sub>Built for **SE3090 — Software Engineering Frameworks, Assignment 1**</sub>

</div>

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
| [The Flutter mobile client](#10-the-flutter-mobile-client) | the employee self-service app |
| [Deployment](#11-deployment) | Neon, API host, Vercel |
| [Repository structure](#12-repository-structure) | where everything lives |
| [Security](#13-security-considerations) | what is protected and how |
| [Individual contributions](#14-individual-contributions) | the four components, their owners and what each delivers |
| [AI usage declaration](#15-ai-usage-declaration) | required by spec section 18 |
| [Operating notes](#16-operating-notes-from-running-against-the-real-services) | what we observed running against live Gemini and Neon |
| [Known limitations](#17-known-limitations) | stated honestly, for the viva |

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

Each is owned by one student, end to end across the whole stack — not split by layer (spec section 3).

| Component | Owner | Owns | Business operation beyond CRUD |
|---|---|---|---|
| **A. Ticket Management** | `IT24100858` Wijesinghe D.T.D | tickets, comments, history, categories | validated status-transition machine |
| **B. Assignment & Workload** | `IT24100533` Danthanarayana D.M.R | assignments, agent skills, workload | deterministic skill-vs-workload assignment scoring |
| **C. Knowledge Base** | `IT24101090` Jayakody N.D | knowledge articles, ticket–article links | ticket-aware article relevance ranking |
| **D. SLA, Escalation & Reporting** | `IT23361690` Gunathilake B.M.P | SLA state, escalation, dashboards, approvals | SLA-risk-driven escalation, approval-gated |

Each component carries at least four meaningful API endpoints plus one business-specific operation
beyond CRUD, and each owner contributes an identifiable agent to the AI subsystem. Full breakdown in
[section 14](#14-individual-contributions).

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
| Mobile | **Flutter 3.35 / Dart 3.9** | Required. One codebase for Android and iOS, and the employee half of the workflow is where a phone genuinely beats a browser — you can photograph the error. |
| Mobile state | **Riverpod** | Compile-time-safe injection and a built-in loading/data/error union, so no screen hand-rolls its states — see [ADR-007](./docs/ADRs/ADR-007-flutter-state-management.md). |
| Mobile token storage | **`flutter_secure_storage`** | Keychain / EncryptedSharedPreferences, backed by the platform keystore. `SharedPreferences` is a plain file; a native app has a better option and uses it. |
| Styling | Tailwind CSS 4 | No separate stylesheet to keep in sync; responsive breakpoints inline. |
| Agentic AI | **Custom C# orchestrator, in-process** | Spec section 2 permits a custom approach and forbids clients calling the AI directly. In-process makes that structurally impossible — see [ADR-002](./docs/ADRs/ADR-002-agentic-ai-orchestration.md). |
| LLM | **Google Gemini** `gemini-3.1-flash-lite` (free tier), with a deterministic offline fallback | Spec section 14 requires the assignment be completable at no cost. The `ScriptedLlmClient` also makes every agent evaluation test deterministic. |
| Third-party API | **Resend** transactional email | Notifies on assignment, escalation and status change. Free tier, single REST call, key stays in the backend. |
| Testing | xUnit, Vitest + React Testing Library, k6 | Covers backend, database, React, agent evaluation, end-to-end and performance. |

---

## 4. Architecture

<p align="center">
  <img src="docs/images/architecture.png" alt="SmartDesk AI architecture: React and Flutter clients call the same ASP.NET Core Web API, which alone reaches Neon PostgreSQL, Gemini and Resend" width="900">
</p>

Both clients call the **same** endpoints. Neither can reach the database or the model
directly — that boundary is structural, not a convention.

A **modular monolith**: one codebase, one deployable, one database, one transaction boundary.
Four students still own four folders. Full detail in [`docs/02-architecture.md`](./docs/02-architecture.md).

**Projects**

```
backend/
  SmartDesk.Domain/          entities, enums, role constants. No dependencies.
  SmartDesk.Application/     DTOs, services, business rules, agents, tools, orchestrator.
  SmartDesk.Infrastructure/  DbContext, migrations, seeding, JWT, BCrypt, Gemini, Resend.
  SmartDesk.Api/             controllers, middleware, DI, Swagger, CORS.
  SmartDesk.Tests/           129 tests: unit, agent evaluation, database, API, end-to-end.
```

---

## 5. The Agentic AI subsystem

Full detail: [`docs/06-agentic-ai.md`](./docs/06-agentic-ai.md).

### The workflow

<p align="center">
  <img src="docs/images/agentic-ai-workflow.png" alt="Agentic AI workflow: ticket committed first, then Planner, Triage, Solution, Assignment and Validation agents, then the BusinessRuleEngine routes low-impact advice straight through and high-impact advice to manager approval" width="900">
</p>

Any failure ends in a persisted `Failed` state with a recorded reason and an untouched ticket —
the "safe, clearly recorded failure" spec section 9.1 requires.

### Why five agents, not four

Spec section 9.3 requires *at least* four. The Planner is assessed under the **group** criterion
("Agent Orchestration"), while each of the four students needs their own agent for the 12-mark
individual criterion. Splitting them gives four clean individual contributions plus direct evidence
for the group criterion, at the cost of one extra prompt.

| Agent | Owner | Responsibility | In → out | Tools |
|---|---|---|---|---|
| **PlannerAgent** | *Group* | Planning and coordination | objective + ticket summary → ordered multi-step plan | none — planning needs no data access |
| **TriageAgent** | A · `IT24100858` Wijesinghe | Domain analysis | ticket text + category list → category, priority, urgency 1–5, keywords | `GetTicket` |
| **SolutionAgent** | C · `IT24101090` Jayakody | Knowledge retrieval | triage output + ticket → matched article ids, steps, confidence | `SearchKnowledgeBase` |
| **AssignmentAgent** | B · `IT24100533` Danthanarayana | Action / tool use | ticket, category, agent pool → recommended owner, score, alternatives | `GetSupportAgents`, `GetAgentWorkload`, `ScoreAssignmentCandidates` |
| **ValidationAgent** | D · `IT23361690` Gunathilake | Validation and safety | all prior agent outputs → `isValid`, violations, SLA risk, escalate? | `CheckSla` |

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

| For | Need |
|---|---|
| Backend | .NET SDK 10 |
| Web | Node.js 20+ |
| Mobile *(optional)* | Flutter 3.35+ / Dart 3.9+, and an Android emulator or iOS simulator |
| Database | A free [Neon](https://neon.tech) project, or Docker locally |

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

### Step 5 — Run the mobile app *(optional)*

```bash
cd mobile/smartdesk_mobile
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5299   # Android emulator
```

See [section 10](#10-the-flutter-mobile-client) for the right `API_BASE_URL` per target.

### Startup order

Database → API (migrates and seeds) → React and/or Flutter. The AI subsystem runs **inside** the API
process, so there is nothing else to start. The two clients are independent — run either, or both
side by side to demonstrate the cross-platform approval workflow.

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

**For the cross-platform version of the same story**, raise the ticket from the Flutter app in
step 1 instead of the browser, approve it in React at step 5, then reopen it in Flutter: the
Overview shows the new assignee, and the History tab shows the change attributed to `System / AI`.
That single loop is the evidence for spec §4.7 and §10.2 — see [section 10](#10-the-flutter-mobile-client).

---

## 8. Testing

**255 tests, all passing** — 129 backend, 34 web, 92 mobile.

```bash
# Backend — needs a PostgreSQL server for the integration tests
cd backend
export TEST_DATABASE_CONNECTION_STRING="Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres"
dotnet test

# Web
cd frontend && npm run test:run

# Mobile
cd mobile/smartdesk_mobile && flutter analyze --fatal-infos && flutter test

# Performance
BASE_URL=http://localhost:5299 k6 run perf/smoke.js
```

> The 30 backend integration tests need a reachable PostgreSQL. Without one they fail with an
> `NpgsqlException` and the other 99 still pass — CI supplies a `postgres:16` service container.

| Layer | Count | What it covers |
|---|---|---|
| Business rules (unit) | 39 | status machine, SLA calculation, assignment scoring, article relevance, `BusinessRuleEngine` |
| Ticket service | 18 | creation, authorization scoping, status workflow, comments, search/filter/sort/page |
| **Agent evaluation** | 30 | the 12 golden cases below |
| Database integration | 13 | migrations, unique/check/FK constraints, cascades, `jsonb`, `text[]`, transaction atomicity |
| API + end-to-end | 17 | HTTP status codes, authn/authz, Swagger, and the complete workflow |
| Configuration | 12 | `ConnectionStringNormalizer` — Neon URI and Npgsql key-value forms, SSL and pooling options |
| React | 34 | protected routes, form validation, search/filter/sort/pagination, API interaction, loading/empty/error states |
| **Flutter** | 92 | form validation, DTO parsing, error mapping (401/403/404/409/5xx/timeout), auth state, the agent checklist, reusable widgets, dark mode, 320dp layout — plus **9 tests against real captured API payloads** |

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

`.github/workflows/ci.yml` runs on every push and pull request to `main` and `develop`, as three
parallel jobs:

| Job | Steps |
|---|---|
| `backend` | restore → build → test against a real `postgres:16` service container |
| `frontend` | `npm ci` → type check and build → Vitest |
| `mobile` | `flutter pub get` → `flutter analyze --fatal-infos` → `flutter test` → **build the APK and upload it as an artifact** |

**No secrets required** — the scripted LLM and null email provider are the automatic fallbacks. The
APK artifact is what spec §14.4 asks for, produced by CI rather than by hand.

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

## 10. The Flutter mobile client

<img src="mobile/smartdesk_mobile/assets/brand/logo-mark.png" align="right" width="52" alt="">

[`mobile/smartdesk_mobile/`](./mobile/smartdesk_mobile) is the **employee self-service** app — a
second client for this same API, not a second system. It adds **no** database, no authentication
scheme, no AI logic, and it required **no backend changes**: every endpoint it calls already existed.

### Deliberately a different product from the web console

Spec §4.5 asks the two clients to serve meaningfully different purposes. They do:

| | React web console | Flutter mobile app |
|---|---|---|
| **Audience** | Support agents, managers, admins | Employees |
| **Core job** | Work the queue, assign, escalate, report, **approve AI actions** | Raise a ticket and follow what happens to it |
| **Surface** | 18 routes incl. Approval Centre, reporting, admin | 8 screens, all employee-facing |
| **Cannot do** | — | Approve an AI action — the API returns **403** |
| **Can do that the other cannot** | — | **Photograph the problem with the device camera** |

### What's inside

| | |
|---|---|
| **Screens** | Splash · Login · Sign up · Home · My Tickets · Create Ticket · Ticket detail *(Overview / AI Support / Comments / History)* · Profile |
| **State** | Riverpod — `StateNotifierProvider` for the session and filters, `FutureProvider` for server state ([ADR-007](./docs/ADRs/ADR-007-flutter-state-management.md)) |
| **Routing** | `go_router` with a single `redirect` guard for protected routes |
| **Security** | JWT in the platform keystore via `flutter_secure_storage`. **The password is never stored.** A 401 clears the session and returns to Login, from one interceptor |
| **Reusable widgets** | 17 — buttons, fields, badges, ticket card, loading / empty / error views, AI step tile, recommendation card |
| **Search & filters** | Debounced search plus status, priority and category — **all server-side** query parameters, never filtered on the phone |
| **Device feature** | Camera **and** gallery attachments when raising a ticket |
| **Theming** | Light and dark, from the same tokens as the web app's `index.css` |
| **Quality** | 92 tests · `flutter analyze --fatal-infos` clean · APK builds in CI |

### The AI, shown honestly

Creating a ticket makes the **server** start the workflow. The app polls and renders what the
orchestrator actually recorded — the five agents under **their real backend names**, taken from
`AgentNames` in the C#:

| Shown as | Backend agent |
|---|---|
| Planning | `PlannerAgent` |
| Ticket Analysis | `TriageAgent` |
| Knowledge Search | `SolutionAgent` |
| Assignment Analysis | `AssignmentAgent` |
| Validation | `ValidationAgent` |

The employee-facing label sits above the raw agent name, so the checklist traces straight back to
the source at a viva. Ticks, spinners and timings are the persisted `AgentSteps` values — nothing is
advanced by a client-side animation, and an agent the plan skipped shows as pending rather than
being hidden. Every recommendation row renders **only if the backend returned that field**.

### The cross-client workflow — executed, not theorised

Run end to end against the live API and the Neon database:

```
①  Flutter        employee raises "VPN is not connecting from home"      → TKT-000033
②  ASP.NET Core   ticket committed, agent workflow starts in background
③  Agents         Planner 3.9s → Triage 3.7s → Solution 4.1s → Assignment 5.4s → Validation 2.5s
④  Rules          Triage: Network/High · 1 article linked automatically
⑤  Approval gate  assignment is HIGH IMPACT → not applied → workflow parks
    Flutter shows "Waiting for manager approval"
⑥  React          manager opens the Approval Centre and approves
⑦  ASP.NET Core   validates the decision, executes it in one transaction
⑧  Flutter        Overview → "Assigned · Priya Network"
                  History  → "Assigned to a support agent", actor System / AI,
                             note "Assigned via approved AI recommendation (approval #17)"
                  AI Support → Completed · Approved by Morgan Manager
```

The employee **cannot** skip step ⑥ — `POST /api/ai/approvals/{id}/decision` returns 403 for an
Employee. That is what makes this a real cross-client workflow rather than two views of the same
permissions. The payloads from that run are committed in `test/fixtures/` and asserted by
`live_payload_test.dart`, so the models are pinned to what the server really sends.

### Run it

```bash
cd mobile/smartdesk_mobile
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5299   # Android emulator
```

`10.0.2.2` is the emulator's alias for your machine's `localhost`; `localhost` inside the emulator
would mean the emulator itself. Use `http://localhost:5299` on the iOS simulator, or your LAN IP on
a physical phone. The URL in use is printed under the Sign in button, so you can confirm it at a
glance during a demo.

📖 [Full app documentation](./mobile/smartdesk_mobile/README.md) ·
📋 [Assignment write-up](./docs/13-flutter-application.md) ·
🧭 [ADR-007 — state management](./docs/ADRs/ADR-007-flutter-state-management.md)

---

## 11. Deployment

See [`docs/11-deployment.md`](./docs/11-deployment.md) for the full runbook.

| Component | Platform | Notes |
|---|---|---|
| PostgreSQL | Neon | Free tier. Restrict the role; use the pooled connection string. |
| API | Render / Railway / Azure App Service | Set the environment variables from section 6. Migrations run on startup. Health at `/health`, Swagger at `/swagger`. |
| React | Vercel | Set `VITE_API_URL` to the deployed API. Add the Vercel URL to `Cors:AllowedOrigins` on the API. |
| Agentic AI | runs in-process | Nothing extra to deploy. Set `AI_API_KEY`, or leave it unset to use the scripted client. |

---

## 12. Repository structure

```
SmartDeskAI/
├── backend/                     ASP.NET Core solution — the only thing that touches the database
│   ├── SmartDesk.Domain/          entities, enums, role constants
│   ├── SmartDesk.Application/     DTOs, services, business rules, the 5 agents, tools, orchestrator
│   ├── SmartDesk.Infrastructure/  DbContext, migrations, seeding, JWT, BCrypt, Gemini, Resend
│   ├── SmartDesk.Api/             controllers, middleware, DI, Swagger, CORS
│   └── SmartDesk.Tests/           129 tests
│
├── frontend/                    React 19 + Vite + TypeScript
│   └── src/                       staff · manager · admin · Approval Centre · reporting
│
├── mobile/
│   └── smartdesk_mobile/        Flutter 3.35 — employee self-service
│       ├── lib/core/              config, theme, API client, secure storage, providers
│       ├── lib/features/          auth · tickets · ai   (domain / data / state / ui each)
│       ├── lib/shared/widgets/    17 reusable widgets
│       ├── lib/routing/           go_router + protected-route guard
│       ├── assets/brand/          the shared SmartDesk logo mark
│       └── test/                  92 tests, incl. fixtures captured from the live API
│
├── docs/                        13 design documents + 7 ADRs
├── perf/                        k6 performance smoke test
├── .github/workflows/ci.yml     CI — backend · frontend · mobile
├── .env.example                 backend environment variables (no secrets)
└── README.md
```

Each Flutter feature is split `domain / data / state / ui`, mirroring how the backend splits into
Domain / Application / Infrastructure / Api.

---

## 13. Security considerations

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
| CORS | Explicit allow-list of origins, not a wildcard. The native mobile client is not subject to CORS at all. |
| Mobile token storage | iOS Keychain / Android EncryptedSharedPreferences via `flutter_secure_storage`, not `SharedPreferences`. The password is never written to the device. |
| Mobile authorization | The app hides what an employee cannot do, but it is **not** the boundary. Verified from a real employee token: another user's ticket → 403 · deciding an approval → 403 · the approval queue → 403 · no or forged token → 401 · attachment bytes without a token → 401. |
| Client secrets | Neither client holds one. The mobile app is compiled with a base URL only; the database password, JWT signing key and AI key never leave the API process. |
| Data minimisation | Outbound email contains ticket number, title and status only — never the description. |

---

## 14. Individual contributions

Four students, four business components, one agent each — plus a group-owned coordinator.

| Student | ID | Component | Agent | Branches |
|---|---|---|---|---|
| Wijesinghe D.T.D | `IT24100858` | A — Ticket Management | **TriageAgent** | `feature/ticket-management`, `feature/agent-triage` |
| Danthanarayana D.M.R | `IT24100533` | B — Assignment & Workload | **AssignmentAgent** | `feature/assignment-management`, `feature/agent-assignment` |
| Jayakody N.D | `IT24101090` | C — Knowledge Base | **SolutionAgent** | `feature/knowledge-base`, `feature/agent-solution` |
| Gunathilake B.M.P | `IT23361690` | D — SLA, Escalation & Reporting | **ValidationAgent** | `feature/sla-reporting`, `feature/agent-validation` |
| *Group* | — | Orchestration | **PlannerAgent** | `feature/agent-orchestrator` |

### Component A — Ticket Management · `IT24100858` Wijesinghe D.T.D

The front door: capture a problem as structured, trackable work with a controlled lifecycle and a
full audit trail.

| | |
|---|---|
| **Entities** | `Tickets`, `TicketComments`, `TicketHistory`, `TicketCategories`, `TicketAttachments` |
| **Endpoints** | `GET /api/tickets` (search · filter · sort · page) · `GET /api/tickets/{id}` · `POST /api/tickets` · `PUT /api/tickets/{id}` · `GET`/`POST /api/tickets/{id}/comments` · `GET /api/tickets/{id}/history` |
| **Beyond CRUD** | `POST /api/tickets/{id}/status` — a validated status-transition machine. Illegal moves are refused with `409`, not silently accepted. |
| **React** | Ticket list with server-side search/filter/sort/pagination, ticket detail, comments, history timeline, category admin |
| **Flutter** | My Tickets, Create Ticket form and validation, ticket detail with Overview / Comments / History tabs |
| **Database** | Ticket schema, category FK, history table, unique ticket number, indexes on status / priority / created, `CreatedAt` / `UpdatedAt` audit fields |
| **Agent** | **TriageAgent** — classifies category, priority and urgency from unstructured text. Tool: `GetTicket` |
| **Third party** | Status-change notification email through the shared Resend integration |
| **Tests** | Status-machine unit tests · ticket service authorization scoping · list query tests · React ticket screens · Flutter form validation |
| **Security** | An Employee reads only their own tickets — enforced inside the SQL query, not by the client. Only the requester may edit, and only while `New` |

Creates the record every other component acts on, and starts the agent workflow.

### Component B — Assignment & Workload · `IT24100533` Danthanarayana D.M.R

Get each ticket to the right person: balance skill against current load instead of assigning by hand.

| | |
|---|---|
| **Entities** | `TicketAssignments`, `AgentSkills`, `Users` (SupportAgent) |
| **Endpoints** | `GET /api/support-agents` · `GET /api/assignments/workload` · `GET /api/tickets/{id}/assignments` · `PUT /api/support-agents/{id}/skills` · `DELETE /api/support-agents/{id}/skills/{skillId}` |
| **Beyond CRUD** | `POST /api/tickets/{id}/assign` — deterministic skill-vs-workload scoring, then a transactional (re)assignment that writes history and notifies |
| **React** | Assignment console, agent workload dashboard, skill matrix editor, recommendation panel on a ticket |
| **Flutter** | Assigned-agent display on ticket detail; the assignment result surfaced in the AI Support tab and history |
| **Database** | Assignment history table, agent–skill join with proficiency, unique constraint per agent + category, FK delete behaviour, workload aggregation query |
| **Agent** | **AssignmentAgent** — recommends an owner with a score and alternatives. Tools: `GetSupportAgents`, `GetAgentWorkload`, `ScoreAssignmentCandidates` |
| **Third party** | Assignment notification email to the newly assigned agent |
| **Tests** | Scoring algorithm unit tests · assignment authorization (an agent cannot self-assign) · workload query · React console · Flutter display |
| **Security** | Assignment is Manager/Admin only; a support agent calling it gets `403`. The AI may only recommend — applying it requires approval |

Owns the high-impact action that triggers the human approval gate in the cross-platform workflow.

### Component C — Knowledge Base · `IT24101090` Jayakody N.D

Reuse what has already been solved: surface the right article for a specific ticket so problems are
not re-diagnosed.

| | |
|---|---|
| **Entities** | `KnowledgeArticles`, `TicketArticleLinks` |
| **Endpoints** | `GET /api/knowledge-articles` (search · filter · sort · page) · `GET /api/knowledge-articles/{id}` · `POST /api/knowledge-articles` · `PUT /api/knowledge-articles/{id}` · `DELETE /api/knowledge-articles/{id}` |
| **Beyond CRUD** | `GET /api/tickets/{id}/relevant-articles` — ticket-aware relevance ranking, plus `POST .../link-article` to attach one as a suggested solution |
| **React** | Article browser with search and filters, editor with publish/unpublish, relevance panel on a ticket, link-article action |
| **Flutter** | Suggested-solution list on ticket detail; the AI's recommended steps rendered in the AI Support tab |
| **Database** | Article schema with `text[]` tags, published flag, ticket–article join carrying relevance score and source, indexes supporting the search |
| **Agent** | **SolutionAgent** — matches articles and drafts troubleshooting steps. Tool: `SearchKnowledgeBase`. Ids it did not receive from the tool are discarded |
| **Third party** | Not primary — suggested articles are included in the notification body where relevant |
| **Tests** | Relevance-ranking unit tests · publish/unpublish authorization · search query · React browser and editor · Flutter suggested-solution rendering |
| **Security** | Employees see published articles only; authoring is staff-only. Article ids returned by the model are validated against the database before use |

Supplies the low-impact AI action — article links are applied automatically, no approval needed.

### Component D — SLA, Escalation & Reporting · `IT23361690` Gunathilake B.M.P

Make the promise measurable: track SLA risk, escalate before a breach, report on it — and own the
human approval gate.

| | |
|---|---|
| **Entities** | `AiApprovals`, `AuditLogs`, `Notifications`, SLA fields on `Ticket` |
| **Endpoints** | `GET /api/tickets/sla-at-risk` · `GET /api/reports/dashboard` · `GET /api/reports/sla` · `GET /api/ai/approvals` (the review queue) · `GET /api/audit-logs` |
| **Beyond CRUD** | `POST /api/ai/approvals/{id}/decision` — the human-in-the-loop gate; approval executes the action in one transaction. Plus `POST /api/tickets/{id}/escalate` |
| **React** | Approval Centre, SLA at-risk queue, analytics dashboard with charts, agent-performance report, audit-log viewer |
| **Flutter** | SLA badge on tickets, escalation reason, and the approval's status and outcome shown read-only in the AI Support tab |
| **Database** | Approval table with status and decision audit, audit-log table, notification rows, SLA deadline column, and the transaction that makes approval atomic |
| **Agent** | **ValidationAgent** — checks SLA risk, decides whether escalation is warranted, reports rule violations. Tool: `CheckSla` |
| **Third party** | Primary owner of the Resend integration — timeouts, retries, failure rows, and keeping the key server-side |
| **Tests** | SLA calculation unit tests · approval authorization (`403` for an employee) · transaction atomicity · React Approval Centre · Flutter approval-status display |
| **Security** | The approval gate is enforced in the service layer, not the UI — a hidden button changes nothing. Only `Approved` executes anything |

Closes the cross-platform loop: the manager's decision here is what updates the employee's ticket in Flutter.

### Every member covers every layer

Owning a component is not enough — spec section 3 requires each student to contribute across the
whole required stack and to have an identifiable Agentic AI contribution.

| Required of every student | A · Wijesinghe | B · Danthanarayana | C · Jayakody | D · Gunathilake |
|---|:---:|:---:|:---:|:---:|
| ASP.NET Core endpoints | ✓ | ✓ | ✓ | ✓ |
| PostgreSQL and data modelling | ✓ | ✓ | ✓ | ✓ |
| React screens | ✓ | ✓ | ✓ | ✓ |
| Flutter screens | ✓ | ✓ | ✓ | ✓ |
| Distinct Agentic AI agent | ✓ | ✓ | ✓ | ✓ |
| API integration and security | ✓ | ✓ | ✓ | ✓ |
| Tests | ✓ | ✓ | ✓ | ✓ |
| Git commits, PRs, reviews | ✓ | ✓ | ✓ | ✓ |
| Documentation | ✓ | ✓ | ✓ | ✓ |

Strategy: [`docs/09-git-ci-and-flutter-gap.md`](./docs/09-git-ci-and-flutter-gap.md).

> **⚠ Git history is the evidence for this section.** Spec sections 13 and 18.2 explicitly reject
> back-filled commit history and final-day bulk uploads. Every student must commit their own work
> incrementally and be able to explain, modify, test and debug it at the viva.

---

## 15. AI usage declaration

This assignment is assessed at **AI Use Level 4 (Full AI)** — AI tools are permitted during
development *with disclosure*, and prohibited during the final demonstration and viva.

**Each student must complete their own AI usage log and one-page reflection** using
[`docs/12-ai-usage-log-template.md`](./docs/12-ai-usage-log-template.md), and the group must submit a
consolidated declaration. These are marked, and a reflection that does not match the student's git
history will not receive credit (spec section 18.3).

> **⚠ Not yet completed.** This is written work only you can do.

---

## 16. Operating notes from running against the real services

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

## 17. Known limitations

Stated honestly, because the viva will ask.

- **The Flutter app's light mode has not been eyeballed on a device.** It is implemented from the
  same tokens as dark mode and covered by widget tests, but the live walkthrough was done on a
  dark-mode machine.
- **The Flutter tests are unit and widget tests**, plus assertions against real captured API
  payloads. There is no automated integration test driving an emulator; the end-to-end
  cross-client run was performed and observed manually
  (see [`docs/13-flutter-application.md`](./docs/13-flutter-application.md) §13.4).
- **`mobile/smartdesk_mobile/android/` deviates from the Flutter template** — Gradle 9.1 + AGP 8.13
  so the build runs on the Java 25 that current Android Studio bundles, and the unused `ndkVersion`
  pin removed.
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
