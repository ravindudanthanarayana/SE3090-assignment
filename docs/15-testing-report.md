# SmartDesk AI testing report

## 1. Scope

This report records the test evidence available for the SmartDesk AI web, API, mobile and Agentic AI
subsystems. It distinguishes executed results from planned or manually documented evidence. No result is
marked as passing unless it was observed or is backed by a committed test assertion.

Test date: 2026-10-08

## 2. Test inventory

| Area | Tool / evidence | Result |
|---|---|---|
| ASP.NET Core unit, service and agent tests | xUnit in `backend/SmartDesk.Tests` | 99 passed; 30 failed during local database initialization because the PostgreSQL password did not match the running local server |
| PostgreSQL integration tests | xUnit + real PostgreSQL fixture | Test code exists; local execution is blocked by the database authentication error above |
| API authorization and integration smoke checks | Live Render API | Passed for login, `/api/auth/me`, ticket listing, role-restricted approvals, assignment, knowledge, reports and workflow endpoints |
| React component and page tests | Vitest + React Testing Library | 34 passed |
| React production build | TypeScript compiler + Vite | Passed |
| Flutter unit and widget tests | `flutter test` | 92 passed |
| Flutter static analysis | `flutter analyze --fatal-infos` | Passed with no issues |
| Agentic AI evaluation tests | `AgentEvaluationTests.cs` and deterministic scripted client | Present in the repository; full local execution depends on the PostgreSQL test fixture |
| Performance testing | `perf/smoke.js` with k6 | Script is present; a measured run and result summary are still required |

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

The 30 local backend failures are environment failures, not assertion failures: the test fixture attempted to
connect to PostgreSQL as `postgres`, but the running local server rejected the configured password. The CI
workflow defines a fresh PostgreSQL service with matching credentials; that CI run must be retained as the
authoritative integration-test evidence.

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

No measured result is claimed in this report because k6 was not executed during this verification. The team
must run the script against the intended API/database configuration and append the generated summary, provider,
database location, machine and timestamp before submission.

## 8. Known test limitations and actions

1. Configure `TEST_DATABASE_CONNECTION_STRING` to a reachable PostgreSQL instance and rerun `dotnet test`.
2. Retain the passing GitHub Actions backend artifact, including the PostgreSQL service run.
3. Execute k6 and add the performance summary.
4. Build and test the Flutter APK on a real Android target.
5. Repeat and record the complete Flutter-to-React approval workflow for the demonstration.

These actions are evidence requirements; they should not be replaced with invented output or back-filled test
results.
