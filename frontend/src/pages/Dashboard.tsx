import { Link } from 'react-router-dom';
import { BarChart, Bar, PieChart, Pie, Cell, ResponsiveContainer, Tooltip, XAxis, YAxis, CartesianGrid } from 'recharts';
import { reportsApi } from '../api/endpoints';
import { useApiResource } from '../hooks/useApiResource';
import { useAuth } from '../context/AuthContext';
import { AsyncState, Card, StatCard } from '../components/Ui';

// A small, fixed palette: readable, colour-blind safe enough, and consistent across both charts.
const PALETTE = ['#3b6fe0', '#5b8def', '#7aa5f5', '#f59e0b', '#ef4444', '#10b981', '#94a3b8', '#64748b'];

export function Dashboard() {
  const { user, hasRole } = useAuth();
  const dashboard = useApiResource(() => reportsApi.dashboard(), []);
  const isStaff = hasRole('SupportAgent', 'SupportManager', 'Admin');

  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-xl font-semibold text-fg">
          {isStaff ? 'Service desk overview' : 'My requests'}
        </h1>
        <p className="mt-1 text-sm text-fg-subtle">
          Signed in as {user?.fullName} ({user?.role}).
          {!isStaff && ' These figures cover only the tickets you raised.'}
        </p>
      </header>

      <AsyncState {...dashboard} onRetry={dashboard.refetch}>
        {(data) => (
          <>
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
              <StatCard label="Total tickets" value={data.totalTickets} />
              <StatCard label="Open" value={data.openTickets} />
              <StatCard label="In progress" value={data.inProgressTickets} />
              <StatCard label="Resolved" value={data.resolvedTickets} tone="good" />
              <StatCard label="High priority" value={data.highPriorityTickets} tone="warn" />
              <StatCard label="Escalated" value={data.escalatedTickets} tone="warn" />
              <StatCard label="SLA at risk" value={data.slaAtRiskTickets} tone="warn" />
              <StatCard label="SLA breached" value={data.slaBreachedTickets} tone="danger" />
            </div>

            {isStaff && (
              <div className="grid gap-3 sm:grid-cols-2">
                <Link to="/app/ai-workflows" className="block">
                  <StatCard label="Active AI workflows" value={data.activeWorkflows} />
                </Link>
                <Link to="/app/approvals" className="block">
                  <StatCard
                    label="Approvals awaiting your decision"
                    value={data.pendingApprovals}
                    tone={data.pendingApprovals > 0 ? 'warn' : 'default'}
                  />
                </Link>
              </div>
            )}

            <div className="grid gap-6 lg:grid-cols-2">
              <Card title="Tickets by status">
                {data.byStatus.length === 0 ? (
                  <p className="text-sm text-fg-subtle">No tickets yet.</p>
                ) : (
                  <div className="h-64">
                    <ResponsiveContainer width="100%" height="100%">
                      <BarChart data={data.byStatus} margin={{ top: 8, right: 8, bottom: 8, left: -20 }}>
                        <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" vertical={false} />
                        <XAxis dataKey="label" tick={{ fontSize: 11 }} interval={0} angle={-25} textAnchor="end" height={60} />
                        <YAxis allowDecimals={false} tick={{ fontSize: 11 }} />
                        <Tooltip />
                        <Bar dataKey="count" name="Tickets" fill="#3b6fe0" radius={[4, 4, 0, 0]} />
                      </BarChart>
                    </ResponsiveContainer>
                  </div>
                )}
              </Card>

              <Card title="Tickets by category">
                {data.byCategory.length === 0 ? (
                  <p className="text-sm text-fg-subtle">No tickets yet.</p>
                ) : (
                  <div className="h-64">
                    <ResponsiveContainer width="100%" height="100%">
                      <PieChart>
                        <Pie data={data.byCategory} dataKey="count" nameKey="label"
                             cx="50%" cy="50%" outerRadius={90} label={{ fontSize: 11 }}>
                          {data.byCategory.map((entry, index) => (
                            <Cell key={entry.label} fill={PALETTE[index % PALETTE.length]} />
                          ))}
                        </Pie>
                        <Tooltip />
                      </PieChart>
                    </ResponsiveContainer>
                  </div>
                )}
              </Card>
            </div>
          </>
        )}
      </AsyncState>
    </div>
  );
}
