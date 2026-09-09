# ADR-002: Agentic AI framework and orchestration — custom C# orchestrator, in-process

**Status:** Accepted · **Date:** 2026-09 · **Required by:** spec §14.2

## Context

Spec §9 requires a workflow that takes a domain objective, produces a structured plan, delegates to
distinct agent roles, calls allow-listed tools with validated inputs and structured outputs,
persists its state, applies deterministic validation, pauses a high-impact action for human
approval, and produces either an auditable result or a safe recorded failure.

Spec §2 lists LangGraph (the stack used in labs), Microsoft Agent Framework, LlamaIndex agents and
Google ADK — and explicitly also permits **"a custom orchestration approach"**.

There is a hard constraint that shapes this decision: spec §2 states that React and Flutter must
communicate **only** with the ASP.NET Core API, and that a Python AI service, if used, must be an
internal service that clients never call directly.

## Options considered

**LangGraph in a Python sidecar.** The lab stack, so an evaluator may expect it, and its graph
model maps neatly onto our five agents. The costs are real: a second language, a second deployable,
a second dependency tree, an HTTP hop with its own failure and timeout semantics, and a second set
of tests. Worse for us, the tools our agents need are all database reads over *our* EF Core model —
so a Python service would either need its own database access (duplicating our authorization rules
in a second language, which is exactly how those rules drift apart) or would have to call back into
ASP.NET Core for every tool, adding a network round-trip per tool call for no benefit.

**Microsoft Agent Framework.** Stays in C# and in-process, which addresses the language split. We
rejected it because our orchestration is a linear pipeline over four specialists with a
deterministic gate at the end — the framework's abstractions would sit between us and code we need
to be able to explain line by line at a viva, and its own conventions would be one more thing to
learn and defend.

**Custom C# orchestrator, in-process.** Chosen.

## Decision

`WorkflowOrchestrator` in `SmartDesk.Application/Agents` is a plain deterministic loop. It asks the
Planner for a plan, resolves each planned agent by name from DI, runs it with a bounded retry
budget, validates its output, persists an `AgentSteps` row, and then hands the combined result to
`BusinessRuleEngine`.

The language model never controls the loop, never chooses permissions and never writes to the
database. Tools are reached only through `ToolRegistry`, which enforces the calling agent's own
allow-list before the global registry.

## Consequences

**Good.** The §2 constraint is satisfied *structurally* rather than by convention: because the
orchestrator is a set of C# classes inside the API process, there is physically no AI endpoint a
client could call. One language, one deployable, one dependency tree. The tools use the same
`DbContext`, the same entities and the same authorization primitives as the rest of the
application, so the AI cannot drift out of step with the business rules. The whole subsystem is
directly testable with xUnit and a scripted model — which is what makes the twelve golden cases
deterministic and free.

**Bad.** We wrote the orchestration ourselves — roughly 300 lines — where a framework would have
supplied it. We do not get graph features we are not using: conditional branching, parallel fan-out,
checkpoint-and-resume across process restarts. If a future workflow genuinely needs those, this
decision should be revisited rather than extended.

**Risk accepted.** An evaluator familiar with the lab stack may expect LangGraph. The mitigation is
this ADR: the spec permits a custom approach, and the reasoning above is the group's own and is
defensible at the viva.
