# SE3090 Assignment 1 — Requirements Checklist (extracted from the specification PDF)

Source: `2026-S1-SE3090-Assignment 1 - ...-SpecificationWithMarkingScheme.pdf`
Every row below is traced to a section of that PDF. Nothing here is invented.

Legend for **Phase**: `WEB` = built now · `FLUTTER` = out of scope now (built separately) · `PROC` = process/report work the group must do (not code).

---

## §2 Mandatory technology stack

| # | Requirement (PDF §2) | Our implementation | Phase |
|---|---|---|---|
| 2.1 | Backend: C# + ASP.NET Core Web API, the mandatory public backend | `backend/SmartDesk.Api` (.NET 10) | WEB |
| 2.2 | Data access: EF Core with PostgreSQL provider | EF Core 10 + `Npgsql.EntityFrameworkCore.PostgreSQL` in `SmartDesk.Infrastructure` | WEB |
| 2.3 | Database: PostgreSQL | Neon serverless PostgreSQL | WEB |
| 2.4 | React: functional components, hooks, routing, **justified** state management | Vite + React 19 + TypeScript, React Router 7, Context API (ADR-001) | WEB |
| 2.5 | Flutter + Dart with justified state management | — | FLUTTER |
| 2.6 | Agentic AI: any suitable **and justified** framework (LangGraph, MS Agent Framework, LlamaIndex, ADK, **or a custom orchestration approach**) | Custom in-process C# orchestrator (ADR-002) | WEB |
| 2.7 | Git + GitHub from project start, incl. GitHub Actions CI | `.github/workflows/ci.yml` | WEB |
| 2.8 | Testing tools for backend, React, Flutter, integration, performance, agent evaluation | xUnit, Vitest+RTL, k6, golden-case agent evals | WEB (Flutter tests later) |
| 2.9 | **Mandatory backend rule**: React and Flutter talk *only* to the ASP.NET Core API; any Python AI service must be internal-only | Satisfied trivially — AI orchestrator runs **inside** the ASP.NET Core process; no separate AI endpoint is publicly reachable | WEB |

## §3 Group structure and individual contribution

| # | Requirement | Our implementation | Phase |
|---|---|---|---|
| 3.1 | 4 students → **4 primary business components**, one owner each | Components A–D (see `03-components-and-ownership.md`) | WEB |
| 3.2 | No PM-only / testing-only / docs-only roles | Every student owns backend + DB + React + Flutter + tests + docs + an agent | PROC |
| 3.3 | Every student needs an **identifiable, distinct Agentic AI contribution** | 1 specialist agent per student (Triage / Assignment / Solution / Validation) | WEB |
| 3.4 | Individual marks adjusted via git history, PRs, issue ownership, test evidence | Branch + issue + PR strategy in `08-git-ci-strategy.md` | PROC |
| 3.5 | Students must be able to explain/modify/debug their code | Design deliberately kept minimal and viva-explainable | PROC |

## §4 Domain and functional scope

| # | Requirement | Our implementation | Phase |
|---|---|---|---|
| 4.1 | Unique real-world domain | IT Help Desk & Support Management ("help-desk systems" is a listed suggestion) | WEB |
| 4.2 | **≥3 user roles** with different responsibilities/permissions | 4 roles: Employee, SupportAgent, SupportManager, Admin | WEB |
| 4.3 | **≥4 major business components** with relational data and business-specific operations | A: Tickets · B: Assignment · C: Knowledge Base · D: SLA/Escalation/Reporting | WEB |
| 4.4 | CRUD **plus** status workflows, search, filtering, sorting, pagination, reporting/analytics | All implemented server-side on `/api/tickets` and `/api/knowledge-articles`; `/api/reports/*` | WEB |
| 4.5 | Meaningful and **different** purposes for React vs Flutter | React = staff/admin/manager/AI-approval console. Flutter = employee self-service (raise ticket, track status). Backend designed for both. | WEB (design) / FLUTTER |
| 4.6 | **≥1 third-party service integration** | Email notification provider through `INotificationService` (see `07-third-party.md`) | WEB |
| 4.7 | **≥1 complete cross-platform workflow** React + Flutter + API + PostgreSQL + Agentic AI | Designed: Employee raises ticket → AI workflow → **Manager approves in React** → status returns to initiator. React implements *both* ends now so it is demonstrable standalone. | WEB (both ends) / FLUTTER (initiator) |

## §5 Part 1 — Secure ASP.NET Core RESTful API

| # | Requirement | Our implementation | Phase |
|---|---|---|---|
| 5.1 | Controllers, DTOs, service/application layer, data-access abstraction, DI | `Api/Controllers` → `Application/Services` → `Infrastructure/AppDbContext` (`IAppDbContext` is the data-access abstraction — justified in ADR-006) | WEB |
| 5.2 | Correct routes, HTTP methods, status codes, request/response models, **async** | REST conventions, `ProducesResponseType`, all handlers `async Task<>` | WEB |
| 5.3 | JWT auth, role-based authorization, protected endpoints, password hashing, secure config | JWT bearer, `[Authorize(Roles=...)]` + resource-level ownership checks, BCrypt, env-var config | WEB |
| 5.4 | CRUD, search, filtering, sorting, pagination, **history**, business-specific operations | `TicketHistory` table + `GET /api/tickets/{id}/history`; ≥1 non-CRUD op per component | WEB |
| 5.5 | Server-side validation, global error handling, structured logging, CORS, Swagger/OpenAPI | FluentValidation-style DataAnnotations + explicit guards, exception middleware → RFC7807 `ProblemDetails`, `ILogger` scopes, CORS policy, Swashbuckle w/ JWT auth button | WEB |
| 5.6 | Agent integration endpoints: start workflow, review status, human approval, execution summaries | `POST /api/ai/workflows`, `GET /api/ai/workflows/{id}`, `GET/POST /api/ai/approvals` | WEB |
| 5.7 | **Each owned component: ≥4 meaningful endpoints + ≥1 business operation beyond CRUD** | Verified per component in `05-api-design.md` | WEB |

## §6 Part 2 — PostgreSQL

| # | Requirement | Our implementation | Phase |
|---|---|---|---|
| 6.1 | Normalized schema + ER diagram + relational schema doc | `docs/04-database-design.md` (Mermaid ER + narrative) | WEB |
| 6.2 | PKs, FKs, relationships, constraints, indexes, suitable PostgreSQL types | See DDL notes; `jsonb`, `text[]`, `timestamptz`, unique + check constraints, 12 targeted indexes | WEB |
| 6.3 | EF Core migrations + seed data | `dotnet ef migrations`, `DbSeeder` with demo users/tickets/articles/skills | WEB |
| 6.4 | Transactions where required; `CreatedAt`/`UpdatedAt` audit fields | Approval execution wrapped in an explicit transaction; audit fields on all mutable entities | WEB |
| 6.5 | Persist only required workflow state/summaries — **no hidden reasoning, passwords, tokens, unnecessary sensitive data** | Only structured JSON outputs persisted; explicit rule + test asserting no raw prompt/CoT storage | WEB |

## §7 Part 3 — React

| # | Requirement | Our implementation | Phase |
|---|---|---|---|
| 7.1 | React primarily for admin/staff/dashboard/reporting/business-data/AI monitoring + approval | Exactly our React scope | WEB |
| 7.2 | Functional components, hooks, React Router, reusable components | `components/` (DataTable, StatusBadge, Pagination, Modal, …) | WEB |
| 7.3 | Justified state management (Context/Redux/Zustand/other) | Context API + custom hooks — ADR-001 | WEB |
| 7.4 | Complete API integration, protected routes, role-based navigation | `ProtectedRoute`, role-filtered nav | WEB |
| 7.5 | CRUD UIs, validation, search, filters, sorting, pagination, dashboards | Tickets + KB + Users + Categories screens; dashboard w/ charts | WEB |
| 7.6 | Responsive + accessible UI with loading / empty / success / error states | Tailwind, shared `<AsyncState>` wrapper, toasts, focus/aria attributes | WEB |
| 7.7 | Agent workflow monitoring, execution summaries, approve/reject/**revise** controls | AI Workflows, Workflow Detail (timeline), Approval Center with 3 actions | WEB |

## §8 Part 4 — Flutter — **DEFERRED**
All of §8 (widgets, routing, state mgmt, secure token storage, forms, device feature, APK) is out of scope for this phase. Impact is analysed in `09-flutter-gap.md`. The backend is designed so Flutter needs **zero** new endpoints.

## §9 Part 5 — Agentic AI subsystem

| # | Requirement | Our implementation | Phase |
|---|---|---|---|
| 9.0 | Not a chatbot / FAQ / single-prompt / text generator | Multi-agent, tool-using, stateful, human-gated ticket triage-to-resolution workflow | WEB |
| 9.1 | **Minimum assessed workflow**: receive domain objective → structured multi-step plan → delegate to distinct agent roles → allow-listed tools with validated inputs + structured outputs → persist state → deterministic checks → pause a high-impact action for authorized approval → auditable result **or** safe recorded failure | The whole `06-agentic-ai.md` design | WEB |
| 9.2 | Distinct agent = identifiable responsibility + defined I/O contract + controlled tool permissions + visible participation | Each agent has its own prompt, its own C# request/response record, its own tool allow-list, its own persisted `AgentStep` row | WEB |
| 9.3 | **≥4 distinct agents** across planning/coordination, domain analysis, action/tool use, validation/safety | 5 roles: Planner (coordination) · Triage (domain analysis) · Solution (retrieval) · Assignment (action) · Validation & Escalation (safety) | WEB |
| 9.4 | Planning and delegation from a user objective | Planner Agent emits a validated `Plan` the orchestrator executes | WEB |
| 9.5 | Allow-listed tools, validated inputs, structured outputs, error handling, least privilege | `IAgentTool` registry; per-agent allow-list; input DTO validation; no arbitrary SQL/HTTP | WEB |
| 9.6 | Shared state: workflow ID, objective, plan, completed steps, tool results, validation results, errors, approval status, final outcome | `AgentWorkflows` + `AgentSteps` + `AgentToolCalls` + `AiApprovals` | WEB |
| 9.7 | Deterministic validation (schema + business rules) before accepting outputs or high-impact actions; reject or return for revision | `AgentOutputValidator` (JSON schema/shape) + `BusinessRuleEngine` (pure C#) | WEB |
| 9.8 | **≥1 high-impact action pauses** for approve / reject / **request revision** | Escalation (and AI-proposed assignment) → `AiApprovals` Pending; enforced in the service layer | WEB |
| 9.9 | Observability: execution summaries, tool calls, **timings**, validation results, errors, **retries**, approval decisions, final result | Every step and tool call stores `DurationMs`, `RetryCount`, status; rendered as a React timeline | WEB |
| 9.10 | Security: RBAC, prompt + tool-input validation, output validation, secret protection, timeouts, retry limits, safe failure | `09` security controls + prompt-injection tests | WEB |

## §10 Required integrated architecture
| # | Requirement | Status |
|---|---|---|
| 10.1 | React + Flutter → same ASP.NET Core API → same PostgreSQL → Agentic AI | Backend + React done now; Flutter slots in unchanged |
| 10.2 | **End-to-end evidence**: workflow begins in one client, passes through API + PostgreSQL + AI, requires review/approval **in the other client**, returns updated status to the initiator | Partially satisfiable now (React initiates *and* approves as different roles). Fully satisfied once Flutter is added — see `09-flutter-gap.md` |

## §11 Third-party integration
Business purpose, backend-routed access, protected credentials, timeout/invalid-response/failure/rate-limit handling, data minimisation → `docs/07-third-party.md`. **WEB.**

## §12 Testing
| Area | Required evidence | Plan | Phase |
|---|---|---|---|
| Backend | unit, service-layer, validation, authn/authz, controller, API integration | xUnit + `WebApplicationFactory` | WEB |
| Database | PostgreSQL integration, constraints, migrations, transactions | xUnit against a real Postgres service container in CI | WEB |
| React | component, form validation, protected route, API integration, error state | Vitest + RTL | WEB |
| Flutter | unit, widget, form, navigation, API | — | FLUTTER |
| End to end | ≥1 complete client → API → PostgreSQL → Agentic AI workflow | E2E xUnit test + scripted demo | WEB |
| Performance | concurrency, response time, success/failure rate, DB response, AI latency | k6 script + report template | WEB |
| Agent evaluation | golden case covering planning/delegation, agent+tool selection, structured output, deterministic validation, business-rule compliance, approval enforcement, **prompt-injection resistance**, failure recovery, safe failure | Deterministic scripted-LLM golden cases | WEB |
| — | **LLM-as-judge must not be the only method** | We use rule-based assertions + schema validation + golden cases; no LLM judge required | WEB |

## §13 Git, CI/CD
Repo from day one · meaningful commits, feature branches, issues, PRs, reviews, project board · **≥1 GitHub Actions workflow that restores, builds and runs backend tests on every push and PR to main** · task allocation + merge/conflict evidence · regular per-student contribution · no artificial/bulk commits. → `docs/08-git-ci-strategy.md`. **WEB + PROC.**

## §14 Deployment and documentation
| # | Requirement | Plan | Phase |
|---|---|---|---|
| 14.1 | API deployed with working **health URL** and **Swagger URL** | `/health` + `/swagger`; platform choice in ADR-005 | WEB |
| 14.2 | PostgreSQL deployed securely, migrations, restricted credentials, init instructions | Neon + `docs/10-deployment.md` | WEB |
| 14.3 | React deployed, live URL, pointing at deployed API | Vercel | WEB |
| 14.4 | Flutter source + runnable APK | — | FLUTTER |
| 14.5 | Agentic AI deployed/runnable with setup, model requirements and startup order | Runs in-process; documented | WEB |
| 14.6 | README + technical documentation (overview, roles, architecture, DB, install, env vars, API docs, test instructions, deployment, live URLs, test accounts, contributions, security, AI declaration) | `README.md` + `docs/` | WEB + PROC |
| 14.7 | **ADR** — min: React state mgmt, Flutter state mgmt, Agentic AI framework/orchestration, DB schema strategy for agent workflow state, cloud deployment platform (3–6 decisions typical) | 6 ADRs in `docs/ADRs/` (Flutter one written when Flutter is built) | WEB + PROC |

## §15 Submission — **PROC**
Group-leader single submission · one consolidated PDF (group report + per-student individual sections) · repo/React/API/Swagger/Neon/AI links + env var names + startup instructions · Flutter APK · 10-min demo video, link-accessible · access kept until 21 Oct 2026 · naming `SE3090_GroupNumber`.

## §17 Demonstration checklist — **WEB (all except the Flutter line)**
Role logins + protected ops · CRUD + business workflow + visible PostgreSQL change + Swagger · React **and Flutter** on the same API · full minimum acceptance AI workflow · human approval + execution history · error handling, tests, passing CI, deployed apps, GitHub history.

## §18 AI usage — **PROC**
Level 4 (Full AI) during development **with disclosure**; Level 1 (No AI) at demo/viva. Per-student AI usage log (date, tool+model, task, what it produced, what was changed/rejected, how verified) + group declaration + ~1-page individual reflection (marked). Template provided in `docs/11-ai-usage-log-template.md`.

---

## Requirements that CANNOT be satisfied in this web-only phase
1. **§8 Part 4 — Flutter application** (entire section) — and the 10 individual marks for it.
2. **§14.4** Flutter APK deliverable.
3. **§12** Flutter test evidence.
4. **§10.2 / §4.7** — the *cross-platform* half of the end-to-end workflow. We build both ends in React so the workflow is demonstrable today; the "other client" requirement needs Flutter.
5. **§17.1** demo line "React and Flutter using the same API".
6. **ADR** for Flutter state management (§14.2) — placeholder left.

Nothing else in the PDF is blocked by deferring Flutter.
