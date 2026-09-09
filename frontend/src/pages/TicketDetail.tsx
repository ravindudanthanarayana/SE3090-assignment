import { useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { aiApi, assignmentApi, ticketsApi } from '../api/endpoints';
import { useApiResource } from '../hooks/useApiResource';
import { errorMessage } from '../api/client';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { AsyncState, Button, Card, PriorityBadge, SlaBadge, StatusBadge, WorkflowBadge } from '../components/Ui';
import { Modal } from '../components/Modal';
import { Select, TextArea } from '../components/Form';
import { formatDateTime, formatHours, formatRelative } from '../utils/format';
import type { TicketStatus } from '../types';

type Tab = 'details' | 'comments' | 'history' | 'ai';

export function TicketDetail() {
  const { id } = useParams();
  const ticketId = Number(id);
  const navigate = useNavigate();
  const { user, hasRole } = useAuth();

  const [tab, setTab] = useState<Tab>('details');
  const ticket = useApiResource(() => ticketsApi.get(ticketId), [ticketId]);

  const isStaff = hasRole('SupportAgent', 'SupportManager', 'Admin');
  const isManager = hasRole('SupportManager', 'Admin');

  return (
    <div className="space-y-4">
      <AsyncState {...ticket} onRetry={ticket.refetch}>
        {(t) => (
          <>
            <header className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <div className="flex flex-wrap items-center gap-2">
                  <h1 className="text-xl font-semibold text-fg">{t.ticketNumber}</h1>
                  <StatusBadge status={t.status} />
                  <PriorityBadge priority={t.priority} />
                  <SlaBadge state={t.slaState} />
                  {t.isEscalated && (
                    <span className="rounded-full bg-accent-soft px-2 py-0.5 text-xs font-medium text-accent-text ring-1 ring-accent/30">
                      Escalated
                    </span>
                  )}
                </div>
                <p className="mt-1 text-fg">{t.title}</p>
              </div>
              <Button variant="secondary" onClick={() => navigate('/app/tickets')}>Back to tickets</Button>
            </header>

            <nav className="-mx-4 flex gap-1 overflow-x-auto border-b border-line px-4 sm:mx-0 sm:px-0" aria-label="Ticket sections">
              {(['details', 'comments', 'history', 'ai'] as Tab[]).map((key) => (
                <button
                  key={key}
                  type="button"
                  onClick={() => setTab(key)}
                  aria-current={tab === key ? 'page' : undefined}
                  className={`-mb-px shrink-0 whitespace-nowrap border-b-2 px-4 py-2 text-sm font-medium transition ${
                    tab === key
                      ? 'border-accent text-accent-text'
                      : 'border-transparent text-fg-subtle hover:text-fg'
                  }`}
                >
                  {key === 'ai' ? 'AI workflow' : key.charAt(0).toUpperCase() + key.slice(1)}
                </button>
              ))}
            </nav>

            {tab === 'details' && (
              <DetailsTab
                ticket={t}
                isStaff={isStaff}
                isManager={isManager}
                canWorkOn={isManager || (hasRole('SupportAgent') && t.assignedToUserId === user?.id)}
                onChanged={ticket.refetch}
              />
            )}
            {tab === 'comments' && <CommentsTab ticketId={ticketId} isStaff={isStaff} />}
            {tab === 'history' && <HistoryTab ticketId={ticketId} />}
            {tab === 'ai' && <AiTab ticketId={ticketId} canStart={isStaff || t.createdByUserId === user?.id} />}
          </>
        )}
      </AsyncState>
    </div>
  );
}

// ---- Details ------------------------------------------------------------------------------

function DetailsTab({ ticket, isStaff, isManager, canWorkOn, onChanged }: {
  ticket: import('../types').TicketDetail;
  isStaff: boolean;
  isManager: boolean;
  canWorkOn: boolean;
  onChanged: () => void;
}) {
  const toast = useToast();
  const [statusOpen, setStatusOpen] = useState(false);
  const [assignOpen, setAssignOpen] = useState(false);
  const [escalateOpen, setEscalateOpen] = useState(false);

  return (
    <div className="grid gap-4 lg:grid-cols-3">
      <div className="space-y-4 lg:col-span-2">
        <Card title="Description">
          <p className="whitespace-pre-wrap text-sm text-fg">{ticket.description}</p>
        </Card>

        {ticket.resolution && (
          <Card title="Resolution">
            <p className="whitespace-pre-wrap text-sm text-fg">{ticket.resolution}</p>
            <p className="mt-2 text-xs text-fg-subtle">Resolved {formatDateTime(ticket.resolvedAt)}</p>
          </Card>
        )}

        {ticket.escalationReason && (
          <Card title="Escalation reason">
            <p className="text-sm text-fg">{ticket.escalationReason}</p>
            <p className="mt-2 text-xs text-fg-subtle">Escalated {formatDateTime(ticket.escalatedAt)}</p>
          </Card>
        )}

        {ticket.suggestedArticles.length > 0 && (
          <Card title="Suggested knowledge articles">
            <ul className="space-y-2">
              {ticket.suggestedArticles.map((a) => (
                <li key={a.articleId} className="flex items-center justify-between gap-3 text-sm">
                  <Link to={`/app/knowledge-base/${a.articleId}`} className="text-accent-text hover:underline">
                    {a.title}
                  </Link>
                  <span className="shrink-0 rounded bg-surface-2 px-2 py-0.5 text-xs text-fg-muted">
                    {a.source === 'Agent' ? 'Suggested by AI' : 'Added manually'}
                  </span>
                </li>
              ))}
            </ul>
          </Card>
        )}
      </div>

      <div className="space-y-4">
        <Card title="Details">
          <dl className="space-y-3 text-sm">
            <Row label="Category" value={ticket.categoryName} />
            <Row label="Raised by" value={ticket.createdByName} />
            <Row label="Assigned to" value={ticket.assignedToName ?? 'Unassigned'} />
            <Row label="Raised" value={formatDateTime(ticket.createdAt)} />
            <Row label="SLA due" value={`${formatDateTime(ticket.slaDueAt)} (${formatHours(ticket.hoursUntilSlaDue)})`} />
            <Row label="Last updated" value={formatRelative(ticket.updatedAt)} />
          </dl>
        </Card>

        {isStaff && (
          <Card title="Actions">
            <div className="flex flex-col gap-2">
              {canWorkOn && ticket.allowedNextStatuses.length > 0 && (
                <Button variant="secondary" onClick={() => setStatusOpen(true)}>Change status</Button>
              )}
              {isManager && (
                <Button variant="secondary" onClick={() => setAssignOpen(true)}>
                  {ticket.assignedToUserId ? 'Reassign' : 'Assign'}
                </Button>
              )}
              {isManager && !ticket.isEscalated && (
                <Button variant="secondary" onClick={() => setEscalateOpen(true)}>Escalate</Button>
              )}
              {!canWorkOn && !isManager && (
                <p className="text-xs text-fg-subtle">
                  You can only change tickets that are assigned to you.
                </p>
              )}
            </div>
          </Card>
        )}
      </div>

      <ChangeStatusModal
        open={statusOpen} ticket={ticket}
        onClose={() => setStatusOpen(false)}
        onDone={() => { setStatusOpen(false); onChanged(); toast.success('Status updated.'); }}
      />
      <AssignModal
        open={assignOpen} ticketId={ticket.id}
        onClose={() => setAssignOpen(false)}
        onDone={() => { setAssignOpen(false); onChanged(); toast.success('Ticket assigned.'); }}
      />
      <EscalateModal
        open={escalateOpen} ticketId={ticket.id}
        onClose={() => setEscalateOpen(false)}
        onDone={() => { setEscalateOpen(false); onChanged(); toast.success('Ticket escalated.'); }}
      />
    </div>
  );
}

const Row = ({ label, value }: { label: string; value: string }) => (
  <div className="flex justify-between gap-3">
    <dt className="text-fg-subtle">{label}</dt>
    <dd className="text-right font-medium text-fg">{value}</dd>
  </div>
);

// ---- Action modals ---------------------------------------------------------------------------

function ChangeStatusModal({ open, ticket, onClose, onDone }: {
  open: boolean; ticket: import('../types').TicketDetail; onClose: () => void; onDone: () => void;
}) {
  const [status, setStatus] = useState<TicketStatus | ''>('');
  const [note, setNote] = useState('');
  const [resolution, setResolution] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (!status) { setError('Choose a status.'); return; }
    if (status === 'Resolved' && resolution.trim().length === 0) {
      setError('A resolution is required when resolving a ticket.');
      return;
    }

    setBusy(true); setError(null);
    try {
      await ticketsApi.changeStatus(ticket.id, {
        status, note: note.trim() || undefined, resolution: resolution.trim() || undefined,
      });
      onDone();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <Modal open={open} title="Change ticket status" onClose={onClose}
           footer={<>
             <Button variant="secondary" onClick={onClose} disabled={busy}>Cancel</Button>
             <Button onClick={submit} disabled={busy}>{busy ? 'Saving…' : 'Update status'}</Button>
           </>}>
      <form onSubmit={submit} className="space-y-4">
        {error && <p role="alert" className="rounded bg-danger-soft px-3 py-2 text-sm text-danger">{error}</p>}

        {/* Only transitions the backend will actually accept are offered. */}
        <Select id="new-status" label="New status" value={status}
                onChange={(e) => setStatus(e.target.value as TicketStatus)} required>
          <option value="">Choose…</option>
          {ticket.allowedNextStatuses.map((s) => (
            <option key={s} value={s}>{s.replace(/([a-z])([A-Z])/g, '$1 $2')}</option>
          ))}
        </Select>

        {status === 'Resolved' && (
          <TextArea id="resolution" label="Resolution" rows={4} required
                    value={resolution} onChange={(e) => setResolution(e.target.value)}
                    hint="What fixed it? The requester will see this." />
        )}

        <TextArea id="note" label="Note" rows={3} value={note} onChange={(e) => setNote(e.target.value)}
                  hint="Optional. Recorded in the ticket history." />
      </form>
    </Modal>
  );
}

function AssignModal({ open, ticketId, onClose, onDone }: {
  open: boolean; ticketId: number; onClose: () => void; onDone: () => void;
}) {
  const recommendation = useApiResource(
    () => (open ? assignmentApi.recommendation(ticketId) : Promise.resolve(null)),
    [open, ticketId],
  );
  const [userId, setUserId] = useState('');
  const [reason, setReason] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const submit = async () => {
    if (!userId) { setError('Choose a support agent.'); return; }
    setBusy(true); setError(null);
    try {
      await ticketsApi.assign(ticketId, { assignedToUserId: Number(userId), reason: reason.trim() || undefined });
      onDone();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <Modal open={open} title="Assign this ticket" onClose={onClose}
           footer={<>
             <Button variant="secondary" onClick={onClose} disabled={busy}>Cancel</Button>
             <Button onClick={submit} disabled={busy}>{busy ? 'Assigning…' : 'Assign'}</Button>
           </>}>
      <div className="space-y-4">
        {error && <p role="alert" className="rounded bg-danger-soft px-3 py-2 text-sm text-danger">{error}</p>}

        <p className="text-sm text-fg-muted">
          Candidates are ranked by the same skill-and-workload score the Assignment agent uses.
        </p>

        <AsyncState {...recommendation} onRetry={recommendation.refetch}>
          {(data) =>
            data === null ? <></> : (
              <div className="space-y-2">
                {data.candidates.map((c) => (
                  <label key={c.userId}
                         className={`flex cursor-pointer items-start gap-3 rounded-lg border p-3 text-sm ${
                           userId === String(c.userId) ? 'border-accent bg-accent-soft' : 'border-line'
                         }`}>
                    <input type="radio" name="assignee" value={c.userId} checked={userId === String(c.userId)}
                           onChange={(e) => setUserId(e.target.value)} className="mt-1" />
                    <span className="min-w-0 flex-1">
                      <span className="flex items-center justify-between gap-2">
                        <span className="font-medium text-fg">{c.fullName}</span>
                        <span className="rounded bg-surface-2 px-2 py-0.5 text-xs text-fg-muted">score {c.score}</span>
                      </span>
                      <span className="mt-0.5 block text-xs text-fg-subtle">{c.explanation}</span>
                    </span>
                  </label>
                ))}
              </div>
            )
          }
        </AsyncState>

        <TextArea id="assign-reason" label="Reason" rows={2} value={reason}
                  onChange={(e) => setReason(e.target.value)} hint="Optional. Stored in the assignment history." />
      </div>
    </Modal>
  );
}

function EscalateModal({ open, ticketId, onClose, onDone }: {
  open: boolean; ticketId: number; onClose: () => void; onDone: () => void;
}) {
  const [reason, setReason] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const submit = async () => {
    if (reason.trim().length < 10) { setError('Give a reason of at least 10 characters.'); return; }
    setBusy(true); setError(null);
    try {
      await ticketsApi.escalate(ticketId, reason.trim());
      onDone();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <Modal open={open} title="Escalate this ticket" onClose={onClose}
           footer={<>
             <Button variant="secondary" onClick={onClose} disabled={busy}>Cancel</Button>
             <Button variant="danger" onClick={submit} disabled={busy}>{busy ? 'Escalating…' : 'Escalate'}</Button>
           </>}>
      <div className="space-y-3">
        {error && <p role="alert" className="rounded bg-danger-soft px-3 py-2 text-sm text-danger">{error}</p>}
        <p className="text-sm text-fg-muted">
          This escalates the ticket immediately and notifies the requester.
        </p>
        <TextArea id="escalate-reason" label="Why does this need escalating?" rows={3} required
                  value={reason} onChange={(e) => setReason(e.target.value)} />
      </div>
    </Modal>
  );
}

// ---- Comments ---------------------------------------------------------------------------------

function CommentsTab({ ticketId, isStaff }: { ticketId: number; isStaff: boolean }) {
  const comments = useApiResource(() => ticketsApi.comments(ticketId), [ticketId]);
  const toast = useToast();
  const [body, setBody] = useState('');
  const [internal, setInternal] = useState(false);
  const [busy, setBusy] = useState(false);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (!body.trim()) return;

    setBusy(true);
    try {
      await ticketsApi.addComment(ticketId, { body: body.trim(), isInternal: internal });
      setBody(''); setInternal(false);
      comments.refetch();
      toast.success('Comment added.');
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="space-y-4">
      <Card title="Add a comment">
        <form onSubmit={submit} className="space-y-3">
          <TextArea id="comment" label="Comment" rows={3} value={body}
                    onChange={(e) => setBody(e.target.value)} maxLength={4000} />
          <div className="flex items-center justify-between">
            {isStaff ? (
              <label className="flex items-center gap-2 text-sm text-fg-muted">
                <input type="checkbox" checked={internal} onChange={(e) => setInternal(e.target.checked)}
                       className="rounded border-line" />
                Internal note (not shown to the requester)
              </label>
            ) : <span />}
            <Button type="submit" disabled={busy || !body.trim()}>{busy ? 'Posting…' : 'Post comment'}</Button>
          </div>
        </form>
      </Card>

      <AsyncState {...comments} onRetry={comments.refetch}
                  isEmpty={(c) => c.length === 0}
                  emptyTitle="No comments yet"
                  emptyHint="Comments are visible to the requester unless marked internal.">
        {(list) => (
          <ul className="space-y-3">
            {list.map((c) => (
              <li key={c.id} className={`rounded-2xl border p-4 ${
                c.isInternal ? 'border-warn/30 bg-warn-soft' : 'border-line bg-surface'
              }`}>
                <div className="flex items-center justify-between gap-2">
                  <p className="text-sm font-medium text-fg">{c.authorName}</p>
                  <div className="flex items-center gap-2">
                    {c.isInternal && (
                      <span className="rounded bg-warn-soft px-2 py-0.5 text-xs font-medium text-warn">Internal</span>
                    )}
                    <time className="text-xs text-fg-subtle" dateTime={c.createdAt}>{formatRelative(c.createdAt)}</time>
                  </div>
                </div>
                <p className="mt-2 whitespace-pre-wrap text-sm text-fg">{c.body}</p>
              </li>
            ))}
          </ul>
        )}
      </AsyncState>
    </div>
  );
}

// ---- History ------------------------------------------------------------------------------------

function HistoryTab({ ticketId }: { ticketId: number }) {
  const history = useApiResource(() => ticketsApi.history(ticketId), [ticketId]);

  return (
    <AsyncState {...history} onRetry={history.refetch}
                isEmpty={(h) => h.length === 0} emptyTitle="No history yet">
      {(entries) => (
        <ol className="space-y-3">
          {entries.map((h) => (
            <li key={h.id} className="flex gap-3 rounded-2xl border border-line bg-surface p-4">
              <div className="mt-1 h-2 w-2 shrink-0 rounded-full bg-accent-soft0" aria-hidden="true" />
              <div className="min-w-0 flex-1">
                <p className="text-sm text-fg">
                  <span className="font-medium">{h.field}</span>
                  {h.oldValue ? <> changed from <code className="rounded bg-surface-2 px-1">{h.oldValue}</code></> : ' set'}
                  {h.newValue && <> to <code className="rounded bg-surface-2 px-1">{h.newValue}</code></>}
                </p>
                {h.note && <p className="mt-1 text-sm text-fg-muted">{h.note}</p>}
                <p className="mt-1 text-xs text-fg-subtle">
                  {/* "System / AI" is what the API returns when the actor was not a person. */}
                  {h.changedByName ?? 'System / AI'} · {formatDateTime(h.createdAt)}
                </p>
              </div>
            </li>
          ))}
        </ol>
      )}
    </AsyncState>
  );
}

// ---- AI workflow ----------------------------------------------------------------------------------

function AiTab({ ticketId, canStart }: { ticketId: number; canStart: boolean }) {
  const toast = useToast();
  const workflows = useApiResource(() => aiApi.workflows({ ticketId, pageSize: 20 }), [ticketId]);
  const [busy, setBusy] = useState(false);

  const start = async () => {
    setBusy(true);
    try {
      await aiApi.startWorkflow(ticketId);
      toast.success('Agent workflow started.');
      // The workflow runs in the background, so give it a moment before refreshing.
      window.setTimeout(() => workflows.refetch(), 1500);
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="space-y-4">
      {canStart && (
        <Card>
          <div className="flex flex-wrap items-center justify-between gap-3">
            <p className="text-sm text-fg-muted">
              Run the agent workflow again to re-triage this ticket with the current knowledge base.
            </p>
            <Button onClick={start} disabled={busy}>{busy ? 'Starting…' : 'Run agent workflow'}</Button>
          </div>
        </Card>
      )}

      <AsyncState {...workflows} onRetry={workflows.refetch}
                  isEmpty={(w) => w.items.length === 0}
                  emptyTitle="No agent workflow has run for this ticket"
                  emptyHint="A workflow starts automatically when a ticket is raised.">
        {(page) => (
          <ul className="space-y-2">
            {page.items.map((w) => (
              <li key={w.id}>
                <Link to={`/app/ai-workflows/${w.id}`}
                      className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-line bg-surface p-4 hover:border-accent/45">
                  <div className="min-w-0">
                    <p className="text-sm font-medium text-fg">Workflow #{w.id}</p>
                    <p className="truncate text-xs text-fg-subtle">{w.objective}</p>
                  </div>
                  <div className="flex items-center gap-2">
                    {w.pendingApprovals > 0 && (
                      <span className="rounded-full bg-warn-soft px-2 py-0.5 text-xs font-medium text-warn">
                        {w.pendingApprovals} awaiting approval
                      </span>
                    )}
                    <span className="text-xs text-fg-subtle">{w.stepCount} steps</span>
                    <WorkflowBadge status={w.status} />
                  </div>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </AsyncState>
    </div>
  );
}
