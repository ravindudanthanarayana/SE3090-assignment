# Architecture Decision Records

Required by spec §14.2. Each ADR is one page: Context · Options considered · Decision · Consequences.

| ID | Decision | Required by spec? |
|---|---|---|
| ADR-001 | React state management: Context API | **Yes** |
| ADR-002 | Agentic AI framework and orchestration: custom in-process C# orchestrator | **Yes** |
| ADR-003 | Database schema strategy for agent workflow state: relational rows + `jsonb` payloads | **Yes** |
| ADR-004 | PostgreSQL hosting: Neon | supporting |
| ADR-005 | Cloud deployment platform | **Yes** |
| ADR-006 | No repository layer over EF Core | supporting |
| ADR-007 | Flutter state management | **Yes** — *stub, written when Flutter is built* |

The written justification in each ADR must be the group's own reasoning (§18.1); the drafts here are a
starting point to argue with, not a text to copy.
