# Agentic AI evaluation report

## Scope

The Agentic AI subsystem is tested with a deterministic scripted model and rule-based assertions. This is
appropriate for the assignment because an LLM judge must not be the only evaluation method.

Primary source: `backend/SmartDesk.Tests/Agents/AgentEvaluationTests.cs`.

## Evaluation cases in the repository

The test file contains 26 fact/theory cases covering:

| Area | Coverage |
|---|---|
| Planning | Valid ordered plan, unknown-agent rejection and duplicate-agent rejection |
| Delegation | Every planned agent runs once and in order; structured step input/output is persisted |
| Tool permissions | Allow-list enforcement, unregistered-tool refusal, workflow-ticket scoping and least privilege |
| Knowledge safety | Unpublished articles are excluded; article IDs not returned by search are discarded |
| Output validation | Malformed, out-of-range and markdown-wrapped model output handling |
| Business rules | Invalid assignment candidates are rejected; high-impact changes are held for approval |
| Human approval | Approval creation does not execute the action; unauthorized or invalid decisions are refused |
| Prompt-injection resistance | Ticket text is treated as untrusted data and cannot escape its delimiters |
| Reliability | Transient model failure retries and persists retry count; permanent validation failure produces safe failure |
| Data protection | Prompt text and model reasoning are not persisted |

## Workflow acceptance mapping

| Assignment requirement | Implementation evidence |
|---|---|
| Domain objective | Ticket creation starts a triage-and-routing workflow |
| Structured plan | `PlannerAgent` produces an ordered plan |
| Distinct agents | Planner, Triage, Solution, Assignment and Validation agents |
| Controlled tools | `ToolRegistry` and per-agent allow-lists |
| Structured outputs | Typed agent contracts and deterministic validation |
| Persisted state | `AgentWorkflows`, `AgentSteps`, `AgentToolCalls` and `AiApprovals` |
| Business-rule validation | `AgentOutputValidator` and `BusinessRuleEngine` |
| Human approval | High-impact assignment/escalation pauses in `AwaitingApproval` |
| Auditable outcome | Timings, retries, validation results, approval decision, history and final outcome |
| Safe failure | Failed validation leaves the ticket unchanged and records the failure |

## Execution status

The repository contains the evaluation tests and the live workflow implementation. A complete local run of
the backend test assembly was not fully successful during the latest verification because 30 tests could not
initialize the local PostgreSQL fixture due to a password-authentication mismatch. The evaluation cases must be
rerun with the CI PostgreSQL service or a correctly configured local database, and the passing CI artifact must
be retained for final submission.

The recorded cross-client scenario in `docs/13-flutter-application.md` §13.4 should also be demonstrated live:
Flutter employee submission → five-agent workflow → React manager approval → backend transaction → Flutter
status/history update.

## Final evidence required

- [ ] Passing CI test artifact for the full Agentic AI evaluation suite.
- [ ] Screenshots or logs showing a pending approval and the authorized decision.
- [ ] One safe-failure or rejected-action example.
- [ ] One prompt-injection test result.
- [ ] Demonstration evidence showing the persisted workflow timeline and audit history.
