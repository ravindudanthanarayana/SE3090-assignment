import { Link, useNavigate } from 'react-router-dom';
import { BarChart, Bar, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { assignmentApi, reportsApi, ticketsApi } from '../api/endpoints';
import { useApiResource } from '../hooks/useApiResource';
import { AsyncState, Card, PriorityBadge, SlaBadge, StatCard, StatusBadge } from '../components/Ui';
import { DataTable, type Column } from '../components/DataTable';
import { formatDateTime, formatHours } from '../utils/format';
import type { AgentPerformance, SlaAtRiskTicket, SupportAgent } from '../types';

/** Component B: agents, their skills and their live workload. */
export function AgentsAndWorkload() {
  const agents = useApiResource(() => assignmentApi.supportAgents(), []);

  const columns: Column<SupportAgent>[] = [
    {
      key: 'name',
      header: 'Support agent',
      render: (a) => (
        <div>
          <p className="font-medium text-fg">{a.fullName}</p>
          <p className="text-xs text-fg-subtle">{a.email}</p>
        </div>
      ),
    },
    {
      key: 'skills',
      header: 'Skills',
      render: (a) => (
        <div className="flex flex-wrap gap-1">
          {a.skills.length === 0 && <span className="text-xs text-fg-subtle">No skills recorded</span>}
          {a.skills.map((s) => (
            <span key={s.id} className="rounded bg-surface-2 px-2 py-0.5 text-xs text-fg">
              {s.categoryName} <span className="font-medium">{s.proficiencyLevel}/5</span>
            </span>
          ))}
        </div>
      ),
    },
    { key: 'open', header: 'Open', render: (a) => <span className="font-medium">{a.workload.openCount}</span> },
    { key: 'progress', header: 'In progress', render: (a) => a.workload.inProgressCount },
    {
      key: 'atRisk',
      header: 'At risk',
      render: (a) => <span className={a.workload.atRiskCount > 0 ? 'font-medium text-warn' : ''}>{a.workload.atRiskCount}</span>,
    },
    {
      key: 'breached',
      header: 'Breached',
      render: (a) => <span className={a.workload.breachedCount > 0 ? 'font-medium text-danger' : ''}>{a.workload.breachedCount}</span>,
    },
    { key: 'resolved', header: 'Resolved (30d)', render: (a) => a.workload.resolvedLast30Days },
  ];

  return (
    <div className="space-y-4">
      <header>
        <h1 className="text-xl font-semibold text-fg">Agents and workload</h1>
        <p className="mt-1 text-sm text-fg-subtle">
          The same skill levels and open-ticket counts the Assignment agent scores against.
        </p>
      </header>

      <AsyncState {...agents} onRetry={agents.refetch} isEmpty={(a) => a.length === 0}
                  emptyTitle="No support agents yet"
                  emptyHint="An administrator can create support agent accounts under Users.">
        {(list) => (
          <>
            <div className="grid gap-3 sm:grid-cols-3">
              <StatCard label="Support agents" value={list.length} />
              <StatCard label="Open tickets" value={list.reduce((s, a) => s + a.workload.openCount, 0)} />
              <StatCard label="Breaching SLA" value={list.reduce((s, a) => s + a.workload.breachedCount, 0)} tone="danger" />
            </div>

            <DataTable columns={columns} rows={list} rowKey={(a) => a.userId}
                       loading={agents.loading} caption="Support agents and their workload" />
          </>
        )}
      </AsyncState>
    </div>
  );
}

/** Component B: the unassigned queue, with the deterministic recommendation for each ticket. */
export function AssignmentBoard() {
  const navigate = useNavigate();
  const unassigned = useApiResource(
    () => ticketsApi.list({ unassigned: true, pageSize: 50, sortBy: 'priority', sortDir: 'desc' }),
    [],
  );

  return (
    <div className="space-y-4">
      <header>
        <h1 className="text-xl font-semibold text-fg">Assignment board</h1>
        <p className="mt-1 text-sm text-fg-subtle">
          Tickets with no owner yet, highest priority first. Open one to see the ranked candidates.
        </p>
      </header>

      <AsyncState {...unassigned} onRetry={unassigned.refetch}
                  isEmpty={(p) => p.items.length === 0}
                  emptyTitle="Every ticket has an owner"
                  emptyHint="Nothing is waiting to be assigned.">
        {(page) => (
          <ul className="space-y-2">
            {page.items.map((t) => (
              <li key={t.id}>
                <button
                  type="button"
                  onClick={() => navigate(`/app/tickets/${t.id}`)}
                  className="flex w-full flex-wrap items-center justify-between gap-3 rounded-2xl border border-line bg-surface p-4 text-left hover:border-accent/45"
                >
                  <div className="min-w-0">
                    <p className="text-sm font-medium text-fg">{t.ticketNumber}</p>
                    <p className="truncate text-sm text-fg-muted">{t.title}</p>
                  </div>
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="text-xs text-fg-subtle">{t.categoryName}</span>
                    <PriorityBadge priority={t.priority} />
                    <SlaBadge state={t.slaState} />
                    <StatusBadge status={t.status} />
                  </div>
                </button>
              </li>
            ))}
          </ul>
        )}
      </AsyncState>
    </div>
  );
}

/** Component D: SLA position across the desk. */
export function SlaDashboard() {
  const atRisk = useApiResource(() => ticketsApi.slaAtRisk(), []);

  const columns: Column<SlaAtRiskTicket>[] = [
    {
      key: 'ticket',
      header: 'Ticket',
      render: (t) => (
        <div>
          <Link to={`/app/tickets/${t.id}`} className="font-medium text-accent-text hover:underline">{t.ticketNumber}</Link>
          <p className="max-w-xs truncate text-fg-muted">{t.title}</p>
        </div>
      ),
    },
    { key: 'status', header: 'Status', render: (t) => <StatusBadge status={t.status} /> },
    { key: 'priority', header: 'Priority', render: (t) => <PriorityBadge priority={t.priority} /> },
    { key: 'assigned', header: 'Owner', render: (t) => t.assignedToName ?? <em className="text-fg-subtle">Unassigned</em> },
    { key: 'sla', header: 'SLA', render: (t) => <SlaBadge state={t.slaState} /> },
    {
      key: 'remaining',
      header: 'Time',
      render: (t) => (
        <span className={t.hoursRemaining < 0 ? 'font-medium text-danger' : 'text-warn'}>
          {formatHours(t.hoursRemaining)}
        </span>
      ),
    },
    { key: 'due', header: 'Due', render: (t) => <span className="text-fg-subtle">{formatDateTime(t.slaDueAt)}</span> },
  ];

  return (
    <div className="space-y-4">
      <header>
        <h1 className="text-xl font-semibold text-fg">SLA and escalation</h1>
        <p className="mt-1 text-sm text-fg-subtle">
          Tickets that have breached their SLA, or are inside the final quarter of their window.
        </p>
      </header>

      <AsyncState {...atRisk} onRetry={atRisk.refetch} isEmpty={(t) => t.length === 0}
                  emptyTitle="Nothing is at risk"
                  emptyHint="Every open ticket has comfortable time remaining.">
        {(tickets) => (
          <>
            <div className="grid gap-3 sm:grid-cols-2">
              <StatCard label="Breached" value={tickets.filter((t) => t.slaState === 'Breached').length} tone="danger" />
              <StatCard label="At risk" value={tickets.filter((t) => t.slaState === 'AtRisk').length} tone="warn" />
            </div>
            <DataTable columns={columns} rows={tickets} rowKey={(t) => t.id}
                       loading={atRisk.loading} caption="Tickets at risk of breaching their SLA" />
          </>
        )}
      </AsyncState>
    </div>
  );
}

/** Component D: management reporting. */
export function Reports() {
  const sla = useApiResource(() => reportsApi.sla(), []);
  const performance = useApiResource(() => reportsApi.agentPerformance(), []);

  const columns: Column<AgentPerformance>[] = [
    { key: 'name', header: 'Support agent', render: (a) => <span className="font-medium text-fg">{a.fullName}</span> },
    { key: 'assigned', header: 'Assigned', render: (a) => a.assignedTotal },
    { key: 'resolved', header: 'Resolved', render: (a) => a.resolved },
    { key: 'open', header: 'Open now', render: (a) => a.openNow },
    { key: 'avg', header: 'Avg resolution', render: (a) => (a.averageResolutionHours > 0 ? `${a.averageResolutionHours} h` : '—') },
    {
      key: 'breach',
      header: 'Breach rate',
      render: (a) => (
        <span className={a.breachRatePercent > 20 ? 'font-medium text-danger' : 'text-fg'}>
          {a.breachRatePercent}%
        </span>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-xl font-semibold text-fg">Reports</h1>
        <p className="mt-1 text-sm text-fg-subtle">SLA performance and support agent throughput.</p>
      </header>

      <AsyncState {...sla} onRetry={sla.refetch}>
        {(report) => (
          <>
            <div className="grid gap-3 sm:grid-cols-4">
              <StatCard label="On track" value={report.onTrack} tone="good" />
              <StatCard label="At risk" value={report.atRisk} tone="warn" />
              <StatCard label="Breached" value={report.breached} tone="danger" />
              <StatCard label="Breach rate" value={`${report.breachRatePercent}%`}
                        tone={report.breachRatePercent > 20 ? 'danger' : 'default'} />
            </div>

            <Card title="SLA position by category">
              {report.byCategory.length === 0 ? (
                <p className="text-sm text-fg-subtle">No open tickets.</p>
              ) : (
                <div className="h-72">
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={report.byCategory} margin={{ top: 8, right: 8, bottom: 8, left: -20 }}>
                      <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" vertical={false} />
                      <XAxis dataKey="categoryName" tick={{ fontSize: 11 }} interval={0} angle={-20} textAnchor="end" height={70} />
                      <YAxis allowDecimals={false} tick={{ fontSize: 11 }} />
                      <Tooltip />
                      <Bar dataKey="total" name="Open" fill="#94a3b8" radius={[4, 4, 0, 0]} />
                      <Bar dataKey="atRisk" name="At risk" fill="#f59e0b" radius={[4, 4, 0, 0]} />
                      <Bar dataKey="breached" name="Breached" fill="#ef4444" radius={[4, 4, 0, 0]} />
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              )}
            </Card>
          </>
        )}
      </AsyncState>

      <section>
        <h2 className="mb-3 text-sm font-semibold text-fg">Support agent performance</h2>
        <AsyncState {...performance} onRetry={performance.refetch} isEmpty={(p) => p.length === 0}
                    emptyTitle="No support agents to report on">
          {(rows) => (
            <DataTable columns={columns} rows={rows} rowKey={(a) => a.userId}
                       loading={performance.loading} caption="Support agent performance" />
          )}
        </AsyncState>
      </section>
    </div>
  );
}
