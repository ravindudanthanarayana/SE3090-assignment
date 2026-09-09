<div align="center">

<img src="public/logo-mark.png" alt="SmartDesk AI" width="76">

# SmartDesk AI — Web

**The staff console.**

Work the queue, assign, escalate, report — and approve what the AI proposes.

[![React](https://img.shields.io/badge/React-19-61DAFB?style=flat-square&logo=react&logoColor=black)](https://react.dev)
[![TypeScript](https://img.shields.io/badge/TypeScript-5-3178C6?style=flat-square&logo=typescript&logoColor=white)](https://www.typescriptlang.org)
[![Vite](https://img.shields.io/badge/Vite-7-646CFF?style=flat-square&logo=vite&logoColor=white)](https://vite.dev)
[![Tailwind](https://img.shields.io/badge/Tailwind-4-06B6D4?style=flat-square&logo=tailwindcss&logoColor=white)](https://tailwindcss.com)
[![Tests](https://img.shields.io/badge/tests-34%20passing-2ea44f?style=flat-square)](#tests)

</div>

---

The web client for the SmartDesk AI API in [`../backend`](../backend). It covers the **staff** side
of the product: support agents, managers and administrators. Employee self-service lives in the
[Flutter app](../mobile/smartdesk_mobile) — spec §4.5 asks the two clients to serve meaningfully
different purposes, and the Approval Centre is the clearest example of the split.

## Running it

```bash
cp .env.example .env      # VITE_API_URL=http://localhost:5299
npm install
npm run dev               # http://localhost:5173
```

The API must be running first — see the [root README](../README.md#6-running-it-locally).

| Script | Does |
|---|---|
| `npm run dev` | Vite dev server with HMR |
| `npm run build` | Type check, then production build to `dist/` |
| `npm run preview` | Serve the production build locally |
| `npm run test:run` | Vitest, once |

## The two experiences, one app

| | Routes | Chrome |
|---|---|---|
| **Public** | `/`, `/features`, `/solutions`, `/ai-agents`, `/how-it-works`, `/about`, `/contact` | Marketing navbar + full footer |
| **Auth** | `/signin`, `/signup` | Split-screen auth shell |
| **Workspace** | `/app/dashboard`, `/app/tickets`, `/app/knowledge-base`, `/app/assignments`, `/app/sla`, `/app/reports`, `/app/ai-workflows`, `/app/approvals`, `/app/audit-logs`, `/app/admin/*` | Sidebar shell, no public navigation |

Both share one design system, so they read as the same product.

## Structure

```
src/
├── api/          client.ts (axios + JWT interceptor + 401 handling) · endpoints.ts (every call, in one file)
├── components/   ui/ · brand/ · marketing/ — the reusable layer
├── context/      AuthProvider, ThemeProvider
├── hooks/        shared data-fetching and UI hooks
├── pages/        public/ · auth/ · app/
├── types/        the API surface, typed once
├── routes.ts     route constants, so a typo is a compile error
└── index.css     the design system — semantic tokens, light + dark
```

## Design system

`src/index.css` defines one set of semantic tokens (`bg-surface`, `text-fg`, `border-line`,
`bg-accent-solid`) exposed to Tailwind via `@theme inline`. Components never use a raw palette
colour, which is what makes dark mode a **token swap** rather than a sweep of `dark:` overrides.

The theme is stored in `localStorage`, falls back to the OS preference, and is applied by an inline
script in `index.html` **before first paint**, so there is no flash of the wrong theme.

The same tokens are transcribed into the Flutter app's `AppColors`, and both clients use the same
`logo-mark.png`. See [ADR-001](../docs/ADRs/ADR-001-react-state-management.md) for the state
decision.

## Tests

```bash
npm run test:run
```

34 tests across 6 files: protected routes, form validation, search/filter/sort/pagination, API
interaction, and loading / empty / error states.

## Notes

- **State is the Context API**, not Redux — identity plus the JWT is the only genuinely global
  client state ([ADR-001](../docs/ADRs/ADR-001-react-state-management.md)).
- **Authorization is not enforced here.** The UI hides what a role cannot do, but the API re-checks
  every request; a hidden button changes nothing.
- **The bundle is ~816 KB** (237 KB gzipped), dominated by Recharts. Route-level code splitting is
  the fix if it starts to matter.
