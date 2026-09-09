# React Application Design + Third-Party Integration

## Part 1 — React (spec §7)

Vite + React 18 + TypeScript + Tailwind + React Router 6 + Axios.
State: **Context API** (`AuthContext` for identity/token/role, `ToastContext` for notifications) plus a small
`useApiResource` hook that owns `{data, loading, error, refetch}`. Justified in ADR-001. No Redux — there is
no cross-cutting client state beyond auth.

### Pages

| Route | Roles | Purpose |
|---|---|---|
| `/login`, `/register` | anonymous | JWT auth, client-side validation |
| `/` (dashboard) | all | role-aware KPI cards + charts |
| `/tickets` | all | **DataTable**: search, status/priority/category/agent filters, sortable columns, server-side pagination |
| `/tickets/new` | Employee, Admin | create form + validation |
| `/tickets/:id` | all (scoped) | detail + tabs: Comments · History · **AI Workflow** |
| `/agents` | Manager, Admin | support agents, skills, live workload |
| `/assignments` | Manager, Admin | unassigned queue + recommendation → assign |
| `/knowledge` `/knowledge/:id` `/knowledge/new` `/knowledge/:id/edit` | all read; Manager/Admin write | KB CRUD + search |
| `/sla` | Manager, Admin, SupportAgent | at-risk / breached buckets |
| `/reports` | Manager, Admin | analytics + agent performance |
| `/ai/workflows` | staff | workflow list + status filter |
| `/ai/workflows/:id` | staff | **execution timeline**: plan, each agent step with duration/retries/validation, every tool call, final outcome |
| `/approvals` | **Manager, Admin** | Approval Center — Approve / Reject / **Request revision** |
| `/admin/users`, `/admin/categories` | Admin | CRUD |
| `/audit` | Manager, Admin | filterable audit trail |

18 routes. Each maps to a marked requirement; none is decorative.

### Reusable components
`DataTable` (sort + empty + loading), `Pagination`, `SearchFilterBar`, `StatusBadge`, `PriorityBadge`,
`SlaBadge`, `Modal`, `ConfirmDialog`, `FormField`, `AsyncState` (loading/empty/error wrapper), `Toast`,
`RoleGuard`, `ProtectedRoute`, `AgentStepCard`, `StatCard`. That is the whole set — no premature abstraction.

### Required UI states (§7.6)
`AsyncState` renders exactly one of: spinner · error panel with retry · empty-state message · children.
Success feedback goes through `ToastContext`. Responsive via Tailwind breakpoints; the table collapses to
cards under `md`. Accessibility: semantic elements, labelled inputs, `aria-live` on toasts, visible focus rings.

---

## Part 2 — Third-party integration (spec §11)

**Chosen service: transactional email notifications** (provider decided with you — Resend / Brevo / SMTP).

### Business purpose and user benefit
Help-desk value depends on people finding out that something happened without watching a screen. We notify on
three events: **ticket assigned** (the support agent learns they own work), **ticket escalated after manager
approval** (the requester and the manager learn the AI-recommended escalation was accepted), and **ticket
resolved** (the requester can verify and close). This is the natural, non-decorative integration for the domain.

### Design
```
Service layer ──► INotificationService ──► EmailNotificationProvider ──► provider REST API (HTTPS)
                        │                                │
                        └──► Notifications table  ◄───────┘   (status: Pending → Sent | Failed)
```
- **Routed through the backend** (§11) — no API key ever reaches React or Flutter.
- **Credentials**: `NOTIFICATION_API_KEY`, `NOTIFICATION_FROM_ADDRESS` from environment. Never committed.
- **Failure handling**: 10 s timeout, 2 retries with backoff on 5xx/timeout, no retry on 4xx, rate-limit (429)
  respected via `Retry-After`. A send failure is recorded on the `Notifications` row and **never fails the
  business operation** — assigning a ticket succeeds even if email is down.
- **Data minimisation** (§11): we send recipient address, ticket number, title, status and a link. No
  description body, no personal data beyond the work email, no attachments.
- **`NullNotificationProvider`** is used in tests and available for offline demos; it writes the same
  `Notifications` rows so the flow is fully demonstrable without the external service.
