# Git / CI Strategy, and the Flutter Gap (now closed)

## 1. Git and GitHub (spec §13)

**Branches**
```
main                    protected; PRs only; CI must pass
develop                 integration branch
feature/ticket-management        (S1)
feature/assignment-management    (S2)
feature/knowledge-base           (S3)
feature/sla-reporting            (S4)
feature/agent-triage             (S1)
feature/agent-assignment         (S2)
feature/agent-solution           (S3)
feature/agent-validation         (S4)
feature/agent-orchestrator       (shared / pair-reviewed)
```
Each student's business branch **and** agent branch are separate so the "distinct Agentic AI contribution"
(§3.3) is visible in the git history on its own.

**Practice**: one GitHub Issue per feature, labelled by component and by owner · branch named after the issue ·
PR references the issue and is reviewed by a different student before merge · a GitHub Projects board with
Todo / In Progress / In Review / Done. Merge conflicts resolved in PRs, which is itself the evidence §13 asks for.

**Warning carried from the spec**: §13 and §18.2 explicitly reject back-filled commit history and final-day
bulk uploads. The code produced here must be committed incrementally by the students as they read and
understand it, and each student must be able to explain their own commits at the viva. This tool cannot
create that evidence for you.

## 2. CI (spec §13 — mandatory)

`.github/workflows/ci.yml`, triggered on **push and pull_request to `main`** (and `develop`):

```
job: backend
  services: postgres:16   (health-checked)
  - setup-dotnet 8
  - dotnet restore
  - dotnet build --no-restore -c Release
  - dotnet test  --no-build -c Release   (env: test connection string, ScriptedLlmClient)
job: frontend
  - setup-node 20
  - npm ci
  - npm run build
  - npm run test -- --run
job: mobile
  - flutter-action 3.35.5 (pinned)
  - flutter pub get
  - flutter analyze --fatal-infos
  - flutter test
  - flutter build apk --debug   → uploaded as an artifact
```
That satisfies "restores, builds and runs the automated backend tests on every push and pull request to main",
plus the encouraged frontend pipeline, plus the Flutter tests §12 asks for. The APK artifact is what
§14.4 wants, produced by CI rather than by hand.

## 3. Flutter gap analysis — CLOSED

The app is built and lives in `mobile/smartdesk_mobile/`. See
[`13-flutter-application.md`](13-flutter-application.md) for the full write-up.

| Spec item | Marks at stake | Status |
|---|---|---|
| §8 Flutter application | **10 individual** | ✅ Built. 8 screens, 17 reusable widgets, Riverpod, go_router, secure JWT storage, camera device feature. Backend required **no changes**. |
| §14.4 runnable APK | part of Documentation & Deployment (group 10) | ✅ `flutter build apk` succeeds; CI uploads it as an artifact |
| §12 Flutter tests | part of Testing/CI/Git (8) | ✅ 92 tests, including 9 against **real captured API payloads** |
| §4.7 / §10.2 cross-platform end-to-end workflow | part of Integrated Architecture (group 10) + API Integration & Cross-Platform (10) | ✅ **Executed end to end**: Flutter employee raises a ticket → 5 agents run → approval gate → React manager approves → backend acts → Flutter shows the updated status and history. Documented in §13.4. |
| §14.2 ADR for Flutter state management | part of Documentation (group 10) | ✅ ADR-007 decided (Riverpod), with the options and consequences written up |

**Why it was cheap, as predicted**
1. React is deliberately scoped to the *staff/manager/admin/AI-approval* side; Flutter's role (employee
   self-service: register, login, raise ticket, track status, view AI suggestion, comment) is left free —
   this is the "meaningful and different purposes" requirement of §4.5.
2. Every endpoint Flutter needs already exists and is listed in `05-api-design.md`.
3. JWT in an `Authorization` header, no cookies, no session affinity → works from Dart unchanged.
4. All list endpoints return the same `{items, page, pageSize, totalCount, totalPages}` envelope.
5. Errors are uniform `ProblemDetails`, so one Dart error mapper covers the whole API.
6. Device feature (§8, required): **image attachment via camera/image picker** on ticket creation.
   The attachment endpoints were already in place by the time the app was built, so this needed no
   backend work either — upload, list and authorised download were verified byte-for-byte.

Every one of those six predictions held. The only files outside `mobile/` that changed were
documentation and the CI workflow.
