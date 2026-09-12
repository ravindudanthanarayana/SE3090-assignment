<div align="center">

<img src="frontend/public/logo-mark.png" alt="SmartDesk AI" width="88">

# SmartDesk AI

<sub>**An Agentic AI powered IT help desk — with a human always in the loop.**</sub>

<sub>Five specialist AI agents triage every ticket, search the knowledge base, recommend an owner and</sub>
<sub>check SLA risk. Anything high-impact **stops and waits for a manager**. Every step is recorded.</sub>

<br>

[![Frontend](https://img.shields.io/badge/Frontend-se3090.vercel.app-2ea44f?style=for-the-badge&logo=vercel&logoColor=white)](https://se3090.vercel.app/)
[![Backend API](https://img.shields.io/badge/Backend%20API-se3090--assignment.onrender.com-2ea44f?style=for-the-badge&logo=render&logoColor=white)](https://se3090-assignment.onrender.com)

<br>

[![CI](https://img.shields.io/badge/CI-passing-grey?style=flat-square&logo=githubactions&logoColor=lightgrey)](.github/workflows/ci.yml)
[![Tests](https://img.shields.io/badge/tests-255%20passing-grey?style=flat-square)](#8-testing)
[![.NET](https://img.shields.io/badge/.NET-10-grey?style=flat-square&logo=dotnet&logoColor=lightgrey)](backend)
[![React](https://img.shields.io/badge/React-19-grey?style=flat-square&logo=react&logoColor=lightgrey)](frontend)
[![Flutter](https://img.shields.io/badge/Flutter-3.35-grey?style=flat-square&logo=flutter&logoColor=lightgrey)](mobile/smartdesk_mobile)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Neon-grey?style=flat-square&logo=postgresql&logoColor=lightgrey)](https://neon.tech)

<br>

<sub>**Three clients, one API, one database.**</sub>

| | | |
|:--:|:--:|:--:|
| <sub>🖥️&nbsp;&nbsp;**React web console**</sub> | <sub>📱&nbsp;&nbsp;**Flutter mobile app**</sub> | <sub>🤖&nbsp;&nbsp;**Agentic AI subsystem**</sub> |
| <sub>Staff · managers · admins<br>Approval Centre, reporting</sub> | <sub>Employee self-service<br>Raise, track, camera attachments</sub> | <sub>Planner · Triage · Solution<br>Assignment · Validation</sub> |

<sub>Built for **SE3090 — Software Engineering Frameworks, Assignment 1**</sub>

</div>

---

## Seeded accounts

<sub>All 8 seeded accounts share the same password: `Password123!`</sub>

| <sub>#</sub> | <sub>Email</sub> | <sub>Password</sub> | <sub>Name</sub> | <sub>Role</sub> | <sub>Department</sub> |
|---|---|---|---|---|---|
| <sub>1</sub> | <sub>`admin@smartdesk.local`</sub> | <sub>`Password123!`</sub> | <sub>Alex Admin</sub> | <sub>Admin</sub> | <sub>IT</sub> |
| <sub>2</sub> | <sub>`manager@smartdesk.local`</sub> | <sub>`Password123!`</sub> | <sub>Morgan Manager</sub> | <sub>SupportManager</sub> | <sub>IT Service Desk</sub> |
| <sub>3</sub> | <sub>`agent1@smartdesk.local`</sub> | <sub>`Password123!`</sub> | <sub>Priya Network</sub> | <sub>SupportAgent</sub> | <sub>IT Service Desk</sub> |
| <sub>4</sub> | <sub>`agent2@smartdesk.local`</sub> | <sub>`Password123!`</sub> | <sub>Sam Hardware</sub> | <sub>SupportAgent</sub> | <sub>IT Service Desk</sub> |
| <sub>5</sub> | <sub>`agent3@smartdesk.local`</sub> | <sub>`Password123!`</sub> | <sub>Riya Software</sub> | <sub>SupportAgent</sub> | <sub>IT Service Desk</sub> |
| <sub>6</sub> | <sub>`employee1@smartdesk.local`</sub> | <sub>`Password123!`</sub> | <sub>Dev Employee</sub> | <sub>Employee</sub> | <sub>Engineering</sub> |
| <sub>7</sub> | <sub>`employee2@smartdesk.local`</sub> | <sub>`Password123!`</sub> | <sub>Fay Finance</sub> | <sub>Employee</sub> | <sub>Sales/Engineering</sub> |
| <sub>8</sub> | <sub>`employee3@smartdesk.local`</sub> | <sub>`Password123!`</sub> | <sub>Hari Sales</sub> | <sub>Employee</sub> | <sub>Sales</sub> |

---

## 1. Contents

| <sub>Section</sub> | |
|---|---|
| <sub>[Business problem and roles](#2-business-problem-and-roles)</sub> | <sub>what the system does and who uses it</sub> |
| <sub>[Technology and why](#3-technology-choices)</sub> | <sub>stack with justification</sub> |
| <sub>[Architecture](#4-architecture)</sub> | <sub>how the pieces fit together</sub> |
| <sub>[The Agentic AI subsystem](#5-the-agentic-ai-subsystem)</sub> | <sub>the five agents, their tools and the approval gate</sub> |
| <sub>[Running it locally](#6-running-it-locally)</sub> | <sub>prerequisites, environment variables, startup order</sub> |
| <sub>[Test accounts](#7-test-accounts)</sub> | <sub>demo credentials</sub> |
| <sub>[Testing](#8-testing)</sub> | <sub>what is tested and how to run it</sub> |
| <sub>[API documentation](#9-api-documentation)</sub> | <sub>Swagger, endpoint list</sub> |
| <sub>[The Flutter mobile client](#10-the-flutter-mobile-client)</sub> | <sub>the employee self-service app</sub> |
| <sub>[Deployment](#11-deployment)</sub> | <sub>Neon, API host, Vercel</sub> |
| <sub>[Repository structure](#12-repository-structure)</sub> | <sub>where everything lives</sub> |
| <sub>[Security](#13-security-considerations)</sub> | <sub>what is protected and how</sub> |
| <sub>[Individual contributions](#14-individual-contributions)</sub> | <sub>the four components, their owners and what each delivers</sub> |
| <sub>[AI usage declaration](#15-ai-usage-declaration)</sub> | <sub>required by spec section 18</sub> |
| <sub>[Operating notes](#16-operating-notes-from-running-against-the-real-services)</sub> | <sub>what we observed running against live Gemini and Neon</sub> |

<sub>Detailed design documents live in [`docs/`](./docs).</sub>

---

## 2. Business problem and roles

<sub>An internal IT help desk has more requests than it has people. Tickets arrive as unstructured text,</sub>
<sub>get mis-categorised, sit unassigned, and quietly breach their service-level agreement. SmartDesk AI</sub>
<sub>addresses that by doing the triage work automatically — while keeping a human in charge of anything</sub>
<sub>that actually changes ownership or escalates.</sub>

### Roles

| <sub>Role</sub> | <sub>Can do</sub> |
|---|---|
| <sub>**Employee**</sub> | <sub>Raise tickets, view and comment on their own tickets, track status and history, read published knowledge articles</sub> |
| <sub>**SupportAgent**</sub> | <sub>Everything above, plus work the tickets assigned to them: change status, add internal notes, record a resolution, see the SLA queue</sub> |
| <sub>**SupportManager**</sub> | <sub>Everything above, plus assign and reassign tickets, escalate, view all tickets, run reports, and **approve or reject AI recommendations**</sub> |
| <sub>**Admin**</sub> | <sub>Everything above, plus manage users and roles, ticket categories and agent skills</sub> |

### The four business components

<sub>Each is owned by one student, end to end across the whole stack — not split by layer (spec section 3).</sub>

| <sub>Component</sub> | <sub>Owner</sub> | <sub>Owns</sub> | <sub>Business operation beyond CRUD</sub> |
|---|---|---|---|
| <sub>**A. Ticket Management**</sub> | <sub>`IT24100858` Wijesinghe D.T.D</sub> | <sub>tickets, comments, history, categories</sub> | <sub>validated status-transition machine</sub> |
| <sub>**B. Assignment & Workload**</sub> | <sub>`IT24100533` Danthanarayana D.M.R</sub> | <sub>assignments, agent skills, workload</sub> | <sub>deterministic skill-vs-workload assignment scoring</sub> |
| <sub>**C. Knowledge Base**</sub> | <sub>`IT24101090` Jayakody N.D</sub> | <sub>knowledge articles, ticket–article links</sub> | <sub>ticket-aware article relevance ranking</sub> |
| <sub>**D. SLA, Escalation & Reporting**</sub> | <sub>`IT23361690` Gunathilake B.M.P</sub> | <sub>SLA state, escalation, dashboards, approvals</sub> | <sub>SLA-risk-driven escalation, approval-gated</sub> |

<sub>Each component carries at least four meaningful API endpoints plus one business-specific operation</sub>
<sub>beyond CRUD, and each owner contributes an identifiable agent to the AI subsystem. Full breakdown in</sub>
<sub>[section 14](#14-individual-contributions).</sub>

---

## 3. Technology choices

| <sub>Layer</sub> | <sub>Choice</sub> | <sub>Why</sub> |
|---|---|---|
| <sub>Backend</sub> | <sub>ASP.NET Core 10 Web API (C#)</sub> | <sub>Required by the module. One process owns all business rules, so both clients get identical behaviour.</sub> |
| <sub>Data access</sub> | <sub>EF Core 10 + Npgsql</sub> | <sub>Required. Migrations give us a reviewable, replayable schema history.</sub> |
| <sub>Database</sub> | <sub>PostgreSQL on **Neon**</sub> | <sub>Free tier, no card, real Postgres. `jsonb` lets us store varying agent state without inventing six tables.</sub> |
| <sub>Web</sub> | <sub>React 19 + Vite + TypeScript</sub> | <sub>Required. TypeScript because the API surface is large and the compiler catches DTO drift for free.</sub> |
| <sub>Design system</sub> | <sub>Semantic CSS custom properties + Tailwind 4 `@theme inline`</sub> | <sub>One token set (`bg-surface`, `text-fg`, `border-line`, `bg-accent-solid`) shared by the marketing site, auth and the workspace. Dark mode is a token swap, not a sweep of `dark:` overrides.</sub> |
| <sub>Web state</sub> | <sub>**Context API**</sub> | <sub>Identity plus the JWT is the only genuinely global client state. Redux would be ceremony without benefit — see [ADR-001](./docs/ADRs/ADR-001-react-state-management.md).</sub> |
| <sub>Mobile</sub> | <sub>**Flutter 3.35 / Dart 3.9**</sub> | <sub>Required. One codebase for Android and iOS, and the employee half of the workflow is where a phone genuinely beats a browser — you can photograph the error.</sub> |
| <sub>Mobile state</sub> | <sub>**Riverpod**</sub> | <sub>Compile-time-safe injection and a built-in loading/data/error union, so no screen hand-rolls its states — see [ADR-007](./docs/ADRs/ADR-007-flutter-state-management.md).</sub> |
| <sub>Mobile token storage</sub> | <sub>**`flutter_secure_storage`**</sub> | <sub>Keychain / EncryptedSharedPreferences, backed by the platform keystore. `SharedPreferences` is a plain file; a native app has a better option and uses it.</sub> |
| <sub>Styling</sub> | <sub>Tailwind CSS 4</sub> | <sub>No separate stylesheet to keep in sync; responsive breakpoints inline.</sub> |
| <sub>Agentic AI</sub> | <sub>**Custom C# orchestrator, in-process**</sub> | <sub>Spec section 2 permits a custom approach and forbids clients calling the AI directly. In-process makes that structurally impossible — see [ADR-002](./docs/ADRs/ADR-002-agentic-ai-orchestration.md).</sub> |
| <sub>LLM</sub> | <sub>**Google Gemini** `gemini-3.1-flash-lite` (free tier), with a deterministic offline fallback</sub> | <sub>Spec section 14 requires the assignment be completable at no cost. The `ScriptedLlmClient` also makes every agent evaluation test deterministic.</sub> |
| <sub>Third-party API</sub> | <sub>**Resend** transactional email</sub> | <sub>Notifies on assignment, escalation and status change. Free tier, single REST call, key stays in the backend.</sub> |
| <sub>Testing</sub> | <sub>xUnit, Vitest + React Testing Library, k6</sub> | <sub>Covers backend, database, React, agent evaluation, end-to-end and performance.</sub> |

---

## 4. Architecture

<p align="center">
  <img src="docs/images/architecture.png" alt="SmartDesk AI architecture: React and Flutter clients call the same ASP.NET Core Web API, which alone reaches Neon PostgreSQL, Gemini and Resend" width="900">
</p>

<sub>Both clients call the **same** endpoints. Neither can reach the database or the model</sub>
<sub>directly — that boundary is structural, not a convention.</sub>

<sub>A **modular monolith**: one codebase, one deployable, one database, one transaction boundary.</sub>
<sub>Four students still own four folders. Full detail in [`docs/02-architecture.md`](./docs/02-architecture.md).</sub>

<sub>**Projects**</sub>

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

<sub>Full detail: [`docs/06-agentic-ai.md`](./docs/06-agentic-ai.md).</sub>

### The workflow

<p align="center">
  <img src="docs/images/agentic-ai-workflow.png" alt="Agentic AI workflow: ticket committed first, then Planner, Triage, Solution, Assignment and Validation agents, then the BusinessRuleEngine routes low-impact advice straight through and high-impact advice to manager approval" width="900">
</p>

<sub>Any failure ends in a persisted `Failed` state with a recorded reason and an untouched ticket —</sub>
<sub>the "safe, clearly recorded failure" spec section 9.1 requires.</sub>

### Why five agents, not four

<sub>Spec section 9.3 requires *at least* four. The Planner is assessed under the **group** criterion</sub>
<sub>("Agent Orchestration"), while each of the four students needs their own agent for the 12-mark</sub>
<sub>individual criterion. Splitting them gives four clean individual contributions plus direct evidence</sub>
<sub>for the group criterion, at the cost of one extra prompt.</sub>

| <sub>Agent</sub> | <sub>Owner</sub> | <sub>Responsibility</sub> | <sub>In → out</sub> | <sub>Tools</sub> |
|---|---|---|---|---|
| <sub>**PlannerAgent**</sub> | <sub>*Group*</sub> | <sub>Planning and coordination</sub> | <sub>objective + ticket summary → ordered multi-step plan</sub> | <sub>none — planning needs no data access</sub> |
| <sub>**TriageAgent**</sub> | <sub>A · `IT24100858` Wijesinghe</sub> | <sub>Domain analysis</sub> | <sub>ticket text + category list → category, priority, urgency 1–5, keywords</sub> | <sub>`GetTicket`</sub> |
| <sub>**SolutionAgent**</sub> | <sub>C · `IT24101090` Jayakody</sub> | <sub>Knowledge retrieval</sub> | <sub>triage output + ticket → matched article ids, steps, confidence</sub> | <sub>`SearchKnowledgeBase`</sub> |
| <sub>**AssignmentAgent**</sub> | <sub>B · `IT24100533` Danthanarayana</sub> | <sub>Action / tool use</sub> | <sub>ticket, category, agent pool → recommended owner, score, alternatives</sub> | <sub>`GetSupportAgents`, `GetAgentWorkload`, `ScoreAssignmentCandidates`</sub> |
| <sub>**ValidationAgent**</sub> | <sub>D · `IT23361690` Gunathilake</sub> | <sub>Validation and safety</sub> | <sub>all prior agent outputs → `isValid`, violations, SLA risk, escalate?</sub> | <sub>`CheckSla`</sub> |

### What makes each agent distinct (spec section 9.2)

<sub>Different system prompt · different C# input/output record · different tool allow-list ·</sub>
<sub>different failure handling · its own persisted `AgentSteps` row with its own timing and retry count.</sub>
<sub>None is a rename of another.</sub>

### Security controls

| <sub>Control</sub> | <sub>How</sub> |
|---|---|
| <sub>**Allow-listed tools**</sub> | <sub>`ToolRegistry` checks the calling agent's own list before the global registry. `ExecuteApprovedAction` is registered but is in **no** agent's list — visible at `GET /api/ai/tools`.</sub> |
| <sub>**No arbitrary access**</sub> | <sub>Agents have no SQL, no HTTP, no file system, no shell. Nine narrow tools, each validating its own input.</sub> |
| <sub>**Scoped tools**</sub> | <sub>Every tool receives a `ToolContext` naming one workflow and one ticket, and cannot widen it. `GetTicket` refuses any other ticket id.</sub> |
| <sub>**Structured output**</sub> | <sub>JSON response mode, then parsed into a C# record that **rejects unknown members**. Enums and numeric ranges bounds-checked.</sub> |
| <sub>**Business rules outside the LLM**</sub> | <sub>`BusinessRuleEngine` re-checks every identifier against the live database. SLA deadlines and assignment scores are computed in C#, never taken from the model.</sub> |
| <sub>**Prompt injection**</sub> | <sub>Ticket text is wrapped in `<untrusted_user_content>` with a standing instruction that it is data. Structurally, the model can only emit one JSON shape, cannot name a tool outside its list, and cannot write to anything.</sub> |
| <sub>**Timeouts / retries**</sub> | <sub>30 s per model call, 120 s per workflow, 2 retries with exponential backoff, then safe failure. Retry counts are persisted.</sub> |
| <sub>**Secrets**</sub> | <sub>`AI_API_KEY` from environment only. Never logged, never persisted, never returned by any endpoint.</sub> |
| <sub>**Not persisted**</sub> | <sub>Prompt text, model reasoning, chain-of-thought, keys, tokens. A test asserts this.</sub> |

---

## 6. Running it locally

### Prerequisites

| <sub>For</sub> | <sub>Need</sub> |
|---|---|
| <sub>Backend</sub> | <sub>.NET SDK 10</sub> |
| <sub>Web</sub> | <sub>Node.js 20+</sub> |
| <sub>Mobile *(optional)*</sub> | <sub>Flutter 3.35+ / Dart 3.9+, and an Android emulator or iOS simulator</sub> |
| <sub>Database</sub> | <sub>A free [Neon](https://neon.tech) project, or Docker locally</sub> |

### Step 1 — Database

<sub>**Neon (recommended):** create a project and copy the connection string from the dashboard. Either</sub>
<sub>the `postgresql://…` URI or the .NET key-value form works — the backend accepts both.</sub>

> <sub>If you paste the URI into a `.env` file, **quote it**. It contains `&`, which the shell would</sub>
> <sub>otherwise treat as a job separator:</sub>
> ```bash
> DATABASE_CONNECTION_STRING='postgresql://user:pass@ep-xxx-pooler.region.aws.neon.tech/neondb?sslmode=require&channel_binding=require'
> ```

<sub>**Or Docker locally:**</sub>
```bash
docker run -d --name smartdesk-pg \
  -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=smartdesk \
  -p 5432:5432 postgres:16-alpine
```

### Step 2 — Backend environment variables

<sub>Copy `.env.example` and fill it in. **Nothing here is committed** — `.env` is git-ignored.</sub>

| <sub>Variable</sub> | <sub>Required</sub> | <sub>Notes</sub> |
|---|---|---|
| <sub>`DATABASE_CONNECTION_STRING`</sub> | <sub>**yes**</sub> | <sub>Neon or local Postgres. **Either format works** — paste Neon's `postgresql://…` URI directly, or use Npgsql's `Host=…;Database=…` form. `ConnectionStringNormalizer` converts the URI.</sub> |
| <sub>`JWT_SECRET`</sub> | <sub>**yes**</sub> | <sub>At least 32 characters. Generate: `openssl rand -base64 48`</sub> |
| <sub>`AI_API_KEY`</sub> | <sub>no</sub> | <sub>Free Gemini key from [AI Studio](https://aistudio.google.com/apikey). **If omitted, the backend automatically falls back to the deterministic `ScriptedLlmClient` — the whole system still runs and demos correctly, at no cost.**</sub> |
| <sub>`NOTIFICATION_API_KEY`</sub> | <sub>no</sub> | <sub>Free [Resend](https://resend.com) key. If omitted, `NullEmailProvider` is used and `Notifications` rows are still written.</sub> |
| <sub>`NOTIFICATION_REDIRECT_TO`</sub> | <sub>no</sub> | <sub>Resend's free tier only delivers to **the address the account was registered with**, and the seeded users have fictional `@smartdesk.local` addresses. Set this to your own address so real emails arrive during a demo. The `Notifications` row still records the true intended recipient.</sub> |
| <sub>`SEED_PASSWORD`</sub> | <sub>no</sub> | <sub>Password for the seeded demo accounts. Defaults to `Password123!` for local development.</sub> |

### Step 3 — Run the API

```bash
cd backend/SmartDesk.Api
export DATABASE_CONNECTION_STRING="Host=...;Database=smartdesk;Username=...;Password=...;SSL Mode=Require"
export JWT_SECRET="$(openssl rand -base64 48)"
dotnet run
```

<sub>On startup it **applies EF Core migrations and seeds demo data automatically** (idempotent — safe to</sub>
<sub>re-run). You should see `Database seeded.` then `Now listening on: http://localhost:5299`.</sub>

- <sub>API health: <http://localhost:5299/health></sub>
- <sub>Swagger UI: <http://localhost:5299/swagger></sub>

### The two experiences

<sub>The frontend is a public marketing site plus an authenticated workspace, in one app:</sub>

| | <sub>Routes</sub> | <sub>Chrome</sub> |
|---|---|---|
| <sub>**Public**</sub> | <sub>`/`, `/features`, `/solutions`, `/ai-agents`, `/how-it-works`, `/about`, `/contact`</sub> | <sub>Marketing navbar + full footer</sub> |
| <sub>**Auth**</sub> | <sub>`/signin`, `/signup`</sub> | <sub>Split-screen auth shell</sub> |
| <sub>**Workspace**</sub> | <sub>`/app/dashboard`, `/app/tickets`, `/app/knowledge-base`, `/app/assignments`, `/app/sla`, `/app/reports`, `/app/ai-workflows`, `/app/approvals`, `/app/audit-logs`, `/app/admin/*`</sub> | <sub>Sidebar workspace shell — no public navigation</sub> |

<sub>Both share one design system, so they read as the same product. **Light and dark themes** apply</sub>
<sub>everywhere; the choice is stored in `localStorage`, falls back to the operating system preference,</sub>
<sub>and is applied by an inline script in `index.html` before first paint so there is no flash of the</sub>
<sub>wrong theme.</sub>

### Step 4 — Run the web app

```bash
cd frontend
cp .env.example .env      # VITE_API_URL=http://localhost:5299
npm install
npm run dev
```

<sub>Open <http://localhost:5173>.</sub>

### Step 5 — Run the mobile app *(optional)*

```bash
cd mobile/smartdesk_mobile
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5299   # Android emulator
```

<sub>See [section 10](#10-the-flutter-mobile-client) for the right `API_BASE_URL` per target.</sub>

### Startup order

<sub>Database → API (migrates and seeds) → React and/or Flutter. The AI subsystem runs **inside** the API</sub>
<sub>process, so there is nothing else to start. The two clients are independent — run either, or both</sub>
<sub>side by side to demonstrate the cross-platform approval workflow.</sub>

---

## 7. Test accounts

<sub>All seeded accounts use the password in `SEED_PASSWORD` (default `Password123!`).</sub>

| <sub>Email</sub> | <sub>Role</sub> | <sub>Use for</sub> |
|---|---|---|
| <sub>`employee1@smartdesk.local`</sub> | <sub>Employee</sub> | <sub>Raising a ticket and watching the AI workflow start</sub> |
| <sub>`agent1@smartdesk.local`</sub> | <sub>SupportAgent</sub> | <sub>Working an assigned ticket (network specialist)</sub> |
| <sub>`manager@smartdesk.local`</sub> | <sub>SupportManager</sub> | <sub>**Approving AI recommendations**, assigning, reports</sub> |
| <sub>`admin@smartdesk.local`</sub> | <sub>Admin</sub> | <sub>Users, categories, knowledge base</sub> |

<sub>Also seeded: `employee2`, `employee3`, `agent2` (hardware), `agent3` (software).</sub>

### Demonstration script

1. <sub>Sign in as **employee1**, raise a ticket (e.g. *"VPN client rejects my login after a password change"*).</sub>
2. <sub>The ticket is created immediately; the workflow runs in the background.</sub>
3. <sub>Open the ticket's **AI workflow** tab, or go to **AI workflows** → the newest run.</sub>
   <sub>You will see all five agents, their tool calls, timings, retry counts and structured outputs.</sub>
4. <sub>Sign in as **manager**. The dashboard shows an approval waiting. Open **Approval centre**.</sub>
5. <sub>Read the recommendation and its reasoning, then **Approve**.</sub>
6. <sub>Reopen the ticket: it is now assigned, the **History** tab shows the change attributed to</sub>
   <sub>`System / AI`, and the **Audit trail** shows the full sequence.</sub>
7. <sub>Try approving as **employee1** — the API returns **403**, because the gate is enforced in the</sub>
   <sub>backend, not in the UI.</sub>

<sub>**For the cross-platform version of the same story**, raise the ticket from the Flutter app in</sub>
<sub>step 1 instead of the browser, approve it in React at step 5, then reopen it in Flutter: the</sub>
<sub>Overview shows the new assignee, and the History tab shows the change attributed to `System / AI`.</sub>
<sub>That single loop is the evidence for spec §4.7 and §10.2 — see [section 10](#10-the-flutter-mobile-client).</sub>

---

## 8. Testing

<sub>**255 tests, all passing** — 129 backend, 34 web, 92 mobile.</sub>

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

> <sub>The 30 backend integration tests need a reachable PostgreSQL. Without one they fail with an</sub>
> <sub>`NpgsqlException` and the other 99 still pass — CI supplies a `postgres:16` service container.</sub>

| <sub>Layer</sub> | <sub>Count</sub> | <sub>What it covers</sub> |
|---|---|---|
| <sub>Business rules (unit)</sub> | <sub>39</sub> | <sub>status machine, SLA calculation, assignment scoring, article relevance, `BusinessRuleEngine`</sub> |
| <sub>Ticket service</sub> | <sub>18</sub> | <sub>creation, authorization scoping, status workflow, comments, search/filter/sort/page</sub> |
| <sub>**Agent evaluation**</sub> | <sub>30</sub> | <sub>the 12 golden cases below</sub> |
| <sub>Database integration</sub> | <sub>13</sub> | <sub>migrations, unique/check/FK constraints, cascades, `jsonb`, `text[]`, transaction atomicity</sub> |
| <sub>API + end-to-end</sub> | <sub>17</sub> | <sub>HTTP status codes, authn/authz, Swagger, and the complete workflow</sub> |
| <sub>Configuration</sub> | <sub>12</sub> | <sub>`ConnectionStringNormalizer` — Neon URI and Npgsql key-value forms, SSL and pooling options</sub> |
| <sub>React</sub> | <sub>34</sub> | <sub>protected routes, form validation, search/filter/sort/pagination, API interaction, loading/empty/error states</sub> |
| <sub>**Flutter**</sub> | <sub>92</sub> | <sub>form validation, DTO parsing, error mapping (401/403/404/409/5xx/timeout), auth state, the agent checklist, reusable widgets, dark mode, 320dp layout — plus **9 tests against real captured API payloads**</sub> |

### The 12 agent evaluation golden cases

<sub>Run against `ScriptedLlmClient`, so they are deterministic, offline and free — no LLM judge, which</sub>
<sub>spec section 12 explicitly warns against relying on.</sub>

1. <sub>Planner produces a valid, correctly-ordered plan</sub>
2. <sub>Every planned agent runs exactly once, in order, with structured output persisted</sub>
3. <sub>Each agent calls only the tools its job needs</sub>
4. <sub>A tool outside the agent's allow-list is refused and the refusal is audited</sub>
5. <sub>Malformed, out-of-range or extra-field output is rejected</sub>
6. <sub>Deterministic validation drops invented category names, article ids and assignees</sub>
7. <sub>Business rules hold (critical never auto-closed, priority may only be raised)</sub>
8. <sub>The workflow pauses; the ticket is unchanged; an employee gets 403</sub>
9. <sub>**Prompt injection resistance** — "ignore all previous instructions…" changes nothing</sub>
10. <sub>A transient model failure is retried and the retry count persisted</sub>
11. <sub>A permanent failure ends in a safe, recorded `Failed` state with the ticket untouched</sub>
12. <sub>No prompt text, reasoning or secret is ever persisted</sub>

### Continuous integration

<sub>`.github/workflows/ci.yml` runs on every push and pull request to `main` and `develop`, as three</sub>
<sub>parallel jobs:</sub>

| <sub>Job</sub> | <sub>Steps</sub> |
|---|---|
| <sub>`backend`</sub> | <sub>restore → build → test against a real `postgres:16` service container</sub> |
| <sub>`frontend`</sub> | <sub>`npm ci` → type check and build → Vitest</sub> |
| <sub>`mobile`</sub> | <sub>`flutter pub get` → `flutter analyze --fatal-infos` → `flutter test` → **build the APK and upload it as an artifact**</sub> |

<sub>**No secrets required** — the scripted LLM and null email provider are the automatic fallbacks. The</sub>
<sub>APK artifact is what spec §14.4 asks for, produced by CI rather than by hand.</sub>

---

## 9. API documentation

<sub>Swagger UI is enabled in every environment at `/swagger`, with an **Authorize** button so protected</sub>
<sub>endpoints are testable directly. The full endpoint list, with roles and status codes, is in</sub>
<sub>[`docs/05-api-design.md`](./docs/05-api-design.md).</sub>

<sub>Status codes used: `200` · `201` (+`Location`) · `202` (workflow accepted) · `204` · `400` ·</sub>
<sub>`401` · `403` · `404` · `409` · `422` (agent output failed validation) · `500` · `503`.</sub>

<sub>Errors are RFC 7807 `ProblemDetails`, so one error handler covers the whole API in both clients.</sub>

<sub>**Every list endpoint returns the same envelope**, which is why one deserialiser works everywhere:</sub>

```json
{ "items": [], "page": 1, "pageSize": 20, "totalCount": 0, "totalPages": 0 }
```

---

## 10. The Flutter mobile client

<img src="mobile/smartdesk_mobile/assets/brand/logo-mark.png" align="right" width="52" alt="">

<sub>[`mobile/smartdesk_mobile/`](./mobile/smartdesk_mobile) is the **employee self-service** app — a</sub>
<sub>second client for this same API, not a second system. It adds **no** database, no authentication</sub>
<sub>scheme, no AI logic, and it required **no backend changes**: every endpoint it calls already existed.</sub>

### Deliberately a different product from the web console

<sub>Spec §4.5 asks the two clients to serve meaningfully different purposes. They do:</sub>

| | <sub>React web console</sub> | <sub>Flutter mobile app</sub> |
|---|---|---|
| <sub>**Audience**</sub> | <sub>Support agents, managers, admins</sub> | <sub>Employees</sub> |
| <sub>**Core job**</sub> | <sub>Work the queue, assign, escalate, report, **approve AI actions**</sub> | <sub>Raise a ticket and follow what happens to it</sub> |
| <sub>**Surface**</sub> | <sub>18 routes incl. Approval Centre, reporting, admin</sub> | <sub>8 screens, all employee-facing</sub> |
| <sub>**Cannot do**</sub> | <sub>—</sub> | <sub>Approve an AI action — the API returns **403**</sub> |
| <sub>**Can do that the other cannot**</sub> | <sub>—</sub> | <sub>**Photograph the problem with the device camera**</sub> |

### What's inside

| | |
|---|---|
| <sub>**Screens**</sub> | <sub>Splash · Login · Sign up · Home · My Tickets · Create Ticket · Ticket detail *(Overview / AI Support / Comments / History)* · Profile</sub> |
| <sub>**State**</sub> | <sub>Riverpod — `StateNotifierProvider` for the session and filters, `FutureProvider` for server state ([ADR-007](./docs/ADRs/ADR-007-flutter-state-management.md))</sub> |
| <sub>**Routing**</sub> | <sub>`go_router` with a single `redirect` guard for protected routes</sub> |
| <sub>**Security**</sub> | <sub>JWT in the platform keystore via `flutter_secure_storage`. **The password is never stored.** A 401 clears the session and returns to Login, from one interceptor</sub> |
| <sub>**Reusable widgets**</sub> | <sub>17 — buttons, fields, badges, ticket card, loading / empty / error views, AI step tile, recommendation card</sub> |
| <sub>**Search & filters**</sub> | <sub>Debounced search plus status, priority and category — **all server-side** query parameters, never filtered on the phone</sub> |
| <sub>**Device feature**</sub> | <sub>Camera **and** gallery attachments when raising a ticket</sub> |
| <sub>**Theming**</sub> | <sub>Light and dark, from the same tokens as the web app's `index.css`</sub> |
| <sub>**Quality**</sub> | <sub>92 tests · `flutter analyze --fatal-infos` clean · APK builds in CI</sub> |

### The AI, shown honestly

<sub>Creating a ticket makes the **server** start the workflow. The app polls and renders what the</sub>
<sub>orchestrator actually recorded — the five agents under **their real backend names**, taken from</sub>
<sub>`AgentNames` in the C#:</sub>

| <sub>Shown as</sub> | <sub>Backend agent</sub> |
|---|---|
| <sub>Planning</sub> | <sub>`PlannerAgent`</sub> |
| <sub>Ticket Analysis</sub> | <sub>`TriageAgent`</sub> |
| <sub>Knowledge Search</sub> | <sub>`SolutionAgent`</sub> |
| <sub>Assignment Analysis</sub> | <sub>`AssignmentAgent`</sub> |
| <sub>Validation</sub> | <sub>`ValidationAgent`</sub> |

<sub>The employee-facing label sits above the raw agent name, so the checklist traces straight back to</sub>
<sub>the source at a viva. Ticks, spinners and timings are the persisted `AgentSteps` values — nothing is</sub>
<sub>advanced by a client-side animation, and an agent the plan skipped shows as pending rather than</sub>
<sub>being hidden. Every recommendation row renders **only if the backend returned that field**.</sub>

### The cross-client workflow — executed, not theorised

<sub>Run end to end against the live API and the Neon database:</sub>

<p align="center">
  <img src="docs/images/cross-client-workflow.png" alt="From request to resolution: an employee raises TKT-000033 in Flutter, ASP.NET Core commits it and starts the agents, the business rules apply the low-impact changes, the assignment parks at the approval gate, a manager approves in the React Approval Centre, and the employee sees the assigned ticket with full history" width="900">
</p>

<sub>The employee **cannot** skip step 6 — `POST /api/ai/approvals/{id}/decision` returns 403 for an</sub>
<sub>Employee. That is what makes this a real cross-client workflow rather than two views of the same</sub>
<sub>permissions. The payloads from that run are committed in `test/fixtures/` and asserted by</sub>
<sub>`live_payload_test.dart`, so the models are pinned to what the server really sends.</sub>

### Run it

```bash
cd mobile/smartdesk_mobile
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5299   # Android emulator
```

<sub>`10.0.2.2` is the emulator's alias for your machine's `localhost`; `localhost` inside the emulator</sub>
<sub>would mean the emulator itself. Use `http://localhost:5299` on the iOS simulator, or your LAN IP on</sub>
<sub>a physical phone. The URL in use is printed under the Sign in button, so you can confirm it at a</sub>
<sub>glance during a demo.</sub>

<sub>📖 [Full app documentation](./mobile/smartdesk_mobile/README.md) ·</sub>
<sub>📋 [Assignment write-up](./docs/13-flutter-application.md) ·</sub>
<sub>🧭 [ADR-007 — state management](./docs/ADRs/ADR-007-flutter-state-management.md)</sub>

---

## 11. Deployment

<sub>See [`docs/11-deployment.md`](./docs/11-deployment.md) for the full runbook.</sub>

| <sub>Component</sub> | <sub>Platform</sub> | <sub>Notes</sub> |
|---|---|---|
| <sub>PostgreSQL</sub> | <sub>Neon</sub> | <sub>Free tier. Restrict the role; use the pooled connection string.</sub> |
| <sub>API</sub> | <sub>Render / Railway / Azure App Service</sub> | <sub>Set the environment variables from section 6. Migrations run on startup. Health at `/health`, Swagger at `/swagger`.</sub> |
| <sub>React</sub> | <sub>Vercel</sub> | <sub>Set `VITE_API_URL` to the deployed API. Add the Vercel URL to `Cors:AllowedOrigins` on the API.</sub> |
| <sub>Agentic AI</sub> | <sub>runs in-process</sub> | <sub>Nothing extra to deploy. Set `AI_API_KEY`, or leave it unset to use the scripted client.</sub> |

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

<sub>Each Flutter feature is split `domain / data / state / ui`, mirroring how the backend splits into</sub>
<sub>Domain / Application / Infrastructure / Api.</sub>

---

## 13. Security considerations

| <sub>Concern</sub> | <sub>Control</sub> |
|---|---|
| <sub>Passwords</sub> | <sub>BCrypt, work factor 11, per-password salt. Never logged or returned.</sub> |
| <sub>Tokens</sub> | <sub>JWT with issuer, audience, lifetime and signature all validated. Carries identity and role only.</sub> |
| <sub>Secrets</sub> | <sub>Environment variables only. `appsettings.json` contains none. `.env` is git-ignored.</sub> |
| <sub>Authorization</sub> | <sub>`[Authorize(Roles=…)]` **plus** resource-ownership checks in the service layer — an employee reading another employee's ticket gets 403 even with a valid token.</sub> |
| <sub>Account enumeration</sub> | <sub>Login returns the same message for an unknown email and a wrong password.</sub> |
| <sub>Privilege escalation</sub> | <sub>Self-registration always creates an `Employee`. Only an Admin can grant a staff role. An admin cannot demote or deactivate their own account.</sub> |
| <sub>Approval gate</sub> | <sub>Enforced in `ApprovalService`, not in React. A hidden button changes nothing.</sub> |
| <sub>AI boundary</sub> | <sub>Agents cannot execute SQL, call arbitrary URLs, or write to any table except a *pending* approval request.</sub> |
| <sub>Error leakage</sub> | <sub>Stack traces are never returned outside Development.</sub> |
| <sub>Transport</sub> | <sub>TLS to Neon (`SSL Mode=Require`), HTTPS in production.</sub> |
| <sub>CORS</sub> | <sub>Explicit allow-list of origins, not a wildcard. The native mobile client is not subject to CORS at all.</sub> |
| <sub>Mobile token storage</sub> | <sub>iOS Keychain / Android EncryptedSharedPreferences via `flutter_secure_storage`, not `SharedPreferences`. The password is never written to the device.</sub> |
| <sub>Mobile authorization</sub> | <sub>The app hides what an employee cannot do, but it is **not** the boundary. Verified from a real employee token: another user's ticket → 403 · deciding an approval → 403 · the approval queue → 403 · no or forged token → 401 · attachment bytes without a token → 401.</sub> |
| <sub>Client secrets</sub> | <sub>Neither client holds one. The mobile app is compiled with a base URL only; the database password, JWT signing key and AI key never leave the API process.</sub> |
| <sub>Data minimisation</sub> | <sub>Outbound email contains ticket number, title and status only — never the description.</sub> |

---

## 14. Individual contributions

<sub>Four students, four business components, one agent each — plus a group-owned coordinator.</sub>

| <sub>Student</sub> | <sub>ID</sub> | <sub>Component</sub> | <sub>Agent</sub> | <sub>Branches</sub> |
|---|---|---|---|---|
| <sub>Wijesinghe D.T.D</sub> | <sub>`IT24100858`</sub> | <sub>A — Ticket Management</sub> | <sub>**TriageAgent**</sub> | <sub>`feature/ticket-management`, `feature/agent-triage`</sub> |
| <sub>Danthanarayana D.M.R</sub> | <sub>`IT24100533`</sub> | <sub>B — Assignment & Workload</sub> | <sub>**AssignmentAgent**</sub> | <sub>`feature/assignment-management`, `feature/agent-assignment`</sub> |
| <sub>Jayakody N.D</sub> | <sub>`IT24101090`</sub> | <sub>C — Knowledge Base</sub> | <sub>**SolutionAgent**</sub> | <sub>`feature/knowledge-base`, `feature/agent-solution`</sub> |
| <sub>Gunathilake B.M.P</sub> | <sub>`IT23361690`</sub> | <sub>D — SLA, Escalation & Reporting</sub> | <sub>**ValidationAgent**</sub> | <sub>`feature/sla-reporting`, `feature/agent-validation`</sub> |
| <sub>*Group*</sub> | <sub>—</sub> | <sub>Orchestration</sub> | <sub>**PlannerAgent**</sub> | <sub>`feature/agent-orchestrator`</sub> |

### Component A — Ticket Management · `IT24100858` Wijesinghe D.T.D

<sub>The front door: capture a problem as structured, trackable work with a controlled lifecycle and a</sub>
<sub>full audit trail.</sub>

| | |
|---|---|
| <sub>**Entities**</sub> | <sub>`Tickets`, `TicketComments`, `TicketHistory`, `TicketCategories`, `TicketAttachments`</sub> |
| <sub>**Endpoints**</sub> | <sub>`GET /api/tickets` (search · filter · sort · page) · `GET /api/tickets/{id}` · `POST /api/tickets` · `PUT /api/tickets/{id}` · `GET`/`POST /api/tickets/{id}/comments` · `GET /api/tickets/{id}/history`</sub> |
| <sub>**Beyond CRUD**</sub> | <sub>`POST /api/tickets/{id}/status` — a validated status-transition machine. Illegal moves are refused with `409`, not silently accepted.</sub> |
| <sub>**React**</sub> | <sub>Ticket list with server-side search/filter/sort/pagination, ticket detail, comments, history timeline, category admin</sub> |
| <sub>**Flutter**</sub> | <sub>My Tickets, Create Ticket form and validation, ticket detail with Overview / Comments / History tabs</sub> |
| <sub>**Database**</sub> | <sub>Ticket schema, category FK, history table, unique ticket number, indexes on status / priority / created, `CreatedAt` / `UpdatedAt` audit fields</sub> |
| <sub>**Agent**</sub> | <sub>**TriageAgent** — classifies category, priority and urgency from unstructured text. Tool: `GetTicket`</sub> |
| <sub>**Third party**</sub> | <sub>Status-change notification email through the shared Resend integration</sub> |
| <sub>**Tests**</sub> | <sub>Status-machine unit tests · ticket service authorization scoping · list query tests · React ticket screens · Flutter form validation</sub> |
| <sub>**Security**</sub> | <sub>An Employee reads only their own tickets — enforced inside the SQL query, not by the client. Only the requester may edit, and only while `New`</sub> |

<sub>Creates the record every other component acts on, and starts the agent workflow.</sub>

### Component B — Assignment & Workload · `IT24100533` Danthanarayana D.M.R

<sub>Get each ticket to the right person: balance skill against current load instead of assigning by hand.</sub>

| | |
|---|---|
| <sub>**Entities**</sub> | <sub>`TicketAssignments`, `AgentSkills`, `Users` (SupportAgent)</sub> |
| <sub>**Endpoints**</sub> | <sub>`GET /api/support-agents` · `GET /api/assignments/workload` · `GET /api/tickets/{id}/assignments` · `PUT /api/support-agents/{id}/skills` · `DELETE /api/support-agents/{id}/skills/{skillId}`</sub> |
| <sub>**Beyond CRUD**</sub> | <sub>`POST /api/tickets/{id}/assign` — deterministic skill-vs-workload scoring, then a transactional (re)assignment that writes history and notifies</sub> |
| <sub>**React**</sub> | <sub>Assignment console, agent workload dashboard, skill matrix editor, recommendation panel on a ticket</sub> |
| <sub>**Flutter**</sub> | <sub>Assigned-agent display on ticket detail; the assignment result surfaced in the AI Support tab and history</sub> |
| <sub>**Database**</sub> | <sub>Assignment history table, agent–skill join with proficiency, unique constraint per agent + category, FK delete behaviour, workload aggregation query</sub> |
| <sub>**Agent**</sub> | <sub>**AssignmentAgent** — recommends an owner with a score and alternatives. Tools: `GetSupportAgents`, `GetAgentWorkload`, `ScoreAssignmentCandidates`</sub> |
| <sub>**Third party**</sub> | <sub>Assignment notification email to the newly assigned agent</sub> |
| <sub>**Tests**</sub> | <sub>Scoring algorithm unit tests · assignment authorization (an agent cannot self-assign) · workload query · React console · Flutter display</sub> |
| <sub>**Security**</sub> | <sub>Assignment is Manager/Admin only; a support agent calling it gets `403`. The AI may only recommend — applying it requires approval</sub> |

<sub>Owns the high-impact action that triggers the human approval gate in the cross-platform workflow.</sub>

### Component C — Knowledge Base · `IT24101090` Jayakody N.D

<sub>Reuse what has already been solved: surface the right article for a specific ticket so problems are</sub>
<sub>not re-diagnosed.</sub>

| | |
|---|---|
| <sub>**Entities**</sub> | <sub>`KnowledgeArticles`, `TicketArticleLinks`</sub> |
| <sub>**Endpoints**</sub> | <sub>`GET /api/knowledge-articles` (search · filter · sort · page) · `GET /api/knowledge-articles/{id}` · `POST /api/knowledge-articles` · `PUT /api/knowledge-articles/{id}` · `DELETE /api/knowledge-articles/{id}`</sub> |
| <sub>**Beyond CRUD**</sub> | <sub>`GET /api/tickets/{id}/relevant-articles` — ticket-aware relevance ranking, plus `POST .../link-article` to attach one as a suggested solution</sub> |
| <sub>**React**</sub> | <sub>Article browser with search and filters, editor with publish/unpublish, relevance panel on a ticket, link-article action</sub> |
| <sub>**Flutter**</sub> | <sub>Suggested-solution list on ticket detail; the AI's recommended steps rendered in the AI Support tab</sub> |
| <sub>**Database**</sub> | <sub>Article schema with `text[]` tags, published flag, ticket–article join carrying relevance score and source, indexes supporting the search</sub> |
| <sub>**Agent**</sub> | <sub>**SolutionAgent** — matches articles and drafts troubleshooting steps. Tool: `SearchKnowledgeBase`. Ids it did not receive from the tool are discarded</sub> |
| <sub>**Third party**</sub> | <sub>Not primary — suggested articles are included in the notification body where relevant</sub> |
| <sub>**Tests**</sub> | <sub>Relevance-ranking unit tests · publish/unpublish authorization · search query · React browser and editor · Flutter suggested-solution rendering</sub> |
| <sub>**Security**</sub> | <sub>Employees see published articles only; authoring is staff-only. Article ids returned by the model are validated against the database before use</sub> |

<sub>Supplies the low-impact AI action — article links are applied automatically, no approval needed.</sub>

### Component D — SLA, Escalation & Reporting · `IT23361690` Gunathilake B.M.P

<sub>Make the promise measurable: track SLA risk, escalate before a breach, report on it — and own the</sub>
<sub>human approval gate.</sub>

| | |
|---|---|
| <sub>**Entities**</sub> | <sub>`AiApprovals`, `AuditLogs`, `Notifications`, SLA fields on `Ticket`</sub> |
| <sub>**Endpoints**</sub> | <sub>`GET /api/tickets/sla-at-risk` · `GET /api/reports/dashboard` · `GET /api/reports/sla` · `GET /api/ai/approvals` (the review queue) · `GET /api/audit-logs`</sub> |
| <sub>**Beyond CRUD**</sub> | <sub>`POST /api/ai/approvals/{id}/decision` — the human-in-the-loop gate; approval executes the action in one transaction. Plus `POST /api/tickets/{id}/escalate`</sub> |
| <sub>**React**</sub> | <sub>Approval Centre, SLA at-risk queue, analytics dashboard with charts, agent-performance report, audit-log viewer</sub> |
| <sub>**Flutter**</sub> | <sub>SLA badge on tickets, escalation reason, and the approval's status and outcome shown read-only in the AI Support tab</sub> |
| <sub>**Database**</sub> | <sub>Approval table with status and decision audit, audit-log table, notification rows, SLA deadline column, and the transaction that makes approval atomic</sub> |
| <sub>**Agent**</sub> | <sub>**ValidationAgent** — checks SLA risk, decides whether escalation is warranted, reports rule violations. Tool: `CheckSla`</sub> |
| <sub>**Third party**</sub> | <sub>Primary owner of the Resend integration — timeouts, retries, failure rows, and keeping the key server-side</sub> |
| <sub>**Tests**</sub> | <sub>SLA calculation unit tests · approval authorization (`403` for an employee) · transaction atomicity · React Approval Centre · Flutter approval-status display</sub> |
| <sub>**Security**</sub> | <sub>The approval gate is enforced in the service layer, not the UI — a hidden button changes nothing. Only `Approved` executes anything</sub> |

<sub>Closes the cross-platform loop: the manager's decision here is what updates the employee's ticket in Flutter.</sub>

### Every member covers every layer

<sub>Owning a component is not enough — spec section 3 requires each student to contribute across the</sub>
<sub>whole required stack and to have an identifiable Agentic AI contribution.</sub>

| <sub>Required of every student</sub> | <sub>A · Wijesinghe</sub> | <sub>B · Danthanarayana</sub> | <sub>C · Jayakody</sub> | <sub>D · Gunathilake</sub> |
|---|:---:|:---:|:---:|:---:|
| <sub>ASP.NET Core endpoints</sub> | <sub>✓</sub> | <sub>✓</sub> | <sub>✓</sub> | <sub>✓</sub> |
| <sub>PostgreSQL and data modelling</sub> | <sub>✓</sub> | <sub>✓</sub> | <sub>✓</sub> | <sub>✓</sub> |
| <sub>React screens</sub> | <sub>✓</sub> | <sub>✓</sub> | <sub>✓</sub> | <sub>✓</sub> |
| <sub>Flutter screens</sub> | <sub>✓</sub> | <sub>✓</sub> | <sub>✓</sub> | <sub>✓</sub> |
| <sub>Distinct Agentic AI agent</sub> | <sub>✓</sub> | <sub>✓</sub> | <sub>✓</sub> | <sub>✓</sub> |
| <sub>API integration and security</sub> | <sub>✓</sub> | <sub>✓</sub> | <sub>✓</sub> | <sub>✓</sub> |
| <sub>Tests</sub> | <sub>✓</sub> | <sub>✓</sub> | <sub>✓</sub> | <sub>✓</sub> |
| <sub>Git commits, PRs, reviews</sub> | <sub>✓</sub> | <sub>✓</sub> | <sub>✓</sub> | <sub>✓</sub> |
| <sub>Documentation</sub> | <sub>✓</sub> | <sub>✓</sub> | <sub>✓</sub> | <sub>✓</sub> |

<sub>Strategy: [`docs/09-git-ci-and-flutter-gap.md`](./docs/09-git-ci-and-flutter-gap.md).</sub>

> <sub>**⚠ Git history is the evidence for this section.** Spec sections 13 and 18.2 explicitly reject</sub>
> <sub>back-filled commit history and final-day bulk uploads. Every student must commit their own work</sub>
> <sub>incrementally and be able to explain, modify, test and debug it at the viva.</sub>

---

## 15. AI usage declaration

<sub>This assignment is assessed at **AI Use Level 4 (Full AI)** — AI tools are permitted during</sub>
<sub>development *with disclosure*, and prohibited during the final demonstration and viva.</sub>

<sub>**Each student must complete their own AI usage log and one-page reflection** using</sub>
<sub>[`docs/12-ai-usage-log-template.md`](./docs/12-ai-usage-log-template.md), and the group must submit a</sub>
<sub>consolidated declaration. These are marked, and a reflection that does not match the student's git</sub>
<sub>history will not receive credit (spec section 18.3).</sub>

> <sub>**⚠ Not yet completed.** This is written work only you can do.</sub>

---

## 16. Operating notes from running against the real services

<sub>Recorded because these are exactly the questions a viva asks, and each was observed rather than assumed.</sub>

<sub>**Model latency is the dominant cost, and it changes the shape of the workflow.**</sub>
<sub>Against `ScriptedLlmClient` the five agents finish in about 550 ms. Against live Gemini the same</sub>
<sub>workflow takes roughly 20 s, because each agent is a real network round-trip. That is why</sub>
<sub>`WorkflowTimeoutSeconds` is 300 and `LlmTimeoutSeconds` is 60 — an earlier 120 s budget caused a</sub>
<sub>legitimate timeout when the provider was rate limiting.</sub>

<sub>**We saw the safe-failure path fire for real.** During testing Gemini returned `503 — high demand`.</sub>
<sub>The Assignment agent retried twice (the configured limit), the workflow exceeded its budget, and the</sub>
<sub>system did exactly what it is designed to do:</sub>

| | |
|---|---|
| <sub>Workflow status</sub> | <sub>`Failed`, with the reason recorded</sub> |
| <sub>Ticket</sub> | <sub>**completely untouched** — still `New`, still `Low`, unassigned, not escalated</sub> |
| <sub>Approvals created</sub> | <sub>**zero**</sub> |
| <sub>Audit trail</sub> | <sub>`WorkflowCreated` → `PlanCreated` → `WorkflowFailed`</sub> |

<sub>This is the single best piece of evidence for spec §9.1's "safe, clearly recorded failure", and it is</sub>
<sub>worth demonstrating deliberately by pointing `AI_API_KEY` at an invalid value.</sub>

<sub>**Gemini model names move.** `gemini-2.0-flash` and `gemini-2.5-flash` now return 404 for new keys.</sub>
<sub>Enumerate what your key can actually reach before a demo:</sub>
<sub>`curl "https://generativelanguage.googleapis.com/v1beta/models?key=$AI_API_KEY"`.</sub>

<sub>**Gemini 3 can return a reasoning part before the answer**, so `GeminiLlmClient.ExtractText` scans for</sub>
<sub>the first part carrying text rather than assuming `parts[0]`. It reads only the text; any reasoning</sub>
<sub>metadata is discarded and never persisted.</sub>

<sub>**Neon adds real latency.** A workflow that takes 550 ms of agent time against local PostgreSQL takes</sub>
<sub>noticeably longer against Neon, because every tool call is a round-trip to us-east-2. State this in</sub>
<sub>the performance report.</sub>

---
