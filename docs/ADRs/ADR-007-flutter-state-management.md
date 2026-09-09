# ADR-007: Flutter state management

**Status:** Accepted · **Date:** 2026-09-09 · **Required by:** spec §14.2

## Context

The Flutter application is the **employee self-service** client for SmartDesk AI. Its job is
deliberately narrower than the React console's (spec §4.5 asks the two clients to serve
meaningful and *different* purposes): register, sign in, raise a ticket with an optional photo,
watch the Agentic AI workflow analyse it, read the AI recommendation, comment, and follow the
status and history. The staff, manager, admin and Approval Centre screens stay in React.

The state the app has to manage is therefore of two kinds:

1. **Session state** — one JWT and the profile it belongs to. Long-lived, read by the router on
   every navigation, and invalidated from a place far away from any screen (a 401 on any request).
2. **Server state** — tickets, comments, history, attachments, categories, and the agent workflow.
   Every one of these is *owned by the API*, fetched per screen, and has three visual states
   (loading / data / error) that spec §8 requires us to handle explicitly.

Nothing in the app is genuinely client-owned mutable state. That observation drove the decision.

## Options considered

| Option | What it would give | What it would cost |
|---|---|---|
| `setState` + a service layer | No dependency. Easy to explain. | Every screen re-implements loading/error handling; the session would have to be threaded through constructors or a global singleton; a 401 could not cleanly reach the router. Rejected: it makes the *repeated* part of the work manual, which is where bugs live. |
| `provider` | Familiar, small, officially recommended for a long time. | `Provider.of<T>` is resolved at runtime, so a missing provider is a runtime crash rather than a compile error. No first-class async state — we would hand-roll the loading/error union anyway. |
| `bloc` | Excellent for complex event-driven flows; very explicit and testable. | An event class, a state class and a bloc per feature. For screens that only ever say "fetch this and show it", the ceremony is real work with no matching benefit — and it is harder to explain quickly at a viva. |
| **`riverpod`** (chosen) | Compile-time-safe injection (no `BuildContext` lookup, no runtime "provider not found"); `AsyncValue` is a built-in loading/data/error union; `family` keys a provider by argument; `autoDispose` releases per-screen caches; any provider can be overridden in a test. | One more concept to learn (`Ref` vs `BuildContext`). Two flavours of provider in use (`StateNotifierProvider`, `FutureProvider`). |

## Decision

**Riverpod** (`flutter_riverpod`), used in two shapes that match the two kinds of state above:

- **`StateNotifierProvider` for state we mutate ourselves.** `AuthController` owns the session
  (`unknown` → `authenticated` / `unauthenticated`, plus `isSubmitting` and `errorMessage` for the
  forms). `TicketQueryController` owns the live search and filter selection on My Tickets.
- **`FutureProvider` for state the server owns.** `ticketListProvider`, `ticketDetailProvider`,
  `ticketCommentsProvider`, `ticketHistoryProvider`, `ticketAttachmentsProvider`,
  `categoriesProvider`, `ticketWorkflowProvider`. Each returns an `AsyncValue`, and every screen
  renders it with `.when(loading:, error:, data:)` — which is precisely why no screen in the app
  can be left blank while it waits or silently empty when a request fails.

Two consequences of that split are worth naming, because they are the parts that look like magic:

- `ticketListProvider` *watches* `ticketQueryProvider`. Changing a filter therefore re-issues the
  server-side query automatically; there is no manual "reload" wiring anywhere.
- `ticketWorkflowProvider` re-schedules itself with a 3-second timer **only while** the workflow
  status is `Planned` or `Running`, and cancels that timer in `onDispose`. That is the whole
  polling implementation: it stops on its own when the workflow settles or the screen closes.

`lib/core/providers.dart` is the composition root — the secure store, the API client and the three
repositories are each constructed exactly once, and a test overrides any of them with one line.

## Consequences

**Token storage.** The JWT goes into `flutter_secure_storage`, never `SharedPreferences`. The
Keychain (iOS) and EncryptedSharedPreferences (Android) are backed by the platform keystore;
`SharedPreferences` is a plain file that any process with the app's data directory can read. The
React client uses `localStorage` only because a browser offers nothing better — a native app does,
so it uses it. **The password is never persisted anywhere**, only the short-lived token the server
issued and a cached copy of the profile so the splash screen can render before `/api/auth/me`
returns.

**How a 401 is handled.** `ApiClient`'s response interceptor clears secure storage and calls the
handler `AuthController` registered with it. `AuthController` sets the session to
`unauthenticated`; `_AuthRefreshNotifier` bridges that to go_router's `refreshListenable`; the
router's `redirect` guard re-evaluates and lands the user on Login. No screen contains a line of
code about expired sessions.

**Startup is not trusting.** A stored token is verified against `GET /api/auth/me` before the app
treats the session as live, so a revoked or expired token cannot survive a restart. The one
deliberate exception: if that call fails with a *network* error rather than a 401, the cached
session is kept — losing signal should not sign an employee out.

**Testability.** 92 tests run without a device or a server. `AuthController` is tested against a
fake store and a fake repository injected through provider overrides; the login form is tested by
pumping the real screen with those overrides and asserting that invalid input never reaches the
API at all.

**Authorization is not affected by any of this.** The client hides what an employee cannot do, but
`TicketService` scopes an employee to their own tickets *inside the SQL query* and the API
re-checks the caller's role on every request. Verified: an employee reading another user's ticket
gets 403, and deciding an approval gets 403.

## Related

- [ADR-001](ADR-001-react-state-management.md) — the React client's equivalent decision.
- [ADR-002](ADR-002-agentic-ai-orchestration.md) — why the agents run only inside ASP.NET Core,
  which is what leaves the Flutter app with nothing to do but display the recorded run.
