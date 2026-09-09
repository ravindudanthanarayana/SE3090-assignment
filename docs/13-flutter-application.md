# 13. Flutter Mobile Application

The employee self-service client. Built against the **existing** ASP.NET Core API — no new
endpoints, no second database, no separate authentication scheme, no AI logic on the device.

Location: `mobile/smartdesk_mobile/`. Full developer documentation lives in that project's
[`README.md`](../mobile/smartdesk_mobile/README.md); this file is the assignment-facing summary.

---

## 13.1 Why this client exists, and why it is not a second React app

Spec §4.5 requires the two clients to serve **meaningfully different purposes**. They do:

| | React (`frontend/`) | Flutter (`mobile/smartdesk_mobile/`) |
|---|---|---|
| Audience | Support agents, managers, administrators | Employees |
| Core job | Work the queue, assign, escalate, report, **approve AI actions** | Raise a ticket and follow what happens to it |
| Screens | 18 routes incl. Approval Centre, reporting, admin | 8 screens, all employee-facing |
| Cannot do | — | Approve an AI action (403 from the API) |
| Can do that the other cannot | — | **Photograph the problem with the device camera** |

Neither client duplicates the other. The Flutter app is not a subset of the web console; it is the
half of the workflow that belongs to the person with the problem and a phone in their hand.

---

## 13.2 Requirement coverage (spec §8)

Each row names where it can be demonstrated. Nothing is claimed that has not been executed.

| §8 requirement | Implementation | Demonstrate at | Status |
|---|---|---|---|
| Flutter implementation | 3.35.5 / Dart 3.9.2, `flutter analyze` clean, APK builds | `flutter analyze`; `build/app/outputs/flutter-apk/app-debug.apk` | ✅ Verified |
| Reusable widgets | 17 shared widgets plus an `AppDialog` confirm helper: `AppButton`, `AppTextField`, `AppDropdownField`, `TicketCard`, `AppBadge`, `StatusBadge`, `PriorityBadge`, `SlaBadge`, `LoadingView`, `ErrorView`, `EmptyView`, `InlineMessage`, `SectionHeader`, `AppCard`, `AiWorkflowStepTile`, `RecommendationCard`, `AppDialog`, `BrandMark` | `lib/shared/widgets/`; used across every screen | ✅ Verified |
| Navigation / routing | `go_router`; `ShellRoute` for the three tabs, nested `/tickets/:id`, one `redirect` guard for protected routes | `lib/routing/app_router.dart`; sign out from Profile → bounced to Login | ✅ Verified |
| State management | Riverpod — `StateNotifierProvider` for session and filters, `FutureProvider` for server state | ADR-007; `lib/core/providers.dart` | ✅ Verified |
| Secure API integration | One `Dio` client; JWT attached by interceptor; RFC 7807 errors mapped centrally | `lib/core/api/api_client.dart` | ✅ Verified live |
| Registration | `POST /api/auth/register` → Employee + token → straight to dashboard | Sign up screen | ✅ Verified live |
| Login | `POST /api/auth/login` | Login screen | ✅ Verified live |
| Logout | Clears the token from secure storage; router guard returns to Login | Profile → Sign out | ✅ Verified |
| Secure token handling | `flutter_secure_storage` (Keychain / EncryptedSharedPreferences). **Password never stored.** | ADR-007; `auth_controller_test.dart` asserts the password is absent from what is persisted | ✅ Verified |
| Form validation | `Validators` mirrors the DataAnnotations on the server's DTOs; inline messages on every field | `validators_test.dart` (30 cases); `login_form_test.dart` proves invalid input never reaches the API | ✅ Verified |
| Search / filtering | Debounced search + status, priority and category filters — **all server-side** query parameters on `GET /api/tickets` | My Tickets; filter sheet + active-filter chips | ✅ Verified live |
| Main business transaction | Create a ticket, which also starts the agent workflow server-side | Create Ticket → Submit | ✅ Verified live |
| Status tracking | Status, priority, category, SLA badges; Overview details table | Ticket → Overview | ✅ Verified live |
| History | Timeline with actor attribution (employee vs **System / AI**) | Ticket → History | ✅ Verified live |
| Responsive UI | `LayoutBuilder` on the dashboard stats, `Wrap` for badge rows, `maxWidth` constraints on forms, scroll views with keyboard insets | Widget tests pin 320dp layout **and** the keyboard-open case | ✅ Verified |
| Loading states | `LoadingView` on every `FutureProvider` screen | Any screen on first open | ✅ Verified live |
| Empty states | `EmptyView` with a call to action | New account → Home and My Tickets | ✅ Verified |
| Error states | `ErrorView` with Try again; `InlineMessage` for form-level errors | Stop the API and open the app | ✅ Verified live |
| Agentic AI interaction | Ticket creation starts the real workflow; the app polls and renders the recorded run | Ticket → AI Support | ✅ Verified live |
| AI recommendation display | Category, priority, reason, suggested solution, steps, recommended agent, SLA risk, escalation — each rendered only if the backend returned it | Ticket → AI Support | ✅ Verified live |
| Workflow status | Five agents with real names, real per-step status and real timings; plus Completed / Awaiting approval / Failed | Ticket → AI Support | ✅ Verified live |
| Device feature | **Camera + gallery** attachments on ticket creation | Create Ticket → Add a photo | ✅ Verified live (upload → download → byte-identical) |

---

## 13.3 The agents, under their real names

The AI Support checklist is built from `AgentNames` in
`SmartDesk.Application/Agents/Contracts/AgentContracts.cs`. No agent was renamed or invented.

| Shown as | Backend constant | Observed duration in the recorded run |
|---|---|---|
| Planning | `PlannerAgent` | 3.9s |
| Ticket Analysis | `TriageAgent` | 3.7s |
| Knowledge Search | `SolutionAgent` | 4.1s |
| Assignment Analysis | `AssignmentAgent` | 5.4s |
| Validation | `ValidationAgent` | 2.5s |

The employee-facing label is displayed with the raw agent name underneath it, so the checklist is
traceable to the C# during a viva. Statuses and timings come from the persisted `AgentSteps` rows —
nothing is animated by the client.

---

## 13.4 The cross-platform workflow (spec §4.7, §10.2)

Executed end to end against the running API and the Neon database:

| # | Where | What happened |
|---|---|---|
| 1 | **Flutter** | Employee signs in, raises "VPN is not connecting from home" (Network, High) → `TKT-000033` |
| 2 | **ASP.NET Core** | `TicketsController.Create` starts an agent workflow in the background |
| 3 | **Agents** | Planner → Triage → Solution → Assignment → Validation, all succeeded |
| 4 | **Business rules** | Triage: Network / High, urgency 4. Solution matched 1 article — **applied automatically**. Assignment recommended user 3 (Priya Network) |
| 5 | **Approval gate** | Assignment is high-impact → **not applied**. Approval #17 raised; workflow parked at `AwaitingApproval` |
| 6 | **Flutter** | AI Support shows *"Waiting for manager approval"* and the pending approval with its reason and risk level |
| 7 | **React** | Manager (Morgan Manager) opens the Approval Centre and approves, with a note |
| 8 | **ASP.NET Core** | Validates the decision and executes the assignment transactionally |
| 9 | **PostgreSQL** | Ticket → `Assigned`, `AssignedToUserId = 3`; a `TicketHistory` row is written with a **null actor** (system) |
| 10 | **Flutter** | Employee reopens the ticket: Overview reads **Assigned · Priya Network**; History shows **"Assigned to a support agent"** by **System / AI**, note *"Assigned via approved AI recommendation (approval #17)."*; AI Support shows **Completed** and **Approved by Morgan Manager** |

The employee **cannot** short-circuit step 7: `POST /api/ai/approvals/{id}/decision` returns 403
for an Employee, and so does `GET /api/ai/approvals`. That is what makes this a genuine
cross-client workflow rather than two views of the same permissions.

The payloads from this run are committed in `mobile/smartdesk_mobile/test/fixtures/` and asserted
by `test/unit/live_payload_test.dart`.

---

## 13.5 Security verification

Executed against the live API with a real employee token:

| Check | Result |
|---|---|
| Employee lists tickets | Only their own (scoped in SQL, not by the client) |
| Employee reads another user's ticket | **403** |
| Employee decides an approval | **403** |
| Employee lists the approval queue | **403** |
| Employee reads their own ticket's workflow | 200 |
| Request with no token | **401** |
| Request with a forged token | **401** |
| Attachment bytes without a token | **401** |

The Flutter UI hides what an employee cannot do, but the API is the boundary. No secret is present
in the app — only a base URL, and the user's own token at runtime.

---

## 13.6 Backend changes

**None were required for this client.** Every endpoint it calls already existed.

The one feature that would otherwise have needed a backend change — image attachments for the
device feature — was already present in the working tree when this phase began
(`TicketAttachment` entity, `AddTicketAttachments` migration, three endpoints on
`TicketsController`). It was verified working rather than added: upload → list → download returns
byte-identical content, and is 401 without a token.

Two files under `mobile/smartdesk_mobile/android/` differ from the Flutter template, for toolchain
reasons only (see §10 of the app README): Gradle 9.1 + AGP 8.13 so the build runs on Java 25, and
the unused `ndkVersion` pin removed.

---

## 13.7 Tests — 92

Run with `flutter test` from `mobile/smartdesk_mobile/`.

| Layer | File | Count |
|---|---|---|
| Form validation | `test/unit/validators_test.dart` | 30 assertions across 11 tests |
| Model parsing | `test/unit/models_test.dart` | 14 |
| API error handling | `test/unit/api_error_test.dart` | 10 |
| Authentication state | `test/unit/auth_controller_test.dart` | 9 |
| AI workflow view model | `test/unit/workflow_steps_test.dart` | 6 |
| History wording | `test/unit/history_headline_test.dart` | 6 |
| **Real captured API payloads** | `test/unit/live_payload_test.dart` | 9 |
| Reusable widgets + responsive + dark mode | `test/widget/widgets_test.dart` | 15 |
| Login form behaviour | `test/widget/login_form_test.dart` | 6 |

Two of these caught real defects during development, which is the point of writing them:

- The 320dp layout test found a **120px overflow** in the "New here? / Create an account" row.
- The workflow-step test found the Planner being reported as *running* when no workflow existed.
