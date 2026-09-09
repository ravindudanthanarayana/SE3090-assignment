# REST API Design

Base: `/api`. Auth: `Authorization: Bearer <JWT>`. All responses JSON. Errors are RFC 7807 `ProblemDetails`.
Every client (React now, Flutter later) uses exactly these endpoints — there are no web-only routes.

## Roles
`Employee` · `SupportAgent` · `SupportManager` · `Admin`

## Auth
| Method | Route | Roles | Notes |
|---|---|---|---|
| POST | `/api/auth/register` | anonymous | Self-registration creates an **Employee** only. Staff accounts are created by Admin. |
| POST | `/api/auth/login` | anonymous | → `{ token, expiresAt, user }` · 401 on bad credentials |
| GET | `/api/auth/me` | any | current principal |

## Component A — Ticket Management (S1)  *(5 CRUD + 4 business)*
| Method | Route | Roles | Notes |
|---|---|---|---|
| GET | `/api/tickets` | any | **search / filter / sort / paginate** (see below). Employee is silently scoped to own tickets. |
| GET | `/api/tickets/{id}` | any (ownership enforced) | 404/403 |
| POST | `/api/tickets` | any authenticated | 201 + `Location`. Triggers the AI workflow (fire-and-forget, ticket creation never fails on AI error). |
| PUT | `/api/tickets/{id}` | owner (while `New`), SupportAgent (assigned), Manager, Admin | |
| DELETE | `/api/tickets/{id}` | Admin | soft-guarded: refuses if not `New`/`Cancelled` |
| **POST** | `/api/tickets/{id}/status` | SupportAgent (assigned), Manager, Admin | **business op** — validated transition machine, writes history, sends notification |
| POST | `/api/tickets/{id}/comments` | any (ownership) | `IsInternal` only settable by staff |
| GET | `/api/tickets/{id}/comments` | any (ownership) | internal comments filtered out for Employees |
| GET | `/api/tickets/{id}/history` | any (ownership) | §5.4 history |
| POST | `/api/tickets/{id}/attachments` | any (ownership) | multipart image, maximum 5 MB; secure screenshot/photo upload for Flutter |
| GET | `/api/tickets/{id}/attachments` | any (ownership) | attachment metadata |
| GET | `/api/tickets/{id}/attachments/{attachmentId}/content` | any (ownership) | authenticated image download |

**Query contract (used by tickets and knowledge-articles):**
`?search=&status=&priority=&categoryId=&assignedToUserId=&slaState=&sortBy=createdAt|priority|slaDueAt|status&sortDir=asc|desc&page=1&pageSize=20`
→ `{ items: [...], page, pageSize, totalCount, totalPages }`. All applied **server-side** in SQL.
`search` matches ticket number, title and description (case-insensitive).

**Status machine (deterministic, outside the LLM):**
`New → Assigned → InProgress → Resolved → Closed`, plus `→ OnHold` (from Assigned/InProgress),
`→ Escalated` (approval-gated), `→ Cancelled` (from New only). Anything else ⇒ 409.

## Component B — Assignment & Workload (S2)  *(4 CRUD + 3 business)*
| Method | Route | Roles | Notes |
|---|---|---|---|
| GET | `/api/support-agents` | Manager, Admin | agents + live open-ticket counts |
| GET | `/api/support-agents/{id}/skills` | Manager, Admin, self | |
| POST | `/api/support-agents/{id}/skills` | Admin | |
| PUT | `/api/support-agents/{id}/skills/{skillId}` | Admin | |
| DELETE | `/api/support-agents/{id}/skills/{skillId}` | Admin | |
| **POST** | `/api/tickets/{id}/assign` | Manager, Admin | **business op** — validates target is an active SupportAgent, records `TicketAssignments`, moves status `New→Assigned`, notifies |
| GET | `/api/assignments/workload` | Manager, Admin | per-agent open / in-progress / breaching counts |
| GET | `/api/assignments/recommendation/{ticketId}` | Manager, Admin | **business op** — deterministic skill+load ranking (the same scoring function the Assignment Agent's tool uses) |

## Component C — Knowledge Base (S3)  *(5 CRUD + 2 business)*
| Method | Route | Roles | Notes |
|---|---|---|---|
| GET | `/api/knowledge-articles` | any | search / filter by category / sort / paginate; Employees see published only |
| GET | `/api/knowledge-articles/{id}` | any | increments `ViewCount` |
| POST | `/api/knowledge-articles` | Admin, Manager | |
| PUT | `/api/knowledge-articles/{id}` | Admin, Manager | |
| DELETE | `/api/knowledge-articles/{id}` | Admin | |
| **GET** | `/api/knowledge-articles/relevant?ticketId=` | staff | **business op** — scored relevance for a ticket (category + keyword overlap) |
| **POST** | `/api/tickets/{id}/link-article` | staff | **business op** — links an article as a suggested solution |

## Component D — SLA, Escalation & Reporting (S4)  *(4+ endpoints, 2 business)*
| Method | Route | Roles | Notes |
|---|---|---|---|
| GET | `/api/reports/dashboard` | any (scoped by role) | totals, open, resolved, high-priority, escalated, SLA-at-risk, active workflows, pending approvals |
| GET | `/api/reports/sla` | Manager, Admin | breach/at-risk/on-track buckets + per-category breakdown |
| GET | `/api/reports/agent-performance` | Manager, Admin | resolved count, avg resolution hours, breach rate per agent |
| GET | `/api/tickets/sla-at-risk` | Manager, Admin, SupportAgent | tickets within the risk threshold |
| **POST** | `/api/tickets/{id}/escalate` | Manager, Admin | **business op** — manual escalation with reason (the AI path goes through approval instead) |
| **POST** | `/api/ai/approvals/{id}/decision` | **Manager, Admin only** | **business op** — approve / reject / request revision; executes the action transactionally |

## Agentic AI (group + S4 approval)
| Method | Route | Roles | Notes |
|---|---|---|---|
| POST | `/api/ai/workflows` | staff, or the ticket owner | start/restart a workflow for a ticket |
| GET | `/api/ai/workflows` | staff | paginated list + status filter |
| GET | `/api/ai/workflows/{id}` | staff (Employee: own ticket, redacted) | **execution summary**: plan, steps with timings/retries/validation, tool calls, approval, outcome |
| GET | `/api/ai/approvals?status=Pending` | Manager, Admin | Approval Center feed |
| GET | `/api/ai/tools` | Admin | the tool allow-list + schemas (demo/observability) |

## Admin
| Method | Route | Roles |
|---|---|---|
| GET/POST/PUT/DELETE | `/api/users` (+`/{id}`, `/{id}/role`, `/{id}/status`) | Admin |
| GET/POST/PUT/DELETE | `/api/categories` (+`/{id}`) | Admin |
| GET | `/api/audit-logs` | Admin, Manager (filter by entity, actor, date; paginated) |

## Utility
| Method | Route | Roles | Notes |
|---|---|---|---|
| GET | `/health` | anonymous | DB connectivity check — required deliverable §14.1 |
| GET | `/swagger` | anonymous | OpenAPI UI with JWT authorize button — required deliverable §14.1 |

## Status codes used
`200` ok · `201` created (+`Location`) · `204` deleted · `400` validation · `401` unauthenticated ·
`403` authorized role but not permitted on this resource · `404` not found · `409` invalid state transition /
duplicate · `422` AI output failed deterministic validation · `500` unhandled · `503` downstream (LLM/email) unavailable.

## Flutter-readiness
Nothing above is web-specific: no cookies, no server-rendered views, no CORS-only behaviour. JWT in the
`Authorization` header works identically from Dart's `http`/`dio`. The employee-facing subset Flutter needs
(`auth/*`, `tickets` CRUD + status + comments + history, `knowledge-articles`, `ai/workflows/{id}`) already exists.
