# ADR-003: Database schema strategy for agent workflow state — relational rows with jsonb payloads

**Status:** Accepted · **Date:** 2026-09 · **Required by:** spec §14.2

## Context

Spec §9.6 requires that we persist the workflow id, objective, plan, completed steps, tool results,
validation results, errors, approval status and final outcome — in "structured, durable storage".
Spec §9.9 additionally requires timings, retries and approval decisions to be observable. Spec §6.5
forbids storing hidden reasoning or unnecessary sensitive data.

The difficulty is that the *shape* of what each agent produces differs: a `TriageResult` has a
category and an urgency score, a `SolutionResult` has article ids and a confidence, an
`AssignmentResult` has a user id and alternatives. Those shapes will change as we tune the agents.

## Options considered

**One table per agent output.** `TriageResults`, `SolutionResults`, `AssignmentResults`,
`ValidationResults`, plus tables for plans and tool payloads — six or more tables. Fully relational
and individually queryable, but every prompt change becomes a migration, and the orchestrator would
need a type switch to know where to write. It also makes "show me this workflow's timeline" a
six-way join for a page that only ever renders the data as text.

**A single document per workflow.** One table, one `jsonb` blob holding everything. Simple to write,
but it destroys the things we genuinely query relationally: pending approvals, per-agent timings,
which tool was called by which step. The Approval Centre's "give me all pending approvals" would
become a full scan and a JSON filter.

**Relational rows for the workflow structure, `jsonb` for the varying payloads.** Chosen.

## Decision

Four tables carry the structure, and `jsonb` columns carry the parts that vary:

| Table | Relational columns (queried) | `jsonb` columns (rendered) |
|---|---|---|
| `AgentWorkflows` | ticket, status, current step, timestamps | `PlanJson`, `FinalOutcomeJson` |
| `AgentSteps` | workflow, order, agent name, status, `DurationMs`, `RetryCount` | `InputJson`, `OutputJson`, `ValidationJson` |
| `AgentToolCalls` | workflow, step, tool name, success, `DurationMs` | `InputJson`, `OutputJson` |
| `AiApprovals` | workflow, ticket, action type, status, decider, decided-at | `ProposedActionJson` |

The rule we applied: **if we filter, sort or count by it, it is a column; if we only display it, it
is `jsonb`.**

`jsonb` rather than `text` because PostgreSQL validates it on write, stores it in a binary form, and
lets us query inside it later with `->>` if a report ever needs to — without another migration.

## Consequences

**Good.** The queries the application actually runs are indexed relational ones:
`AiApprovals(Status)` for the approval feed, `AgentSteps(WorkflowId, StepOrder)` for the timeline.
Changing an agent's output contract is a prompt-and-record change with **no migration**. The React
workflow page renders each `jsonb` payload directly, which is what makes the structured outputs
visible and auditable in the UI.

**Bad.** The payloads are not schema-enforced by the database — validity is guaranteed by
`AgentOutputValidator` in C# instead. Aggregating across payloads (for example, "average triage
confidence this month") needs JSON operators rather than a plain `AVG`. We accepted both, because
the C# validator rejects unknown members anyway and no such aggregate is in scope.

**Explicitly excluded.** Prompt text, model reasoning, chain-of-thought, API keys and tokens are
never written to any of these columns. A unit test asserts that the persisted payloads contain none
of the phrases that only appear in our system prompts.
