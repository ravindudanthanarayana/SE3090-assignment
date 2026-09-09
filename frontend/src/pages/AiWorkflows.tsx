import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { aiApi } from '../api/endpoints';
import { useApiResource } from '../hooks/useApiResource';
import { AsyncState, Card, WorkflowBadge, ApprovalBadge } from '../components/Ui';
import { Pagination } from '../components/Pagination';
import { formatDateTime, formatRelative, prettyJson, safeParse } from '../utils/format';
import type { AgentStep, WorkflowStatus } from '../types';

const STATUSES: WorkflowStatus[] = ['Planned', 'Running', 'AwaitingApproval', 'Completed', 'Failed', 'Rejected'];

/** Monitoring list for every agent workflow the system has run. */
export function AiWorkflows() {
  const [status, setStatus] = useState<string>('');
  const [page, setPage] = useState(1);

  const workflows = useApiResource(
    () => aiApi.workflows({ status: status || undefined, page, pageSize: 15 }),
    [status, page],
  );

  return (
    <div className="space-y-4">
      <header>
        <h1 className="text-xl font-semibold text-fg">AI workflows</h1>
        <p className="mt-1 text-sm text-fg-subtle">
          Every run of the five-agent triage workflow, with its current state.
        </p>
      </header>

      <div className="rounded-2xl border border-line bg-surface p-4 shadow-card">
        <label htmlFor="wf-status" className="mb-1 block text-xs font-medium text-fg-muted">Status</label>
        <select id="wf-status" value={status}
                onChange={(e) => { setStatus(e.target.value); setPage(1); }}
                className="rounded-xl border border-line bg-surface px-3 py-2 text-sm text-fg transition focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/25">
          <option value="">All statuses</option>
          {STATUSES.map((s) => <option key={s} value={s}>{s.replace(/([a-z])([A-Z])/g, '$1 $2')}</option>)}
        </select>
      </div>

      <AsyncState {...workflows} onRetry={workflows.refetch}
                  isEmpty={(p) => p.items.length === 0}
                  emptyTitle="No workflows yet"
                  emptyHint="A workflow starts automatically whenever a ticket is raised.">
        {(result) => (
          <>
            <ul className="space-y-2">
              {result.items.map((w) => (
                <li key={w.id}>
                  <Link to={`/app/ai-workflows/${w.id}`}
                        className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-line bg-surface p-4 hover:border-accent/45">
                    <div className="min-w-0">
                      <p className="text-sm font-medium text-fg">
                        #{w.id} · {w.ticketNumber}
                      </p>
                      <p className="truncate text-xs text-fg-subtle">{w.objective}</p>
                    </div>
                    <div className="flex flex-wrap items-center gap-2 text-xs text-fg-subtle">
                      {w.pendingApprovals > 0 && (
                        <span className="rounded-full bg-warn-soft px-2 py-0.5 font-medium text-warn">
                          {w.pendingApprovals} awaiting approval
                        </span>
                      )}
                      <span>{w.stepCount} steps</span>
                      <span>{formatRelative(w.startedAt)}</span>
                      <WorkflowBadge status={w.status} />
                    </div>
                  </Link>
                </li>
              ))}
            </ul>
            <Pagination page={result.page} pageSize={result.pageSize} totalCount={result.totalCount}
                        totalPages={result.totalPages} onPageChange={setPage} />
          </>
        )}
      </AsyncState>
    </div>
  );
}

/**
 * The auditable execution summary the spec asks for: the plan, each agent's structured input and
 * output, every tool call with its timing, the validation result, and the approval decisions.
 */
export function WorkflowDetail() {
  const { id } = useParams();
  const workflow = useApiResource(() => aiApi.workflow(Number(id)), [id]);

  return (
    <div className="space-y-4">
      <AsyncState {...workflow} onRetry={workflow.refetch}>
        {(w) => {
          const plan = safeParse<{ rationale: string; steps: { order: number; agent: string; purpose: string }[] }>(w.planJson);
          const outcome = safeParse<{
            summary: string; appliedActions: string[]; pendingApprovalActions: string[];
          }>(w.finalOutcomeJson);

          return (
            <>
              <header className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <div className="flex items-center gap-2">
                    <h1 className="text-xl font-semibold text-fg">Workflow #{w.id}</h1>
                    <WorkflowBadge status={w.status} />
                  </div>
                  <p className="mt-1 text-sm text-fg-muted">{w.objective}</p>
                  <p className="mt-1 text-xs text-fg-subtle">
                    <Link to={`/app/tickets/${w.ticketId}`} className="text-accent-text hover:underline">
                      {w.ticketNumber}
                    </Link>
                    {' · '}started {formatDateTime(w.startedAt)}
                    {' · '}{w.totalDurationMs} ms of agent time
                  </p>
                </div>
                <button type="button" onClick={workflow.refetch}
                        className="rounded-full px-4 py-1.5 text-sm font-medium text-fg-muted ring-1 ring-line-strong hover:bg-surface-2">
                  Refresh
                </button>
              </header>

              {w.errorMessage && (
                <div role="alert" className="rounded-lg border border-danger/30 bg-danger-soft p-4 text-sm text-danger">
                  <p className="font-medium">The workflow failed safely</p>
                  <p className="mt-1">{w.errorMessage}</p>
                  <p className="mt-2 text-xs">The ticket was left unchanged and the failure was recorded in the audit trail.</p>
                </div>
              )}

              {plan && (
                <Card title="Plan created by the Planner agent">
                  <p className="text-sm text-fg-muted">{plan.rationale}</p>
                  <ol className="mt-3 space-y-1 text-sm">
                    {plan.steps.map((s) => (
                      <li key={s.order} className="flex gap-2">
                        <span className="font-medium text-fg-subtle">{s.order}.</span>
                        <span><span className="font-medium text-fg">{s.agent}</span> — {s.purpose}</span>
                      </li>
                    ))}
                  </ol>
                </Card>
              )}

              <section>
                <h2 className="mb-3 text-sm font-semibold text-fg">Agent execution timeline</h2>
                <ol className="space-y-3">
                  {w.steps.map((step) => <StepCard key={step.id} step={step} />)}
                </ol>
              </section>

              {w.workflowToolCalls.length > 0 && (
                <Card title="Orchestrator tool calls">
                  <p className="mb-3 text-xs text-fg-subtle">
                    Made by the orchestrator after the agents finished, when the business rules decided a
                    human decision was required.
                  </p>
                  <ul className="space-y-2">
                    {w.workflowToolCalls.map((t) => (
                      <li key={t.id} className="rounded border border-line p-3 text-sm">
                        <div className="flex items-center justify-between">
                          <code className="font-medium text-fg">{t.toolName}</code>
                          <span className={t.success ? 'text-xs text-ok' : 'text-xs text-danger'}>
                            {t.success ? 'succeeded' : 'failed'} · {t.durationMs} ms
                          </span>
                        </div>
                        {t.errorMessage && <p className="mt-1 text-xs text-danger">{t.errorMessage}</p>}
                      </li>
                    ))}
                  </ul>
                </Card>
              )}

              {outcome && (
                <Card title="Outcome">
                  <p className="text-sm text-fg">{outcome.summary}</p>

                  {outcome.appliedActions.length > 0 && (
                    <div className="mt-3">
                      <h3 className="text-xs font-semibold uppercase tracking-wide text-fg-subtle">
                        Applied automatically (low impact)
                      </h3>
                      <ul className="mt-1 list-inside list-disc text-sm text-fg">
                        {outcome.appliedActions.map((a) => <li key={a}>{a}</li>)}
                      </ul>
                    </div>
                  )}

                  {outcome.pendingApprovalActions.length > 0 && (
                    <div className="mt-3">
                      <h3 className="text-xs font-semibold uppercase tracking-wide text-warn">
                        Held for human approval (high impact)
                      </h3>
                      <ul className="mt-1 list-inside list-disc text-sm text-fg">
                        {outcome.pendingApprovalActions.map((a) => <li key={a}>{a}</li>)}
                      </ul>
                    </div>
                  )}
                </Card>
              )}

              {w.approvals.length > 0 && (
                <Card title="Approvals" action={<Link to="/app/approvals" className="text-sm text-accent-text hover:underline">Approval centre</Link>}>
                  <ul className="space-y-3">
                    {w.approvals.map((a) => (
                      <li key={a.id} className="rounded-2xl border border-line p-4">
                        <div className="flex flex-wrap items-center justify-between gap-2">
                          <p className="text-sm font-medium text-fg">{a.actionType}</p>
                          <ApprovalBadge status={a.status} />
                        </div>
                        <p className="mt-1 text-sm text-fg-muted">{a.reason}</p>
                        <p className="mt-2 text-xs text-fg-subtle">
                          Requested {formatDateTime(a.requestedAt)}
                          {a.decidedByName && ` · decided by ${a.decidedByName} on ${formatDateTime(a.decidedAt)}`}
                        </p>
                        {a.decisionNote && <p className="mt-1 text-xs italic text-fg-muted">“{a.decisionNote}”</p>}
                      </li>
                    ))}
                  </ul>
                </Card>
              )}
            </>
          );
        }}
      </AsyncState>
    </div>
  );
}

function StepCard({ step }: { step: AgentStep }) {
  const [expanded, setExpanded] = useState(false);

  const tone =
    step.status === 'Succeeded' ? 'border-ok/30 bg-ok-soft'
    : step.status === 'Failed' ? 'border-danger/30 bg-danger-soft'
    : step.status === 'Skipped' ? 'border-line bg-surface-2'
    : 'border-blue-200 bg-blue-50';

  return (
    <li className="rounded-2xl border border-line bg-surface">
      <div className={`flex flex-wrap items-center justify-between gap-2 rounded-t-lg border-b px-4 py-3 ${tone}`}>
        <div>
          <p className="text-sm font-semibold text-fg">
            {step.stepOrder}. {step.agentName}
          </p>
          {step.purpose && <p className="text-xs text-fg-muted">{step.purpose}</p>}
        </div>
        <div className="flex items-center gap-3 text-xs text-fg-muted">
          <span>{step.durationMs} ms</span>
          {step.retryCount > 0 && (
            <span className="rounded bg-warn-soft px-2 py-0.5 font-medium text-warn">
              {step.retryCount} {step.retryCount === 1 ? 'retry' : 'retries'}
            </span>
          )}
          <span className="font-medium">{step.status}</span>
        </div>
      </div>

      <div className="px-4 py-3">
        {step.errorMessage && <p className="mb-2 text-sm text-danger">{step.errorMessage}</p>}

        {step.toolCalls.length > 0 && (
          <div className="mb-3">
            <p className="text-xs font-semibold uppercase tracking-wide text-fg-subtle">Tools called</p>
            <ul className="mt-1 flex flex-wrap gap-2">
              {step.toolCalls.map((t) => (
                <li key={t.id}
                    className={`rounded px-2 py-0.5 text-xs ring-1 ${
                      t.success ? 'bg-surface-2 text-fg ring-line' : 'bg-danger-soft text-danger ring-danger/30'
                    }`}>
                  <code>{t.toolName}</code> · {t.durationMs} ms
                </li>
              ))}
            </ul>
          </div>
        )}

        <button type="button" onClick={() => setExpanded((e) => !e)}
                aria-expanded={expanded}
                className="text-xs font-medium text-accent-text hover:underline">
          {expanded ? 'Hide' : 'Show'} structured input, output and validation
        </button>

        {expanded && (
          <div className="mt-3 space-y-3">
            <JsonBlock label="Input contract" json={step.inputJson} />
            <JsonBlock label="Validated output" json={step.outputJson} />
            <JsonBlock label="Deterministic validation" json={step.validationJson} />
            {step.toolCalls.map((t) => (
              <JsonBlock key={t.id} label={`Tool result: ${t.toolName}`} json={t.outputJson} />
            ))}
          </div>
        )}
      </div>
    </li>
  );
}

function JsonBlock({ label, json }: { label: string; json: string | null }) {
  if (!json) return null;
  return (
    <div>
      <p className="text-xs font-semibold uppercase tracking-wide text-fg-subtle">{label}</p>
      <pre className="mt-1 max-h-64 overflow-auto rounded bg-surface-2 p-3 text-xs leading-relaxed text-fg">
        {prettyJson(json)}
      </pre>
    </div>
  );
}
