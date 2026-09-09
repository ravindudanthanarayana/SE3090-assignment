import { useState } from 'react';
import { ApprovalBadge, PriorityBadge, SlaBadge, StatusBadge, WorkflowBadge } from '../Ui';

/**
 * A preview of the real application, rendered with the real components, the real design tokens
 * and the values the seeded demo database actually contains. It is a live rendering of the
 * product's UI rather than a screenshot, so it themes correctly and never goes stale.
 */

const tabs = ['Dashboard', 'Tickets', 'AI workflow', 'Approvals'] as const;
type Tab = (typeof tabs)[number];

export function AppPreview({ initialTab = 'Dashboard' }: { initialTab?: Tab }) {
  const [tab, setTab] = useState<Tab>(initialTab);

  return (
    <div className="overflow-hidden rounded-3xl border border-line bg-surface shadow-float">
      {/* Window chrome */}
      <div className="flex items-center gap-3 border-b border-line bg-surface-2 px-4 py-3">
        <div className="flex gap-1.5" aria-hidden="true">
          <span className="h-2.5 w-2.5 rounded-full bg-line-strong" />
          <span className="h-2.5 w-2.5 rounded-full bg-line-strong" />
          <span className="h-2.5 w-2.5 rounded-full bg-line-strong" />
        </div>
        <div className="hidden flex-1 justify-center sm:flex">
          <span className="rounded-full bg-surface px-3 py-1 text-[11px] text-fg-subtle">
            app.smartdesk.ai
          </span>
        </div>
        <div className="hidden w-16 sm:block" />
      </div>

      {/* Tabs */}
      <div role="tablist" aria-label="Application preview" className="flex gap-1 overflow-x-auto border-b border-line px-3 py-2">
        {tabs.map((name) => (
          <button
            key={name}
            role="tab"
            type="button"
            aria-selected={tab === name}
            onClick={() => setTab(name)}
            className={`whitespace-nowrap rounded-full px-3.5 py-1.5 text-xs font-medium transition ${
              tab === name ? 'bg-accent-soft text-accent-text' : 'text-fg-subtle hover:bg-surface-hover hover:text-fg'
            }`}
          >
            {name}
          </button>
        ))}
      </div>

      <div role="tabpanel" className="bg-bg-subtle p-4 sm:p-5">
        {tab === 'Dashboard' && <DashboardPanel />}
        {tab === 'Tickets' && <TicketsPanel />}
        {tab === 'AI workflow' && <WorkflowPanel />}
        {tab === 'Approvals' && <ApprovalPanel />}
      </div>
    </div>
  );
}

function Stat({ label, value, tone = 'text-fg' }: { label: string; value: string; tone?: string }) {
  return (
    <div className="rounded-2xl border border-line bg-surface p-3">
      <p className="text-[10px] font-medium uppercase tracking-wide text-fg-subtle">{label}</p>
      <p className={`mt-1 text-xl font-semibold tabular-nums ${tone}`}>{value}</p>
    </div>
  );
}

function DashboardPanel() {
  return (
    <div className="space-y-3">
      <div className="grid grid-cols-2 gap-2.5 sm:grid-cols-4">
        <Stat label="Total tickets" value="23" />
        <Stat label="Open" value="15" />
        <Stat label="SLA at risk" value="3" tone="text-warn" />
        <Stat label="Breached" value="6" tone="text-danger" />
      </div>
      <div className="grid gap-2.5 sm:grid-cols-2">
        <div className="rounded-2xl border border-line bg-surface p-4">
          <p className="text-xs font-semibold text-fg">Tickets by status</p>
          <div className="mt-3 flex h-24 items-end gap-1.5" aria-hidden="true">
            {[8, 5, 6, 2, 1, 4, 3].map((height, i) => (
              <div key={i} className="flex-1 rounded-t bg-accent/70" style={{ height: `${height * 11}%` }} />
            ))}
          </div>
        </div>
        <div className="rounded-2xl border border-line bg-surface p-4">
          <p className="text-xs font-semibold text-fg">Awaiting your decision</p>
          <div className="mt-3 space-y-2">
            <div className="flex items-center justify-between rounded-lg bg-warn-soft px-3 py-2">
              <span className="text-xs text-fg-muted">TKT-000023 · Escalate</span>
              <ApprovalBadge status="Pending" />
            </div>
            <div className="flex items-center justify-between rounded-lg bg-surface-2 px-3 py-2">
              <span className="text-xs text-fg-muted">TKT-000021 · Assign</span>
              <ApprovalBadge status="Approved" />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

const rows = [
  { id: 'TKT-000023', title: 'Whole sales floor lost VPN access', status: 'Escalated', priority: 'Critical', sla: 'AtRisk', owner: 'Priya Network' },
  { id: 'TKT-000021', title: 'VPN certificate expired', status: 'Assigned', priority: 'High', sla: 'OnTrack', owner: 'Priya Network' },
  { id: 'TKT-000006', title: 'Laptop will not turn on', status: 'Assigned', priority: 'Critical', sla: 'Breached', owner: 'Sam Hardware' },
  { id: 'TKT-000005', title: 'Printer on level 3 not working', status: 'InProgress', priority: 'Medium', sla: 'OnTrack', owner: 'Sam Hardware' },
] as const;

function TicketsPanel() {
  return (
    <div className="overflow-hidden rounded-2xl border border-line bg-surface">
      <div className="overflow-x-auto">
        <table className="w-full min-w-[520px] text-left text-xs">
          <thead className="border-b border-line bg-surface-2 text-[10px] uppercase tracking-wide text-fg-subtle">
            <tr>
              <th scope="col" className="px-3 py-2 font-medium">Ticket</th>
              <th scope="col" className="px-3 py-2 font-medium">Status</th>
              <th scope="col" className="px-3 py-2 font-medium">Priority</th>
              <th scope="col" className="px-3 py-2 font-medium">SLA</th>
              <th scope="col" className="hidden px-3 py-2 font-medium sm:table-cell">Owner</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-line">
            {rows.map((row) => (
              <tr key={row.id}>
                <td className="px-3 py-2.5">
                  <p className="font-medium text-accent-text">{row.id}</p>
                  <p className="max-w-[13rem] truncate text-fg-subtle">{row.title}</p>
                </td>
                <td className="px-3 py-2.5"><StatusBadge status={row.status} /></td>
                <td className="px-3 py-2.5"><PriorityBadge priority={row.priority} /></td>
                <td className="px-3 py-2.5"><SlaBadge state={row.sla} /></td>
                <td className="hidden px-3 py-2.5 text-fg-subtle sm:table-cell">{row.owner}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

const steps = [
  { n: 0, agent: 'Planner agent', ms: 5428, tools: [] as string[] },
  { n: 1, agent: 'Triage agent', ms: 2120, tools: ['GetTicket'] },
  { n: 2, agent: 'Solution agent', ms: 2722, tools: ['SearchKnowledgeBase'] },
  { n: 3, agent: 'Assignment agent', ms: 7225, tools: ['GetSupportAgents', 'GetAgentWorkload', 'ScoreAssignmentCandidates'] },
  { n: 4, agent: 'Validation agent', ms: 2949, tools: ['CheckSla'] },
];

function WorkflowPanel() {
  return (
    <div className="space-y-2.5">
      <div className="flex flex-wrap items-center justify-between gap-2 rounded-2xl border border-line bg-surface px-4 py-3">
        <div>
          <p className="text-xs font-semibold text-fg">Workflow #3 · TKT-000023</p>
          <p className="text-[11px] text-fg-subtle">Triage, research and route the request</p>
        </div>
        <WorkflowBadge status="AwaitingApproval" />
      </div>

      {steps.map((step) => (
        <div key={step.n} className="rounded-2xl border border-line bg-surface px-4 py-3">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <p className="text-xs font-semibold text-fg">{step.n}. {step.agent}</p>
            <span className="text-[11px] text-fg-subtle">{step.ms} ms · succeeded</span>
          </div>
          {step.tools.length > 0 && (
            <div className="mt-2 flex flex-wrap gap-1.5">
              {step.tools.map((tool) => (
                <code key={tool} className="rounded bg-surface-2 px-1.5 py-0.5 text-[10px] text-fg-muted">
                  {tool}
                </code>
              ))}
            </div>
          )}
        </div>
      ))}
    </div>
  );
}

function ApprovalPanel() {
  return (
    <div className="rounded-2xl border border-line bg-surface p-4">
      <div className="flex flex-wrap items-center gap-2">
        <h3 className="text-sm font-semibold text-fg">Escalate</h3>
        <ApprovalBadge status="Pending" />
        <span className="rounded-full bg-danger-soft px-2 py-0.5 text-[10px] font-medium text-danger ring-1 ring-inset ring-danger/30">
          High risk
        </span>
      </div>

      <p className="mt-2 text-xs text-fg-subtle">
        <span className="font-medium text-accent-text">TKT-000023</span> — Whole sales floor lost VPN access
      </p>

      <div className="mt-3 rounded-lg bg-surface-2 p-3">
        <p className="text-[10px] font-semibold uppercase tracking-wide text-fg-subtle">
          Why the agent recommends this
        </p>
        <p className="mt-1 text-xs leading-relaxed text-fg-muted">
          The ticket is classified as Critical due to a department-wide outage, which overrides the
          request's original Low priority. Immediate escalation is required to protect the SLA.
        </p>
      </div>

      <div className="mt-3 flex flex-wrap gap-2">
        <span className="rounded-full bg-accent-solid px-3 py-1.5 text-xs font-medium text-accent-fg">Approve</span>
        <span className="rounded-full border border-line px-3 py-1.5 text-xs font-medium text-fg-muted">Request revision</span>
        <span className="rounded-full border border-line px-3 py-1.5 text-xs font-medium text-fg-muted">Reject</span>
      </div>

      <p className="mt-3 text-[11px] text-fg-subtle">
        Nothing has been applied to the ticket. Only an approval executes the action.
      </p>
    </div>
  );
}
