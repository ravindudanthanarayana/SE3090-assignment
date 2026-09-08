# Git / CI Strategy, and the Flutter Gap

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
```
That satisfies "restores, builds and runs the automated backend tests on every push and pull request to main",
plus the encouraged frontend pipeline.

## 3. Flutter gap analysis

Flutter is being built separately. Concretely, this phase leaves these spec items open:

| Spec item | Marks at stake | Status after this phase |
|---|---|---|
| §8 Flutter application | **10 individual** | Not started. Backend requires **no changes** to support it. |
| §14.4 runnable APK | part of Documentation & Deployment (group 10) | Pending |
| §12 Flutter tests | part of Testing/CI/Git (8) | Pending |
| §4.7 / §10.2 cross-platform end-to-end workflow | part of Integrated Architecture (group 10) + API Integration & Cross-Platform (10) | **Demonstrable today in React alone** (employee role initiates, manager role approves). Becomes fully compliant when the employee half moves to Flutter. |
| §14.2 ADR for Flutter state management | part of Documentation (group 10) | Placeholder ADR-007 stub left |

**What we do now to make Flutter cheap later**
1. React is deliberately scoped to the *staff/manager/admin/AI-approval* side; Flutter's role (employee
   self-service: register, login, raise ticket, track status, view AI suggestion, comment) is left free —
   this is the "meaningful and different purposes" requirement of §4.5.
2. Every endpoint Flutter needs already exists and is listed in `05-api-design.md`.
3. JWT in an `Authorization` header, no cookies, no session affinity → works from Dart unchanged.
4. All list endpoints return the same `{items, page, pageSize, totalCount, totalPages}` envelope.
5. Errors are uniform `ProblemDetails`, so one Dart error mapper covers the whole API.
6. Device feature (§8, required): the natural fit is **image attachment via camera/image picker** on ticket
   creation. To keep that from becoming a backend change later, we will include a simple attachment field on
   tickets now — decide with you whether to include it in this phase.
