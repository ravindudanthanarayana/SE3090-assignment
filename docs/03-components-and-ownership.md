# Four Business Components and Individual Ownership

Spec §3 and §4.1: four students ⇒ four primary components; each student must own one component **and**
have a distinct Agentic AI contribution. This table is the backbone of the whole project.

| Student | Component | Core entities | Business op beyond CRUD | **Owned agent** | React screens owned |
|---|---|---|---|---|---|
| S1 | **A. Ticket Management** | `Tickets`, `TicketComments`, `TicketHistory`, `TicketCategories` | `POST /tickets/{id}/status` — validated status transition machine + history + notification | **Triage Agent** (classifies category / priority / urgency from free text) | Tickets list, Ticket detail, Create ticket |
| S2 | **B. Assignment & Workload** | `TicketAssignments`, `SupportAgentSkills` | `POST /tickets/{id}/assign` — skill+workload-aware assignment with reassignment history | **Assignment Agent** (ranks support agents by skill match and current load) | Agents & Workload, Assignment board |
| S3 | **C. Knowledge Base** | `KnowledgeArticles` | `POST /tickets/{id}/link-article` + relevance search that ranks articles for a ticket | **Solution Agent** (retrieves KB articles via tool, produces troubleshooting steps) | KB list, KB article, KB editor |
| S4 | **D. SLA, Escalation & Reporting** | SLA fields on `Tickets`, `AiApprovals`, reporting projections | `POST /tickets/{id}/escalate` — SLA-risk-driven escalation, approval-gated | **Validation & Escalation Agent** (deterministic validation, SLA risk, escalation decision, raises the approval request) | SLA dashboard, Reports, Approval Center |
| Group | **Orchestration** | `AgentWorkflows`, `AgentSteps`, `AgentToolCalls`, `AuditLogs` | workflow lifecycle | **Planner / Coordinator Agent** | AI Workflows list, Workflow detail timeline |

### Why five agent roles instead of four
The spec says **"at least four distinct agents"** (§9.3) and lists four *kinds* of responsibility:
planning/coordination, domain analysis, action/tool use, validation/safety.

The Planner is naturally a **group-level** concern — it is assessed under the *group* criterion
"Integrated Architecture, **Agent Orchestration** and State Management (10 marks)". If we merged the
Planner into one student's agent, that student would own two responsibilities and one of the other three
would have a thinner contribution. Splitting it gives:

- 4 individually-owned specialist agents → 4 clean "Individual Agentic AI Contribution (12 marks)" stories, one per student, each with its own prompt, I/O contract, tool allow-list and tests;
- 1 group-owned Planner → direct evidence for the group orchestration criterion.

This exceeds the minimum (5 ≥ 4) at essentially no extra complexity: the Planner is one more prompt, one
more output record, one more validator.

### Every component satisfies §5.7
"≥4 meaningful API endpoints and ≥1 business operation beyond basic CRUD" — verified in `05-api-design.md`.

### Cross-stack contribution (§3.2)
Each student additionally delivers, for their own component: the EF entity + configuration + migration slice,
the React screens listed above, backend unit tests, agent evaluation tests for their agent, and the matching
`docs/` section. Later, each also delivers their component's Flutter screens.
