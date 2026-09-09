import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { adminApi, ticketsApi, type TicketQuery } from '../api/endpoints';
import { useApiResource, useDebounced } from '../hooks/useApiResource';
import { DataTable, type Column } from '../components/DataTable';
import { Pagination } from '../components/Pagination';
import { AsyncState, Button, PriorityBadge, SlaBadge, StatusBadge } from '../components/Ui';
import { formatRelative } from '../utils/format';
import type { TicketListItem } from '../types';

const STATUSES = ['New', 'Assigned', 'InProgress', 'OnHold', 'Escalated', 'Resolved', 'Closed', 'Cancelled'];
const PRIORITIES = ['Low', 'Medium', 'High', 'Critical'];
const SLA_STATES = ['OnTrack', 'AtRisk', 'Breached'];

/**
 * Component A's main screen: search, filtering, sorting and server-side pagination.
 * Every filter is sent to the API - nothing is filtered in the browser.
 */
export function TicketList() {
  const navigate = useNavigate();
  const [query, setQuery] = useState<TicketQuery>({ page: 1, pageSize: 10, sortBy: 'createdAt', sortDir: 'desc' });
  const [searchInput, setSearchInput] = useState('');
  const search = useDebounced(searchInput);

  const categories = useApiResource(() => adminApi.categories(), []);

  const tickets = useApiResource(
    () => ticketsApi.list({ ...query, search }),
    [search, query.status, query.priority, query.categoryId, query.slaState,
     query.unassigned, query.sortBy, query.sortDir, query.page, query.pageSize],
  );

  /** Any filter change resets to page 1, or the user can land on an empty page. */
  const setFilter = (patch: Partial<TicketQuery>) => setQuery((q) => ({ ...q, ...patch, page: 1 }));

  const toggleSort = (key: string) =>
    setQuery((q) => ({
      ...q,
      sortBy: key,
      sortDir: q.sortBy === key && q.sortDir === 'desc' ? 'asc' : 'desc',
      page: 1,
    }));

  const columns: Column<TicketListItem>[] = [
    {
      key: 'ticketNumber',
      header: 'Ticket',
      render: (t) => (
        <div>
          <Link to={`/app/tickets/${t.id}`} className="font-medium text-accent-text hover:underline"
                onClick={(e) => e.stopPropagation()}>
            {t.ticketNumber}
          </Link>
          <p className="max-w-xs truncate text-fg-muted">{t.title}</p>
        </div>
      ),
    },
    { key: 'status', header: 'Status', sortable: true, render: (t) => <StatusBadge status={t.status} /> },
    { key: 'priority', header: 'Priority', sortable: true, render: (t) => <PriorityBadge priority={t.priority} /> },
    { key: 'category', header: 'Category', render: (t) => <span className="text-fg-muted">{t.categoryName}</span> },
    {
      key: 'assigned',
      header: 'Assigned to',
      render: (t) => <span className="text-fg-muted">{t.assignedToName ?? <em className="text-fg-subtle">Unassigned</em>}</span>,
    },
    {
      key: 'slaDueAt',
      header: 'SLA',
      sortable: true,
      render: (t) => (
        <div className="space-y-1">
          <SlaBadge state={t.slaState} />
          <p className="text-xs text-fg-subtle">{formatRelative(t.slaDueAt)}</p>
        </div>
      ),
    },
    { key: 'createdAt', header: 'Raised', sortable: true, render: (t) => <span className="text-fg-subtle">{formatRelative(t.createdAt)}</span> },
  ];

  return (
    <div className="space-y-4">
      <header className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-fg">Tickets</h1>
          <p className="mt-1 text-sm text-fg-subtle">Search, filter and sort across the service desk.</p>
        </div>
        <Button onClick={() => navigate('/app/tickets/new')}>New ticket</Button>
      </header>

      <div className="grid gap-3 rounded-2xl border border-line bg-surface p-4 shadow-card sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5">
        <div className="sm:col-span-2 lg:col-span-3 xl:col-span-2">
          <label htmlFor="search" className="mb-1 block text-xs font-medium text-fg-muted">Search</label>
          <input
            id="search" type="search" value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            placeholder="Ticket number, title or description"
            className="w-full rounded-xl border border-line bg-surface px-3 py-2 text-sm text-fg transition focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/25"
          />
        </div>

        <FilterSelect id="status" label="Status" value={query.status ?? ''} options={STATUSES}
                      onChange={(v) => setFilter({ status: v as TicketQuery['status'] })} />
        <FilterSelect id="priority" label="Priority" value={query.priority ?? ''} options={PRIORITIES}
                      onChange={(v) => setFilter({ priority: v as TicketQuery['priority'] })} />
        <FilterSelect id="sla" label="SLA" value={query.slaState ?? ''} options={SLA_STATES}
                      onChange={(v) => setFilter({ slaState: v })} />

        <div>
          <label htmlFor="category" className="mb-1 block text-xs font-medium text-fg-muted">Category</label>
          <select
            id="category" value={query.categoryId ?? ''}
            onChange={(e) => setFilter({ categoryId: e.target.value ? Number(e.target.value) : '' })}
            className="w-full rounded-xl border border-line bg-surface px-3 py-2 text-sm text-fg transition focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/25"
          >
            <option value="">All categories</option>
            {categories.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </div>

        <label className="flex items-end gap-2 pb-2 text-sm text-fg-muted">
          <input type="checkbox" checked={query.unassigned ?? false}
                 onChange={(e) => setFilter({ unassigned: e.target.checked || undefined })}
                 className="rounded border-line" />
          Unassigned only
        </label>
      </div>

      <AsyncState {...tickets} onRetry={tickets.refetch}>
        {(page) => (
          <>
            <DataTable
              columns={columns}
              rows={page.items}
              rowKey={(t) => t.id}
              loading={tickets.loading}
              sortBy={query.sortBy}
              sortDir={query.sortDir}
              onSort={toggleSort}
              onRowClick={(t) => navigate(`/app/tickets/${t.id}`)}
              caption="Support tickets"
              emptyTitle="No tickets match your filters"
              emptyHint="Try clearing the search box or widening the filters."
            />
            <Pagination
              page={page.page} pageSize={page.pageSize}
              totalCount={page.totalCount} totalPages={page.totalPages}
              onPageChange={(p) => setQuery((q) => ({ ...q, page: p }))}
            />
          </>
        )}
      </AsyncState>
    </div>
  );
}

function FilterSelect({ id, label, value, options, onChange }: {
  id: string; label: string; value: string; options: string[]; onChange: (value: string) => void;
}) {
  return (
    <div>
      <label htmlFor={id} className="mb-1 block text-xs font-medium text-fg-muted">{label}</label>
      <select
        id={id} value={value} onChange={(e) => onChange(e.target.value)}
        className="w-full rounded-xl border border-line bg-surface px-3 py-2 text-sm text-fg transition focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/25"
      >
        <option value="">All</option>
        {options.map((o) => <option key={o} value={o}>{o.replace(/([a-z])([A-Z])/g, '$1 $2')}</option>)}
      </select>
    </div>
  );
}
