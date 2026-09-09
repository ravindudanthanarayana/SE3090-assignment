# ADR-001: React state management — Context API

**Status:** Accepted · **Date:** 2026-09 · **Required by:** spec §14.2

## Context

The React application is the staff, manager, admin and AI-approval console. It has around eighteen
routes and talks to roughly forty API endpoints. We had to choose how it manages state.

Looking at what the app actually holds, there are two different kinds of state:

1. **Session state** — the signed-in user, their role, and the JWT. Genuinely global: the layout,
   the navigation, every route guard and every request needs it.
2. **Server data** — tickets, articles, workflows, approvals. This is not really *client* state at
   all; it is a cache of the database, scoped to one screen, and invalidated the moment anyone else
   changes anything.

There is almost nothing in between. No multi-step wizard spanning routes, no offline queue, no
optimistic cross-screen updates.

## Options considered

**Redux Toolkit.** The most common answer, and what a reviewer might expect at this size. But the
work it does well — normalising shared entities, time-travel debugging, complex reducer logic — is
work we do not have. We would be writing slices, actions and selectors for data that one screen
reads once and throws away. Every student would have to learn and defend it at the viva.

**Zustand.** Much lighter, and a reasonable middle ground. But it solves the same problem Context
solves for us, while adding a dependency; the win only appears when you have significant global
state, which we do not.

**TanStack Query.** Genuinely well matched to the server-data half. It would give us caching,
refetching and request deduplication for free. We rejected it because it does not remove the need
for session state (we would still add Context on top), and because a ~13 KB dependency plus its own
mental model is hard to justify when the entire need is "loading, error, data, refetch".

**Context API plus a small custom hook.** Chosen.

## Decision

- `AuthContext` for identity, token and role, persisted to `localStorage` and revalidated against
  `GET /api/auth/me` on load.
- `ToastContext` for transient success and error feedback.
- A `useApiResource(fetcher, deps)` hook that owns `{ data, loading, error, refetch }` and cancels
  stale responses, so a slow request cannot overwrite newer data.
- Everything else is ordinary component state.

## Consequences

**Good.** Zero state-management dependencies. `useApiResource` is about forty lines, so every
student can read it and explain it. Because the hook exists, the four required UI states (loading,
empty, error, success) are handled consistently through one `<AsyncState>` component rather than
being reinvented — or forgotten — screen by screen.

**Bad.** No cross-screen caching: navigating from the ticket list to a ticket and back refetches the
list. At this data volume that is imperceptible, but it would not scale to a high-traffic app.
There is no automatic background refetching, so the AI workflow page has a manual **Refresh** button
rather than polling.

**If this changes.** The moment we need shared caching across routes — for example, live-updating the
pending-approval count in the navigation — the honest move is to adopt TanStack Query for server data
and keep `AuthContext` as it is. `useApiResource` is deliberately shaped like `useQuery` so that
swap is mechanical.
