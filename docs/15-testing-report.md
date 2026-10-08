# SmartDesk AI - Testing Report

## 1. What this report covers

This report summarises the checks completed for the SmartDesk AI web app, API, mobile app and Agentic AI
workflow. Where a check was not run, it is stated clearly rather than presented as a pass.

Test date: 2026-10-08

## 2. Test summary

| Area | Tool / evidence | Result |
|---|---|---|
| ASP.NET Core unit, service and Agentic AI tests | xUnit in `backend/SmartDesk.Tests` | Local run: 99 passed; 30 stopped during database setup. The team also checked the affected flows manually. |
| PostgreSQL integration tests | xUnit with a real PostgreSQL fixture | Manual checks passed; the local test run still needs the correct PostgreSQL connection |
| API authorization and integration smoke checks | Live Render API | Passed for login, `/api/auth/me`, ticket listing, role-restricted approvals, assignment, knowledge, reports and workflow endpoints |
| React component and page tests | Vitest + React Testing Library | 34 passed |
| React production build | TypeScript compiler + Vite | Passed |
| Flutter unit and widget tests | `flutter test` | 92 passed |
| Flutter static analysis | `flutter analyze --fatal-infos` | Passed with no issues |
| Agentic AI evaluation tests | `AgentEvaluationTests.cs` and deterministic scripted client | Present in the repository; full local execution depends on the PostgreSQL test fixture |
| Performance testing | `perf/smoke.js` with k6 | The main functions were checked manually; k6 timing/error measurements are not recorded |

## 3. Backend and API coverage

The backend tests cover business rules, authentication, authorization, controller/API behavior, database
constraints and Agentic AI behavior. The live API smoke checks confirmed:

- Employee login and manager login succeed.
- `/api/auth/me` returns the authenticated profile.
- Employees receive their own ticket list.
- Employees receive `403 Forbidden` for the approval queue.
- Managers can read the approval queue.
- Support-agent workload and assignment recommendations return structured data.
- Knowledge article listing/detail and ticket-relevant article ranking return data.
- Dashboard, SLA and agent-performance reports return data.
- AI workflow and approval endpoints return structured workflow state.
- CORS allows the deployed Vercel origin.

The 30 backend failures came from the test setup, not from failed application assertions. The test fixture tried
to connect as `postgres`, but the local PostgreSQL server rejected the configured password. The team manually
checked the affected flows successfully. The CI workflow creates a fresh PostgreSQL service, so its result should
be kept as supporting evidence.

## 4. React testing

The React suite covers protected routes, authentication form behavior, UI states, theme behavior, ticket list
behavior and approval-center behavior. The production build also passed TypeScript checking and Vite bundling.

The deployed application was checked after the Vercel rewrite fix:

| URL | Result |
|---|---|
| `https://se3090.vercel.app/` | HTTP 200 |
| `https://se3090.vercel.app/app/dashboard` | HTTP 200 |
| `https://se3090.vercel.app/signin` | HTTP 200 |

This confirms that direct navigation and browser refresh no longer return Vercel 404 pages for client routes.

## 5. Flutter testing

The Flutter suite contains unit and widget tests for:

- API error mapping and HTTP status handling.
- Authentication state, secure token behavior and logout.
- DTO/model parsing from captured API payloads.
- Ticket validators and form validation.
- Workflow step rendering and status interpretation.
- History wording and actor attribution.
- Login form validation, API submission and responsive layouts.
- Loading, empty, error and theme states.

Observed result: 92 tests passed and `flutter analyze --fatal-infos` reported no issues. A final submission
should still retain a runnable APK from CI and test it on an Android device or emulator.

## 6. Agentic AI evaluation

The repository contains deterministic Agentic AI evaluation tests for planning, delegation, agent/tool
selection, structured output validation, business rules, approval enforcement, prompt-injection resistance,
failure recovery and safe failure. The workflow implementation persists agent steps, tool calls, timings,
retries, validation outcomes and approval decisions.

The complete cross-client scenario is documented in `docs/13-flutter-application.md` §13.4. The group must
reproduce this scenario during the demonstration and retain screenshots or logs showing the Flutter request,
React approval and final Flutter status update.

## 7. Performance testing

`perf/smoke.js` defines the required load scenarios and thresholds:

- Ticket-list p95 below 800 ms.
- Dashboard p95 below 1000 ms.
- Agent workflow p95 below 20 seconds.
- Error rate below 5%.

The team manually checked that the main performance-related functions behaved correctly. No k6 measurement is
claimed because the script was not run during this review. If the final report needs numerical thresholds, run
the script and add its summary, provider, database location, machine and timestamp.

## 8. Known test limitations and actions

1. Configure `TEST_DATABASE_CONNECTION_STRING` to a reachable PostgreSQL instance and rerun `dotnet test`.
2. Retain the passing GitHub Actions backend artifact, including the PostgreSQL service run.
3. Execute k6 if quantitative performance thresholds are required in the final report.
4. Build and test the Flutter APK on a real Android target.
5. Record the manually verified Flutter-to-React approval workflow for the demonstration.

These are the remaining evidence steps. They should be completed with real output rather than copied or guessed
results.
