# ADR-007: Flutter state management

**Status:** ⚠ **Not yet decided — placeholder** · **Required by:** spec §14.2

## Why this file exists

Spec §14.2 names the Flutter state-management approach as one of the decisions the ADR **must**
record. The Flutter application is being built separately and does not exist in this repository yet,
so this decision has not been made and must not be written up as though it had.

**This file must be completed before submission.** Leaving it as a placeholder costs marks under
Documentation and Deployment.

## What the decision has to cover

When the Flutter app is built, record here:

- **Context.** Flutter's role in this system is employee self-service: register, sign in with secure
  token storage, raise a ticket, track its status, read the AI's suggested articles, comment. That is
  a deliberately different purpose from React's staff console, which spec §4.5 requires. The state it
  manages is a session plus per-screen server data — the same shape as the React app.
- **Options considered.** At minimum `provider`, `riverpod`, `bloc`, and plain `setState` with a
  small service layer. Say honestly what each would cost and give.
- **Decision and reasoning.** The group's own reasoning, not a copied summary.
- **Consequences.** Including how the JWT is stored (`flutter_secure_storage` rather than
  `SharedPreferences`, and why), and how a 401 is handled.

## What is already settled and can be stated as fact

- Flutter calls the **same** ASP.NET Core API. No new endpoints are needed — the employee-facing
  subset already exists and is listed in `docs/05-api-design.md`.
- Authentication is a JWT in the `Authorization` header. No cookies, no session affinity, so it
  works from Dart's `http` or `dio` unchanged.
- Every list endpoint returns the same `{ items, page, pageSize, totalCount, totalPages }` envelope,
  so one Dart model covers all of them.
- Errors are uniform RFC 7807 `ProblemDetails`, so one Dart error mapper covers the whole API.
- §8 requires one meaningful device feature. The natural fit for this domain is a camera or image
  picker for attaching a screenshot to a ticket. **That needs a backend attachment endpoint that does
  not exist yet** — plan for it when scoping the Flutter work.
