<div align="center">

<img src="assets/brand/logo-mark.png" alt="SmartDesk AI" width="76">

# SmartDesk AI — Mobile

**Employee self-service, on a phone.**

Raise a ticket, photograph the problem, and watch the Agentic AI workflow handle it.

[![Flutter](https://img.shields.io/badge/Flutter-3.35.5-02569B?style=flat-square&logo=flutter&logoColor=white)](https://flutter.dev)
[![Dart](https://img.shields.io/badge/Dart-3.9.2-0175C2?style=flat-square&logo=dart&logoColor=white)](https://dart.dev)
[![Riverpod](https://img.shields.io/badge/state-Riverpod-4a90d9?style=flat-square)](../../docs/ADRs/ADR-007-flutter-state-management.md)
[![Tests](https://img.shields.io/badge/tests-92%20passing-2ea44f?style=flat-square)](#8-tests--92)
[![Analyze](https://img.shields.io/badge/analyze-0%20issues-2ea44f?style=flat-square)](#1-running-it)

</div>

---

A new **client** for the ASP.NET Core API that already exists in `backend/` — not a new system.
It adds **no** database, no authentication scheme of its own, and no AI logic: the five agents run
on the server, and this app displays what they recorded. **No backend endpoint was added for it.**

The React app in `frontend/` remains the staff, manager, admin, Approval Centre and reporting
console. This app is only for the person who raises the ticket.

```
Flutter (employee)  ─┐
                     ├─→  ASP.NET Core API  →  EF Core  →  PostgreSQL (Neon)
React (staff/manager)┘         │
                               └─→ Agentic AI orchestrator (Planner → Triage → Solution
                                    → Assignment → Validation) → human approval gate
```

### At a glance

| | |
|---|---|
| **Screens** | 8 — Splash, Login, Sign up, Home, My Tickets, Create Ticket, Ticket detail *(4 tabs)*, Profile |
| **Reusable widgets** | 17 |
| **State** | Riverpod ([ADR-007](../../docs/ADRs/ADR-007-flutter-state-management.md)) |
| **Routing** | `go_router`, one redirect guard for protected routes |
| **Token storage** | `flutter_secure_storage` — platform keystore. Password never stored |
| **Device feature** | Camera + gallery attachments |
| **Themes** | Light and dark, from the web app's own tokens |
| **Tests** | 92, incl. 9 against real captured API payloads |
| **Source** | 44 Dart files, ~6,300 lines |

---

## 1. Running it

**Prerequisites:** Flutter 3.35+ (Dart 3.9+), and the SmartDesk API running.

```bash
# 1. Start the backend (from the repository root)
cd backend/SmartDesk.Api
set -a && source ../../.env && set +a
ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://localhost:5299

# 2. Run the app (from this directory)
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5299     # Android emulator
```

### Choosing the right `API_BASE_URL`

`10.0.2.2` is the Android emulator's alias for the host machine's `localhost`. It is the default
compiled into `AppConfig`, so a plain `flutter run` works on an emulator with no flags. Everything
else needs the flag:

| Target | `--dart-define=API_BASE_URL=` |
|---|---|
| Android emulator | `http://10.0.2.2:5299` (the default — flag optional) |
| iOS simulator | `http://localhost:5299` |
| Physical phone | `http://<your-computer's-LAN-IP>:5299` — e.g. `http://192.168.1.20:5299`. The phone and the computer must be on the same network, and the API must be started with `--urls http://0.0.0.0:5299` so it listens on more than loopback. |
| Deployed API | `https://your-api-domain` |

The current value is printed under the Sign in button, so you can confirm at a glance which API a
build is pointing at during a demonstration.

**Why a `--dart-define` and not a `.env` file:** it is compiled in, so there is no configuration
file to ship, lose or accidentally commit. It holds a base URL only. No secret is ever present in
this app — the database password, the JWT signing key and the AI API key all stay inside the
ASP.NET Core process.

### Building an APK

```bash
flutter build apk --debug     # build/app/outputs/flutter-apk/app-debug.apk
flutter build apk --release
```

### Tests

```bash
flutter analyze     # 0 issues
flutter test        # 92 tests
```

---

## 2. Screens

| Screen | Route | What it does |
|---|---|---|
| Splash | `/` | Reads the stored JWT and verifies it against `GET /api/auth/me` before trusting it. |
| Login | `/login` | `POST /api/auth/login`. Validation, loading, invalid-credential and network-error states. |
| Sign up | `/register` | `POST /api/auth/register`. Self-registration always creates an **Employee**; the endpoint returns a token, so a successful sign-up lands on the dashboard. |
| Home | `/home` | Greeting, Open / In progress / Resolved counts, recent tickets, Create Ticket. |
| My Tickets | `/tickets` | Server-side search, status/priority/category filters, pagination. |
| Create Ticket | `/tickets/new` | Title, description, category, priority — plus **camera / gallery attachments**. |
| Ticket detail | `/tickets/:id` | Four tabs: **Overview**, **AI Support**, **Comments**, **History**. |
| Profile | `/profile` | User details, role, and sign out (clears the token from secure storage). |

Navigation is `go_router`. Route protection is **one** `redirect` guard: an unauthenticated
session is sent to Login from anywhere, and an authenticated one cannot land back on the auth
pages. That is also what makes an expired token an automatic sign-out — the auth state flips, the
router re-evaluates, and no screen has to handle it.

---

## 3. Architecture

```
lib/
├── core/
│   ├── config/app_config.dart        API base URL from --dart-define
│   ├── theme/app_theme.dart          light + dark tokens, copied from the web app's index.css
│   ├── api/api_client.dart           the ONE Dio instance: JWT header + 401 handling
│   ├── api/api_exception.dart        RFC 7807 ProblemDetails → one readable sentence
│   ├── storage/secure_token_store.dart   Keychain / EncryptedSharedPreferences
│   └── providers.dart                composition root
├── features/
│   ├── auth/       domain (User, Validators) · data (AuthRepository) · state · ui
│   ├── tickets/    domain (models, enums) · data (TicketRepository) · state · ui
│   └── ai/         domain (WorkflowDetail, Approval) · data (AiRepository) · state · ui
├── shared/
│   ├── widgets/    AppButton, AppTextField, TicketCard, StatusBadge, PriorityBadge,
│   │               SlaBadge, LoadingView, ErrorView, EmptyView, InlineMessage,
│   │               SectionHeader, AppCard, AiWorkflowStepTile, RecommendationCard,
│   │               AppDialog, BrandMark
│   └── format.dart dates and relative times
└── routing/        app_router.dart (guard) · app_shell.dart (bottom navigation)
```

Each feature is `domain / data / state / ui`, which mirrors how the backend is split into
Domain / Application / Infrastructure / Api.

### The API layer

`ApiClient` is the only place in the app that makes an HTTP request. Two interceptors carry the
whole authentication story:

- **request** — attaches `Authorization: Bearer <jwt>` to every call, so no screen can forget it;
- **response** — on a 401, clears secure storage and notifies `AuthController`, so an expired
  token is handled in exactly one place.

Three repositories sit on top: `AuthRepository`, `TicketRepository`, `AiRepository`.

### State management

Riverpod. `StateNotifierProvider` for state we own (session, filters); `FutureProvider` for state
the server owns (every list and detail). See
[`docs/ADRs/ADR-007-flutter-state-management.md`](../../docs/ADRs/ADR-007-flutter-state-management.md)
for the options considered and the reasoning.

### Branding

The app uses the **same logo and the same colour tokens as the web app**, so the two clients read as
one product rather than two projects that happen to share an API.

| | |
|---|---|
| **Mark** | `assets/brand/logo-mark.png`, copied from `frontend/public/logo-mark.png`. Keep the two in step if the logo changes. |
| **Wordmark** | Set as **text**, not the supplied `logo-wordmark.png` — for the same reason the web app does it: that bitmap is dark navy and would disappear on the dark theme, whereas text picks up the theme's foreground colour and stays crisp at any size. |
| **Colours** | `AppColors` in `lib/core/theme/app_theme.dart` is a direct transcription of the tokens in `frontend/src/index.css`, including the dark theme's lighter orange (`#fb8c3b`), which exists because `#f26522` lacks contrast on a dark ground. |
| **App icons** | Android launcher icons (5 densities) and iOS app icons (15 sizes) are generated from the same mark. iOS icons are flattened onto white, because iOS rejects alpha in an app icon. |

`BrandMark` is wrapped in a `FittedBox`, because the horizontal lockup is wider than a 320dp screen
at the larger sizes and must scale down rather than overflow.

### Security

- The JWT lives in `flutter_secure_storage` (platform keystore), never `SharedPreferences`.
- **The password is never stored.** Only the token and a cached profile.
- 401 → clear session → Login. 403 → "You do not have permission to do that."
- The client hides what an employee cannot do, but it is **not** the authorization boundary. The
  API scopes an employee to their own tickets inside the SQL query and re-checks the role on every
  request. Verified: an employee reading another user's ticket gets 403; deciding an approval gets
  403; a request with no token or a forged token gets 401.
- The app never touches PostgreSQL or the AI provider directly.

---

## 4. API endpoints used

Every one of these already existed. **No endpoint was added for this client.**

| Endpoint | Used by |
|---|---|
| `POST /api/auth/register` · `POST /api/auth/login` · `GET /api/auth/me` | Sign up, Login, Splash |
| `GET /api/tickets` | My Tickets (search, filters, pagination), Home counts |
| `GET /api/tickets/{id}` | Ticket Overview |
| `POST /api/tickets` | Create Ticket — **also starts the AI workflow server-side** |
| `GET`/`POST /api/tickets/{id}/comments` | Comments tab |
| `GET /api/tickets/{id}/history` | History tab |
| `GET`/`POST /api/tickets/{id}/attachments` · `GET .../{id}/content` | Device feature |
| `GET /api/categories` | Create Ticket form, category filter |
| `GET /api/ai/workflows?ticketId=` · `GET /api/ai/workflows/{id}` | AI Support tab |

---

## 5. The Agentic AI integration

**No AI runs on the device.** Creating a ticket causes the *server* to start a workflow. This app
polls `GET /api/ai/workflows/{id}` and renders what the orchestrator recorded.

The AI Support tab shows the five agents under **their real backend names**, taken from
`AgentNames` in `SmartDesk.Application/Agents/Contracts/AgentContracts.cs`:

| Shown as | Backend agent |
|---|---|
| Planning | `PlannerAgent` |
| Ticket Analysis | `TriageAgent` |
| Knowledge Search | `SolutionAgent` |
| Assignment Analysis | `AssignmentAgent` |
| Validation | `ValidationAgent` |

The employee-facing label is shown with the raw agent name underneath it, so the checklist can be
traced straight back to the C# during a demonstration. Each row's tick, spinner or cross is the
`AgentStepStatus` the server persisted, and the timing is the real recorded `durationMs` — nothing
is advanced by a client-side animation. An agent the plan skipped shows as pending rather than
being hidden.

Below that, the **AI recommendation** is parsed from `finalOutcomeJson`: triage category and
priority, the reason, the suggested solution and steps, the recommended agent, the SLA risk and
whether escalation is recommended. Every row is conditional — if the backend did not return a
field, the card does not show it, so it cannot claim the agents concluded something they did not.
`appliedActions` and `pendingApprovalActions` are shown separately, because the difference between
"the system did this" and "a manager still has to approve this" is the point of the whole design.

---

## 6. Device feature — camera / image picker

An employee reporting "the printer shows an error" can photograph the screen instead of typing the
code out. That is something the web console genuinely cannot do as well, which is what makes it a
*meaningful* device feature rather than a box-tick.

`image_picker` offers **Take a photo** (camera) or **Choose an image** (gallery) on the Create
Ticket form, up to three images. The picker resizes to 1600px at 80% quality on the device, which
keeps the upload inside the server's 5 MB limit without an image-processing dependency.

**The order matters.** The ticket is created first, then each image is uploaded to
`POST /api/tickets/{id}/attachments` — an attachment needs a ticket to belong to. So a rejected
image never costs the user their ticket; it is reported separately in the confirmation.

Permissions are declared in `android/app/src/main/AndroidManifest.xml` (camera, optional) and
`ios/Runner/Info.plist` (`NSCameraUsageDescription`, `NSPhotoLibraryUsageDescription` — iOS
refuses to open either without them).

Attachments are stored **with the ticket in PostgreSQL**, not in a public folder, so the same
ownership check protects the metadata and the bytes. Verified: fetching an attachment without a
token returns 401.

---

## 7. The cross-client demonstration

This is the scenario the whole system exists to show, and it has been run end to end against the
live API and the Neon database:

1. **Flutter** — the employee signs in and raises "VPN is not connecting from home" (Network, High).
2. **ASP.NET Core** — creating the ticket starts an agent workflow in the background.
3. **The agents run**: Planner → Triage → Solution → Assignment → Validation.
4. Triage classified it Network / High, urgency 4. Solution matched a knowledge article. Assignment
   recommended agent #3 (Priya Network) as the highest-scoring candidate.
5. Assignment is **high-impact**, so the orchestrator did *not* apply it. It raised an approval and
   the workflow parked at `AwaitingApproval`. The Flutter AI Support tab showed
   *"Waiting for manager approval"*.
6. **React** — the manager opens the Approval Centre and approves it.
7. **ASP.NET Core** validates the decision and executes the action transactionally.
8. **Flutter** — the employee reopens the ticket:
   - Overview now reads **Status: Assigned · Assigned to: Priya Network**
   - History shows a second entry, **"Assigned to a support agent"**, attributed to **System / AI**,
     with the note *"Assigned via approved AI recommendation (approval #17)."*
   - AI Support shows the workflow as **Completed** and the approval as **Approved by Morgan Manager**.

An employee cannot approve anything — the API returns 403 — which is exactly why the manager's step
has to happen in the other client.

The payloads captured during that run are committed in `test/fixtures/` and asserted by
`test/unit/live_payload_test.dart`, so the models are pinned against what the server really sends.

---

## 8. Tests — 92

| File | Covers |
|---|---|
| `test/unit/validators_test.dart` | Login and ticket form rules, matched to the DataAnnotations on the server's DTOs. |
| `test/unit/models_test.dart` | Parsing every DTO; unknown enum values degrade instead of crashing. |
| `test/unit/api_error_test.dart` | ProblemDetails mapping; 401 / 403 / 404 / 409 / 5xx; timeout and no-connection. |
| `test/unit/auth_controller_test.dart` | Session restore, login, register, logout — and that the password is never persisted. |
| `test/unit/workflow_steps_test.dart` | The agent checklist merge; a skipped agent is reported, not hidden. |
| `test/unit/history_headline_test.dart` | Timeline wording, including not printing a raw user id at the user. |
| `test/unit/live_payload_test.dart` | **Real captured API responses** from the run described above. |
| `test/widget/widgets_test.dart` | Reusable widgets, loading/empty/error states, dark mode, 320dp layout. |
| `test/widget/login_form_test.dart` | Validation blocks submission before any network call; invalid credentials surface; keyboard-open layout on a small phone. |

The small-phone test earned its place immediately: it caught a real 120px overflow in the
"New here? / Create an account" row at 320dp, which is now a `Wrap`.

---

## 9. Packages

| Package | Why |
|---|---|
| `flutter_riverpod` | State management — see ADR-007. |
| `go_router` | Declarative routing with one redirect guard for protected routes. |
| `dio` | HTTP client. Interceptors are what let the JWT header and 401 handling live in one place. |
| `flutter_secure_storage` | Keychain / EncryptedSharedPreferences for the JWT. |
| `image_picker` | The device feature: camera and gallery. |
| `http_parser` | `MediaType` so the multipart upload is tagged `image/*` and passes the server's check. |
| `intl` | Date formatting on the timeline. |

Nothing else was added.

---

## 10. Known limitations

- **Android toolchain.** The Flutter template pins Gradle 8.12, which refuses to run on the Java 25
  bundled with current Android Studio, so `android/` is on **Gradle 9.1 + AGP 8.13**. The template's
  `ndkVersion` pin was also removed — this app has no native sources and no plugin needs the NDK, so
  requiring it only added a large download that could fail the build. Restore that line if a future
  plugin needs it.
- **Light mode** is implemented from the same tokens as dark and is covered by widget tests; every
  live walkthrough so far has been on a dark-mode machine, so it has not been eyeballed on a device.
- **The end-to-end cross-client run was performed manually**, not by an automated integration test
  driving an emulator. The 92 tests are unit and widget tests plus assertions against the payloads
  captured during that run.
- The app **reads** the AI workflow and shows approval status; deciding an approval is deliberately
  a React/manager action, and the API enforces that.
