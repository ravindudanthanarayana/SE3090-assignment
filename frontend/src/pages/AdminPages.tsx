import { useState, type FormEvent } from 'react';
import { adminApi } from '../api/endpoints';
import { useApiResource, useDebounced } from '../hooks/useApiResource';
import { errorMessage } from '../api/client';
import { useToast } from '../context/ToastContext';
import { AsyncState, Button, Card } from '../components/Ui';
import { DataTable, type Column } from '../components/DataTable';
import { Pagination } from '../components/Pagination';
import { ConfirmDialog, Modal } from '../components/Modal';
import { Select, TextInput } from '../components/Form';
import { formatDateTime, prettyJson } from '../utils/format';
import type { AuditLog, Category, Role, User } from '../types';

const ROLES: Role[] = ['Employee', 'SupportAgent', 'SupportManager', 'Admin'];

export function AdminUsers() {
  const toast = useToast();
  const [searchInput, setSearchInput] = useState('');
  const search = useDebounced(searchInput);
  const [role, setRole] = useState('');
  const [page, setPage] = useState(1);
  const [editing, setEditing] = useState<User | 'new' | null>(null);
  const [deactivating, setDeactivating] = useState<User | null>(null);
  const [busy, setBusy] = useState(false);

  const users = useApiResource(
    () => adminApi.users({ search: search || undefined, role: role || undefined, page, pageSize: 10 }),
    [search, role, page],
  );

  const deactivate = async () => {
    if (!deactivating) return;
    setBusy(true);
    try {
      await adminApi.deactivateUser(deactivating.id);
      toast.success(`${deactivating.fullName} deactivated.`);
      users.refetch();
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setBusy(false); setDeactivating(null);
    }
  };

  const columns: Column<User>[] = [
    {
      key: 'name',
      header: 'User',
      render: (u) => (
        <div>
          <p className="font-medium text-fg">{u.fullName}</p>
          <p className="text-xs text-fg-subtle">{u.email}</p>
        </div>
      ),
    },
    { key: 'role', header: 'Role', render: (u) => <span className="rounded bg-surface-2 px-2 py-0.5 text-xs">{u.role}</span> },
    { key: 'department', header: 'Department', render: (u) => u.department ?? '—' },
    {
      key: 'active',
      header: 'Status',
      render: (u) => (
        <span className={u.isActive ? 'text-ok' : 'text-fg-subtle'}>{u.isActive ? 'Active' : 'Deactivated'}</span>
      ),
    },
    { key: 'created', header: 'Created', render: (u) => <span className="text-fg-subtle">{formatDateTime(u.createdAt)}</span> },
    {
      key: 'actions',
      header: '',
      render: (u) => (
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onClick={() => setEditing(u)}>Edit</Button>
          {u.isActive && <Button variant="ghost" onClick={() => setDeactivating(u)}>Deactivate</Button>}
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-4">
      <header className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-fg">Users</h1>
          <p className="mt-1 text-sm text-fg-subtle">
            Creating a user here is the only way to grant a staff or administrator role.
          </p>
        </div>
        <Button onClick={() => setEditing('new')}>New user</Button>
      </header>

      <div className="grid gap-3 rounded-2xl border border-line bg-surface p-4 shadow-card sm:grid-cols-3">
        <div className="sm:col-span-2">
          <label htmlFor="user-search" className="mb-1 block text-xs font-medium text-fg-muted">Search</label>
          <input id="user-search" type="search" value={searchInput} onChange={(e) => setSearchInput(e.target.value)}
                 placeholder="Name or email"
                 className="w-full rounded-xl border border-line bg-surface px-3 py-2 text-sm text-fg transition focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/25" />
        </div>
        <div>
          <label htmlFor="user-role" className="mb-1 block text-xs font-medium text-fg-muted">Role</label>
          <select id="user-role" value={role} onChange={(e) => { setRole(e.target.value); setPage(1); }}
                  className="w-full rounded-xl border border-line bg-surface px-3 py-2 text-sm text-fg transition focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/25">
            <option value="">All roles</option>
            {ROLES.map((r) => <option key={r} value={r}>{r}</option>)}
          </select>
        </div>
      </div>

      <AsyncState {...users} onRetry={users.refetch} isEmpty={(p) => p.items.length === 0}
                  emptyTitle="No users match your search">
        {(result) => (
          <>
            <DataTable columns={columns} rows={result.items} rowKey={(u) => u.id}
                       loading={users.loading} caption="System users" />
            <Pagination page={result.page} pageSize={result.pageSize} totalCount={result.totalCount}
                        totalPages={result.totalPages} onPageChange={setPage} />
          </>
        )}
      </AsyncState>

      <UserModal user={editing} onClose={() => setEditing(null)}
                 onSaved={() => { setEditing(null); users.refetch(); toast.success('User saved.'); }} />

      <ConfirmDialog
        open={deactivating !== null}
        title="Deactivate this user?"
        message={`${deactivating?.fullName} will no longer be able to sign in. Their tickets, comments and audit history are kept.`}
        confirmLabel="Deactivate" destructive busy={busy}
        onConfirm={deactivate} onCancel={() => setDeactivating(null)}
      />
    </div>
  );
}

function UserModal({ user, onClose, onSaved }: {
  user: User | 'new' | null; onClose: () => void; onSaved: () => void;
}) {
  const isNew = user === 'new';
  const existing = user !== 'new' && user !== null ? user : null;

  const [form, setForm] = useState({ email: '', password: '', fullName: '', department: '', role: 'Employee' as Role, isActive: true });
  const [key, setKey] = useState<number | 'new' | null>(null);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  // Reset the form whenever a different user is opened.
  const currentKey = isNew ? 'new' : existing?.id ?? null;
  if (currentKey !== key) {
    setKey(currentKey);
    setForm({
      email: existing?.email ?? '',
      password: '',
      fullName: existing?.fullName ?? '',
      department: existing?.department ?? '',
      role: existing?.role ?? 'Employee',
      isActive: existing?.isActive ?? true,
    });
    setErrors({}); setFormError(null);
  }

  const validate = () => {
    const next: Record<string, string> = {};
    if (!form.fullName.trim()) next.fullName = 'Enter a full name.';
    if (isNew && !/^\S+@\S+\.\S+$/.test(form.email)) next.email = 'Enter a valid email address.';
    if (isNew && form.password.length < 8) next.password = 'Use at least 8 characters.';
    setErrors(next);
    return Object.keys(next).length === 0;
  };

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setFormError(null);
    if (!validate()) return;

    setBusy(true);
    try {
      if (isNew) {
        await adminApi.createUser({
          email: form.email.trim(), password: form.password, fullName: form.fullName.trim(),
          department: form.department.trim() || undefined, role: form.role,
        });
      } else if (existing) {
        await adminApi.updateUser(existing.id, {
          fullName: form.fullName.trim(), department: form.department.trim() || undefined,
          role: form.role, isActive: form.isActive,
        });
      }
      onSaved();
    } catch (error) {
      setFormError(errorMessage(error));
    } finally {
      setBusy(false);
    }
  };

  return (
    <Modal open={user !== null} title={isNew ? 'Create a user' : 'Edit user'} onClose={onClose}
           footer={<>
             <Button variant="secondary" onClick={onClose} disabled={busy}>Cancel</Button>
             <Button onClick={submit} disabled={busy}>{busy ? 'Saving…' : 'Save'}</Button>
           </>}>
      <form onSubmit={submit} noValidate className="space-y-4">
        {formError && <p role="alert" className="rounded bg-danger-soft px-3 py-2 text-sm text-danger">{formError}</p>}

        <TextInput id="u-fullName" label="Full name" required value={form.fullName}
                   onChange={(e) => setForm({ ...form, fullName: e.target.value })} error={errors.fullName} />

        {isNew && (
          <>
            <TextInput id="u-email" label="Email address" type="email" required value={form.email}
                       onChange={(e) => setForm({ ...form, email: e.target.value })} error={errors.email} />
            <TextInput id="u-password" label="Temporary password" type="password" required value={form.password}
                       onChange={(e) => setForm({ ...form, password: e.target.value })} error={errors.password}
                       hint="At least 8 characters. Stored only as a BCrypt hash." />
          </>
        )}

        <TextInput id="u-department" label="Department" value={form.department}
                   onChange={(e) => setForm({ ...form, department: e.target.value })} />

        <Select id="u-role" label="Role" value={form.role}
                onChange={(e) => setForm({ ...form, role: e.target.value as Role })}>
          {ROLES.map((r) => <option key={r} value={r}>{r}</option>)}
        </Select>

        {!isNew && (
          <label className="flex items-center gap-2 text-sm text-fg">
            <input type="checkbox" checked={form.isActive}
                   onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
                   className="rounded border-line" />
            Active
          </label>
        )}
      </form>
    </Modal>
  );
}

export function AdminCategories() {
  const toast = useToast();
  const categories = useApiResource(() => adminApi.categories(), []);
  const [editing, setEditing] = useState<Category | 'new' | null>(null);
  const [deleting, setDeleting] = useState<Category | null>(null);
  const [busy, setBusy] = useState(false);

  const remove = async () => {
    if (!deleting) return;
    setBusy(true);
    try {
      await adminApi.deleteCategory(deleting.id);
      toast.success('Category deleted.');
      categories.refetch();
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setBusy(false); setDeleting(null);
    }
  };

  const columns: Column<Category>[] = [
    {
      key: 'name',
      header: 'Category',
      render: (c) => (
        <div>
          <p className="font-medium text-fg">{c.name}</p>
          {c.description && <p className="max-w-md text-xs text-fg-subtle">{c.description}</p>}
        </div>
      ),
    },
    {
      key: 'sla',
      header: 'Base SLA',
      render: (c) => <span>{c.defaultSlaHours} h</span>,
    },
    { key: 'tickets', header: 'Tickets', render: (c) => c.ticketCount },
    {
      key: 'active',
      header: 'Status',
      render: (c) => <span className={c.isActive ? 'text-ok' : 'text-fg-subtle'}>{c.isActive ? 'Active' : 'Inactive'}</span>,
    },
    {
      key: 'actions',
      header: '',
      render: (c) => (
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onClick={() => setEditing(c)}>Edit</Button>
          <Button variant="ghost" onClick={() => setDeleting(c)}>Delete</Button>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-4">
      <header className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-fg">Ticket categories</h1>
          <p className="mt-1 text-sm text-fg-subtle">
            The base SLA window here is combined with the ticket's priority to compute its deadline.
          </p>
        </div>
        <Button onClick={() => setEditing('new')}>New category</Button>
      </header>

      <AsyncState {...categories} onRetry={categories.refetch} isEmpty={(c) => c.length === 0}
                  emptyTitle="No categories yet">
        {(rows) => (
          <DataTable columns={columns} rows={rows} rowKey={(c) => c.id}
                     loading={categories.loading} caption="Ticket categories" />
        )}
      </AsyncState>

      <CategoryModal category={editing} onClose={() => setEditing(null)}
                     onSaved={() => { setEditing(null); categories.refetch(); toast.success('Category saved.'); }} />

      <ConfirmDialog
        open={deleting !== null}
        title="Delete this category?"
        message={
          deleting && deleting.ticketCount > 0
            ? `${deleting.name} still has ${deleting.ticketCount} ticket(s). The API will refuse this — deactivate it instead.`
            : 'This cannot be undone.'
        }
        confirmLabel="Delete" destructive busy={busy}
        onConfirm={remove} onCancel={() => setDeleting(null)}
      />
    </div>
  );
}

function CategoryModal({ category, onClose, onSaved }: {
  category: Category | 'new' | null; onClose: () => void; onSaved: () => void;
}) {
  const isNew = category === 'new';
  const existing = category !== 'new' && category !== null ? category : null;

  const [form, setForm] = useState({ name: '', description: '', defaultSlaHours: 24, isActive: true });
  const [key, setKey] = useState<number | 'new' | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const currentKey = isNew ? 'new' : existing?.id ?? null;
  if (currentKey !== key) {
    setKey(currentKey);
    setForm({
      name: existing?.name ?? '',
      description: existing?.description ?? '',
      defaultSlaHours: existing?.defaultSlaHours ?? 24,
      isActive: existing?.isActive ?? true,
    });
    setError(null);
  }

  const submit = async () => {
    if (form.name.trim().length < 2) { setError('Enter a category name.'); return; }
    if (form.defaultSlaHours < 1 || form.defaultSlaHours > 720) { setError('The SLA must be between 1 and 720 hours.'); return; }

    setBusy(true); setError(null);
    const payload = {
      name: form.name.trim(),
      description: form.description.trim() || undefined,
      defaultSlaHours: Number(form.defaultSlaHours),
      isActive: form.isActive,
    };

    try {
      if (isNew) await adminApi.createCategory(payload);
      else if (existing) await adminApi.updateCategory(existing.id, payload);
      onSaved();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <Modal open={category !== null} title={isNew ? 'New category' : 'Edit category'} onClose={onClose}
           footer={<>
             <Button variant="secondary" onClick={onClose} disabled={busy}>Cancel</Button>
             <Button onClick={submit} disabled={busy}>{busy ? 'Saving…' : 'Save'}</Button>
           </>}>
      <div className="space-y-4">
        {error && <p role="alert" className="rounded bg-danger-soft px-3 py-2 text-sm text-danger">{error}</p>}

        <TextInput id="c-name" label="Name" required value={form.name}
                   onChange={(e) => setForm({ ...form, name: e.target.value })} />
        <TextInput id="c-description" label="Description" value={form.description}
                   onChange={(e) => setForm({ ...form, description: e.target.value })} />
        <TextInput id="c-sla" label="Base SLA hours" type="number" min={1} max={720} required
                   value={form.defaultSlaHours}
                   onChange={(e) => setForm({ ...form, defaultSlaHours: Number(e.target.value) })}
                   hint="Critical tickets get a quarter of this; Low priority gets double." />

        <label className="flex items-center gap-2 text-sm text-fg">
          <input type="checkbox" checked={form.isActive}
                 onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
                 className="rounded border-line" />
          Active — inactive categories cannot be chosen on a new ticket
        </label>
      </div>
    </Modal>
  );
}

/** The system-wide audit trail. */
export function AuditTrail() {
  const [entityType, setEntityType] = useState('');
  const [page, setPage] = useState(1);
  const [expanded, setExpanded] = useState<number | null>(null);

  const logs = useApiResource(
    () => adminApi.auditLogs({ entityType: entityType || undefined, page, pageSize: 20 }),
    [entityType, page],
  );

  const columns: Column<AuditLog>[] = [
    { key: 'when', header: 'When', render: (a) => <span className="text-fg-subtle">{formatDateTime(a.createdAt)}</span> },
    {
      key: 'actor',
      header: 'Actor',
      render: (a) => (
        <div>
          <p className="text-fg">{a.actorName ?? 'System'}</p>
          {/* Agent means an approved AI action; System means the platform acted on its own. */}
          <span className={`text-xs ${a.actorType === 'Agent' ? 'text-indigo-600' : 'text-fg-subtle'}`}>{a.actorType}</span>
        </div>
      ),
    },
    { key: 'action', header: 'Action', render: (a) => <code className="text-xs text-fg">{a.action}</code> },
    { key: 'entity', header: 'Entity', render: (a) => <span className="text-fg-muted">{a.entityType} #{a.entityId}</span> },
    {
      key: 'details',
      header: '',
      render: (a) =>
        a.detailsJson ? (
          <Button variant="ghost" onClick={() => setExpanded(expanded === a.id ? null : a.id)}>
            {expanded === a.id ? 'Hide' : 'Details'}
          </Button>
        ) : null,
    },
  ];

  return (
    <div className="space-y-4">
      <header>
        <h1 className="text-xl font-semibold text-fg">Audit trail</h1>
        <p className="mt-1 text-sm text-fg-subtle">
          Every ticket change, agent step, approval decision and administrative action.
        </p>
      </header>

      <div className="rounded-2xl border border-line bg-surface p-4 shadow-card">
        <label htmlFor="audit-entity" className="mb-1 block text-xs font-medium text-fg-muted">Entity type</label>
        <select id="audit-entity" value={entityType} onChange={(e) => { setEntityType(e.target.value); setPage(1); }}
                className="rounded-xl border border-line bg-surface px-3 py-2 text-sm text-fg transition focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/25">
          <option value="">All entities</option>
          {['Ticket', 'AgentWorkflow', 'AgentStep', 'AiApproval', 'User', 'TicketCategory', 'KnowledgeArticle'].map((e) => (
            <option key={e} value={e}>{e}</option>
          ))}
        </select>
      </div>

      <AsyncState {...logs} onRetry={logs.refetch} isEmpty={(p) => p.items.length === 0}
                  emptyTitle="No audit entries match this filter">
        {(result) => (
          <>
            <DataTable columns={columns} rows={result.items} rowKey={(a) => a.id}
                       loading={logs.loading} caption="Audit trail" />

            {expanded !== null && (
              <Card title="Audit detail">
                <pre className="max-h-72 overflow-auto rounded bg-surface-2 p-3 text-xs text-fg">
                  {prettyJson(result.items.find((a) => a.id === expanded)?.detailsJson)}
                </pre>
              </Card>
            )}

            <Pagination page={result.page} pageSize={result.pageSize} totalCount={result.totalCount}
                        totalPages={result.totalPages} onPageChange={setPage} />
          </>
        )}
      </AsyncState>
    </div>
  );
}
