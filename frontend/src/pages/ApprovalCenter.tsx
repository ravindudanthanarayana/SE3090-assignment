import { useState } from 'react';
import { Link } from 'react-router-dom';
import { aiApi } from '../api/endpoints';
import { useApiResource } from '../hooks/useApiResource';
import { errorMessage } from '../api/client';
import { useToast } from '../context/ToastContext';
import { AsyncState, ApprovalBadge, Button, Card } from '../components/Ui';
import { Modal } from '../components/Modal';
import { Pagination } from '../components/Pagination';
import { TextArea } from '../components/Form';
import { formatDateTime, safeParse } from '../utils/format';
import type { Approval, ApprovalStatus } from '../types';

/**
 * The human-in-the-loop gate.
 *
 * These buttons are a convenience, not the control: the backend independently checks that the
 * caller is a SupportManager or Admin, that the approval is still pending, and that the action is
 * one it knows how to execute. Rejecting or requesting revision executes nothing at all.
 */
export function ApprovalCenter() {
  const toast = useToast();
  const [status, setStatus] = useState<ApprovalStatus | ''>('Pending');
  const [page, setPage] = useState(1);
  const [deciding, setDeciding] = useState<{ approval: Approval; decision: ApprovalStatus } | null>(null);

  const approvals = useApiResource(
    () => aiApi.approvals({ status: status || undefined, page, pageSize: 10 }),
    [status, page],
  );

  return (
    <div className="space-y-4">
      <header>
        <h1 className="text-xl font-semibold text-fg">Approval centre</h1>
        <p className="mt-1 text-sm text-fg-subtle">
          High-impact actions the agents recommended. Nothing here has been applied to a ticket yet.
        </p>
      </header>

      <div className="flex flex-wrap gap-2">
        {(['Pending', 'Approved', 'Rejected', 'RevisionRequested', ''] as const).map((s) => (
          <button
            key={s || 'all'}
            type="button"
            onClick={() => { setStatus(s); setPage(1); }}
            aria-pressed={status === s}
            className={`rounded-full px-3 py-1 text-sm font-medium transition ${
              status === s ? 'bg-accent-solid text-accent-fg' : 'bg-surface text-fg-muted ring-1 ring-line-strong hover:bg-surface-2'
            }`}
          >
            {s === '' ? 'All' : s.replace(/([a-z])([A-Z])/g, '$1 $2')}
          </button>
        ))}
      </div>

      <AsyncState {...approvals} onRetry={approvals.refetch}
                  isEmpty={(p) => p.items.length === 0}
                  emptyTitle={status === 'Pending' ? 'Nothing is waiting for your decision' : 'No approvals to show'}
                  emptyHint={status === 'Pending' ? 'Approvals appear here when an agent recommends a high-impact action.' : undefined}>
        {(result) => (
          <>
            <ul className="space-y-3">
              {result.items.map((a) => (
                <li key={a.id}>
                  <ApprovalCard
                    approval={a}
                    onDecide={(decision) => setDeciding({ approval: a, decision })}
                  />
                </li>
              ))}
            </ul>
            <Pagination page={result.page} pageSize={result.pageSize} totalCount={result.totalCount}
                        totalPages={result.totalPages} onPageChange={setPage} />
          </>
        )}
      </AsyncState>

      <DecisionModal
        pending={deciding}
        onClose={() => setDeciding(null)}
        onDone={(decision) => {
          setDeciding(null);
          approvals.refetch();
          toast.success(
            decision === 'Approved' ? 'Approved. The action has been applied to the ticket.'
            : decision === 'Rejected' ? 'Rejected. Nothing was changed.'
            : 'Sent back for revision. Nothing was changed.',
          );
        }}
      />
    </div>
  );
}

function ApprovalCard({ approval, onDecide }: {
  approval: Approval;
  onDecide: (decision: ApprovalStatus) => void;
}) {
  const action = safeParse<{
    actionType: string; targetUserId?: number; targetPriority?: string; description: string;
  }>(approval.proposedActionJson);

  const riskTone =
    approval.riskLevel === 'High' ? 'bg-danger-soft text-danger ring-danger/30'
    : approval.riskLevel === 'Medium' ? 'bg-warn-soft text-warn ring-warn/30'
    : 'bg-surface-2 text-fg-muted ring-line-strong';

  return (
    <Card>
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <h2 className="font-semibold text-fg">{approval.actionType}</h2>
            <ApprovalBadge status={approval.status} />
            <span className={`rounded-full px-2 py-0.5 text-xs font-medium ring-1 ring-inset ${riskTone}`}>
              {approval.riskLevel} risk
            </span>
          </div>

          <p className="mt-1 text-sm text-fg-muted">
            <Link to={`/app/tickets/${approval.ticketId}`} className="font-medium text-accent-text hover:underline">
              {approval.ticketNumber}
            </Link>
            {' — '}{approval.ticketTitle}
          </p>

          <div className="mt-3 rounded-2xl bg-surface-2 p-3 text-sm">
            <p className="text-xs font-semibold uppercase tracking-wide text-fg-subtle">Why the agent recommends this</p>
            <p className="mt-1 text-fg">{approval.reason}</p>
            {action && (
              <p className="mt-2 text-xs text-fg-subtle">
                Proposed action: <code className="rounded bg-surface px-1 py-0.5">{action.actionType}</code>
                {action.targetUserId && <> · target user {action.targetUserId}</>}
                {action.targetPriority && <> · target priority {action.targetPriority}</>}
              </p>
            )}
          </div>

          <p className="mt-2 text-xs text-fg-subtle">
            Requested {formatDateTime(approval.requestedAt)} by workflow{' '}
            <Link to={`/app/ai-workflows/${approval.workflowId}`} className="text-accent-text hover:underline">
              #{approval.workflowId}
            </Link>
            {approval.decidedByName && ` · decided by ${approval.decidedByName} on ${formatDateTime(approval.decidedAt)}`}
          </p>

          {approval.decisionNote && (
            <p className="mt-1 text-xs italic text-fg-muted">“{approval.decisionNote}”</p>
          )}
        </div>

        {approval.status === 'Pending' && (
          <div className="flex shrink-0 flex-col gap-2">
            <Button onClick={() => onDecide('Approved')}>Approve</Button>
            <Button variant="secondary" onClick={() => onDecide('RevisionRequested')}>Request revision</Button>
            <Button variant="danger" onClick={() => onDecide('Rejected')}>Reject</Button>
          </div>
        )}
      </div>
    </Card>
  );
}

function DecisionModal({ pending, onClose, onDone }: {
  pending: { approval: Approval; decision: ApprovalStatus } | null;
  onClose: () => void;
  onDone: (decision: ApprovalStatus) => void;
}) {
  const [note, setNote] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const submit = async () => {
    if (!pending) return;
    setBusy(true); setError(null);
    try {
      await aiApi.decide(pending.approval.id, pending.decision, note.trim() || undefined);
      setNote('');
      onDone(pending.decision);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  const label = pending?.decision === 'Approved' ? 'Approve'
    : pending?.decision === 'Rejected' ? 'Reject'
    : 'Request revision';

  const consequence = pending?.decision === 'Approved'
    ? 'The backend will validate this again and then apply it to the ticket inside a single transaction.'
    : 'Nothing will be changed on the ticket. The decision is recorded in the audit trail.';

  return (
    <Modal
      open={pending !== null}
      title={`${label} this recommendation?`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={busy}>Cancel</Button>
          <Button variant={pending?.decision === 'Rejected' ? 'danger' : 'primary'} onClick={submit} disabled={busy}>
            {busy ? 'Recording…' : label}
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p role="alert" className="rounded bg-danger-soft px-3 py-2 text-sm text-danger">{error}</p>}

        {pending && (
          <div className="rounded-2xl bg-surface-2 p-3 text-sm">
            <p className="font-medium text-fg">
              {pending.approval.actionType} · {pending.approval.ticketNumber}
            </p>
            <p className="mt-1 text-fg-muted">{pending.approval.reason}</p>
          </div>
        )}

        <p className="text-sm text-fg-muted">{consequence}</p>

        <TextArea id="decision-note" label="Note" rows={3} value={note}
                  onChange={(e) => setNote(e.target.value)}
                  hint="Optional. Recorded against the decision, and useful evidence at review time." />
      </div>
    </Modal>
  );
}
