# Marking-Scheme Coverage Matrix (spec §16.1)

Status after the web phase. **Verified** means it was executed and observed, not just written.

## Group contribution — 30 marks

| Criterion | Marks | Evidence | Verified? | Cap now | Blocked by |
|---|---|---|---|---|---|
| Component Design and Business Logic | 10 | 4 components, each ≥4 endpoints + a real non-CRUD operation. Status machine, SLA engine, assignment scorer, article relevance, approval executor — all deterministic C# with unit tests. Complete end-to-end workflow. | ✅ 117 backend tests; E2E test drives the whole flow | **10** | — |
| Integrated Architecture, Agent Orchestration and State Management | 10 | 5 distinct agents, `AgentWorkflows`/`AgentSteps`/`AgentToolCalls`/`AiApprovals`, allow-listed tools, two-layer deterministic validation, auditable logs with timings and retries, safe failure, authorized human approval | ✅ observed live: 5 steps, 6 tool calls, pause, approve, DB update | **8** | The top band expects *complete full-stack* integration; the cross-platform half of §10.2 needs Flutter |
| Documentation and Deployment | 10 | README, 12 design docs, 7 ADRs, Swagger, `/health`, deployment runbook, AI-usage template | ⚠ docs done; deployment not yet performed; AI logs are yours to write | **7** | APK missing (§14.4); deployment + AI logs outstanding |

## Individual contribution — 70 marks (per student)

| Criterion | Marks | Evidence | Verified? | Cap now | Blocked by |
|---|---|---|---|---|---|
| ASP.NET Core RESTful API | 10 | Controllers → services → `IAppDbContext`, DTOs with validation, async throughout, RFC 7807 errors, correct status codes incl. 409/422/503, Swagger with JWT | ✅ 18 API tests over real HTTP | **10** | — |
| PostgreSQL Integration and Data Modelling | 10 | 16 tables, FKs with deliberate delete behaviour, unique + check constraints, 42 indexes (incl. FK indexes), `jsonb`, `text[]`, migrations, seed data, transactional approval | ✅ 12 DB integration tests against real PostgreSQL | **10** | — |
| React Web Application | 10 | 18 routes, 15 reusable components, Context API, protected routes, role-aware nav, validation, loading/empty/error/success states, responsive, charts | ✅ builds; 27 Vitest tests; CORS verified against the live API | **10** | — |
| Flutter Mobile Application | 10 | — | ❌ | **0** | **Flutter not built** |
| Individual Agentic AI Contribution | 12 | One owned agent each: distinct prompt, I/O contract, tool allow-list, validation, error handling, security, tests | ✅ 30 agent-evaluation facts incl. prompt injection | **12** | — |
| API Integration, Security and Cross-Platform | 10 | JWT, RBAC + resource ownership, BCrypt, no secrets committed, backend-enforced approval, CORS allow-list | ✅ 403s verified for employee and support agent | **6** | Top bands require "React **and Flutter** use the same API" |
| Testing, CI and Git Workflow | 8 | 6 test layers, 144 tests, CI with a real PostgreSQL service, k6 perf script | ✅ all tests green | **5** | Flutter tests missing; **git history is yours to create** |

## Honest position

**Reachable now: roughly 78–86 of 100**, depending on how much of the outstanding written and
process work gets done. What is missing splits into three groups:

**1. Flutter (~14 marks).** Nothing in the backend needs to change. See `09-git-ci-and-flutter-gap.md`.

**2. Work only you can do (~5–8 marks).**
- Real git history: incremental commits, feature branches, PRs, code review, a project board.
  §13 and §18.2 explicitly reject back-filled history and final-day bulk uploads.
- Per-student AI usage logs and the one-page reflections (§18.3). Marked, and cross-checked against
  your git history.
- Names against the four components in the README.

**3. Deployment (~3 marks).** Neon, Render and Vercel accounts and the three live URLs. The runbook
is written; the accounts are not created.

## What this project does NOT claim

- Any Flutter functionality.
- LLM-as-a-judge evaluation — deliberately avoided. §12 permits it only as *supporting* evidence,
  and deterministic golden cases are stronger, free and reproducible.
- Load testing beyond a documented k6 smoke run.
- Full-text search. Knowledge search is `ILIKE` plus keyword scoring; the `pg_trgm` upgrade path is
  documented rather than implemented.
