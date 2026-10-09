# Marking-Scheme Coverage Matrix (spec §16.1)

Status after the **web and Flutter** phases. **Verified** means it was executed and observed, not
just written.

## Group contribution — 30 marks

| Criterion | Marks | Evidence | Verified? | Cap now | Blocked by |
|---|---|---|---|---|---|
| Component Design and Business Logic | 10 | 4 components, each ≥4 endpoints + a real non-CRUD operation. Status machine, SLA engine, assignment scorer, article relevance, approval executor — all deterministic C# with unit tests. | ✅ Core behavior is implemented; full end-to-end demonstration evidence remains required | **10** | Final demonstration |
| Integrated Architecture, Agent Orchestration and State Management | 10 | 5 distinct agents, `AgentWorkflows`/`AgentSteps`/`AgentToolCalls`/`AiApprovals`, allow-listed tools, two-layer deterministic validation, auditable logs with timings and retries, safe failure, authorized human approval. **Both clients now integrate**: Flutter employee → agents → approval gate → React manager → backend action → Flutter sees the result | ✅ observed live end to end (`docs/13-flutter-application.md` §13.4) | **10** | — |
| Documentation and Deployment | 10 | README, design docs, 7 ADRs, Swagger, `/health`, deployment runbook, AI-usage template, Vercel/Render deployment | ⚠ live services verified; consolidated report, AI logs, video and APK evidence still outstanding | **8** | Submission evidence |

## Individual contribution — 70 marks (per student)

| Criterion | Marks | Evidence | Verified? | Cap now | Blocked by |
|---|---|---|---|---|---|
| ASP.NET Core RESTful API | 10 | Controllers → services → `IAppDbContext`, DTOs with validation, async throughout, RFC 7807 errors, correct status codes incl. 409/422/503, Swagger with JWT | ✅ 18 API tests over real HTTP | **10** | — |
| PostgreSQL Integration and Data Modelling | 10 | Relational entities, foreign keys, constraints, indexes, `jsonb`, `text[]`, migrations, seed data and transactional approval | ✅ The team manually checked these flows; the local database test run still needs the correct PostgreSQL connection | **10** | DB test evidence |
| React Web Application | 10 | 18 routes, reusable components, Context API, protected routes, role-aware nav, validation, loading/empty/error/success states, responsive, charts | ✅ build and 34 Vitest tests pass; live root and deep links return 200 | **10** | — |
| Flutter Mobile Application | 10 | 8 screens, 17 reusable widgets, `go_router` with a protected-route guard, Riverpod, secure JWT storage, form validation, server-side search/filter, loading/empty/error states, light + dark, camera device feature, AI workflow + recommendation display | ✅ 92 tests; run live against the API; APK builds | **10** | — |
| Individual Agentic AI Contribution | 12 | One owned agent each: distinct prompt, I/O contract, tool allow-list, validation, error handling, security, tests | ✅ 30 agent-evaluation facts incl. prompt injection | **12** | — |
| API Integration, Security and Cross-Platform | 10 | JWT, RBAC + resource ownership, BCrypt, no secrets committed, backend-enforced approval, CORS allow-list. **React and Flutter call the same endpoints**; Flutter stores its token in the platform keystore | ✅ 403/401 matrix re-verified from a real employee token (`docs/13-flutter-application.md` §13.5) | **10** | — |
| Testing, CI and Git Workflow | 8 | Backend, React, Flutter, Agentic AI and k6 test assets; GitHub Actions includes PostgreSQL, web and Flutter jobs | ⚠ React 34/34, Flutter 92/92 and analyzer pass; local backend run stopped at 99 passed/30 database-setup failures; deployed k6 results are recorded but thresholds failed | **6** | DB/performance fixes + contribution evidence |

## Honest position

**The implementation is close to evaluation-ready, but the final mark cannot be certified from the repository alone.**
The remaining work is mainly evidence, reproducibility and the final demonstration.

**1. Work only you can do (~5–8 marks).**
- Real git history: incremental commits, feature branches, PRs, code review, a project board.
  §13 and §18.2 explicitly reject back-filled history and final-day bulk uploads.
- Per-student AI usage logs and the one-page reflections (§18.3). Marked, and cross-checked against
  your git history.
- Names against the four components in the README.

**2. Deployment and artifacts.** Render health/Swagger and Vercel routes are live. The team must retain
the deployed URLs, a runnable APK artifact and the required PostgreSQL/deployment evidence in the final report.

## What this project does NOT claim

- A complete submission package — the live services are available, but the consolidated report, video and
  individual evidence are not part of this repository.
- Light mode having been eyeballed on a device. It is implemented from the same tokens as dark and
  covered by widget tests, but the live walkthrough was done on a dark-mode machine.
- Flutter integration tests driving a real device or emulator. The 92 tests are unit and widget
  tests, plus assertions against **real captured API payloads**; the end-to-end run was performed
  and observed manually rather than through a scripted run.
- LLM-as-a-judge evaluation — deliberately avoided. §12 permits it only as *supporting* evidence,
  and deterministic golden cases are stronger, free and reproducible.
- Load testing beyond a documented k6 smoke run.
- Full-text search. Knowledge search is `ILIKE` plus keyword scoring; the `pg_trgm` upgrade path is
  documented rather than implemented.
