import { useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { adminApi, knowledgeApi, type ArticleQuery } from '../api/endpoints';
import { useApiResource, useDebounced } from '../hooks/useApiResource';
import { errorMessage } from '../api/client';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { AsyncState, Button, Card } from '../components/Ui';
import { Pagination } from '../components/Pagination';
import { ConfirmDialog } from '../components/Modal';
import { Select, TextArea, TextInput } from '../components/Form';
import { formatDateTime } from '../utils/format';

/** Component C: the knowledge base list, with search, category filter, sorting and pagination. */
export function KnowledgeList() {
  const { hasRole } = useAuth();
  const canEdit = hasRole('SupportManager', 'Admin');

  const [query, setQuery] = useState<ArticleQuery>({ page: 1, pageSize: 9, sortBy: 'updatedAt', sortDir: 'desc' });
  const [searchInput, setSearchInput] = useState('');
  const search = useDebounced(searchInput);

  const categories = useApiResource(() => adminApi.categories(), []);
  const articles = useApiResource(
    () => knowledgeApi.list({ ...query, search }),
    [search, query.categoryId, query.sortBy, query.sortDir, query.page],
  );

  return (
    <div className="space-y-4">
      <header className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-fg">Knowledge base</h1>
          <p className="mt-1 text-sm text-fg-subtle">
            The same articles the Solution agent searches when it triages a ticket.
          </p>
        </div>
        {canEdit && <Link to="/app/knowledge-base/new"><Button>New article</Button></Link>}
      </header>

      <div className="grid gap-3 rounded-2xl border border-line bg-surface p-4 shadow-card sm:grid-cols-3">
        <div className="sm:col-span-2">
          <label htmlFor="kb-search" className="mb-1 block text-xs font-medium text-fg-muted">Search</label>
          <input id="kb-search" type="search" value={searchInput}
                 onChange={(e) => setSearchInput(e.target.value)}
                 placeholder="Search titles and article text"
                 className="w-full rounded-xl border border-line bg-surface px-3 py-2 text-sm text-fg transition focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/25" />
        </div>
        <div>
          <label htmlFor="kb-category" className="mb-1 block text-xs font-medium text-fg-muted">Category</label>
          <select id="kb-category" value={query.categoryId ?? ''}
                  onChange={(e) => setQuery((q) => ({ ...q, categoryId: e.target.value ? Number(e.target.value) : '', page: 1 }))}
                  className="w-full rounded-xl border border-line bg-surface px-3 py-2 text-sm text-fg transition focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/25">
            <option value="">All categories</option>
            {categories.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </div>
      </div>

      <AsyncState {...articles} onRetry={articles.refetch}
                  isEmpty={(p) => p.items.length === 0}
                  emptyTitle="No articles match your search"
                  emptyHint="Try a different keyword or clear the category filter.">
        {(page) => (
          <>
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
              {page.items.map((a) => (
                <Link key={a.id} to={`/app/knowledge-base/${a.id}`}
                      className="flex flex-col rounded-2xl border border-line bg-surface p-4 shadow-card transition hover:border-accent/45">
                  <div className="flex items-start justify-between gap-2">
                    <h2 className="font-medium text-fg">{a.title}</h2>
                    {!a.isPublished && (
                      <span className="shrink-0 rounded bg-surface-2 px-2 py-0.5 text-xs text-fg-muted">Draft</span>
                    )}
                  </div>
                  <p className="mt-2 flex-1 text-sm text-fg-muted">{a.excerpt}</p>
                  <div className="mt-3 flex flex-wrap items-center gap-2 text-xs text-fg-subtle">
                    <span className="rounded bg-accent-soft px-2 py-0.5 text-accent-text">{a.categoryName}</span>
                    {a.tags.slice(0, 3).map((t) => (
                      <span key={t} className="rounded bg-surface-2 px-2 py-0.5">{t}</span>
                    ))}
                    <span className="ml-auto">{a.viewCount} views</span>
                  </div>
                </Link>
              ))}
            </div>
            <Pagination page={page.page} pageSize={page.pageSize} totalCount={page.totalCount}
                        totalPages={page.totalPages} onPageChange={(p) => setQuery((q) => ({ ...q, page: p }))} />
          </>
        )}
      </AsyncState>
    </div>
  );
}

export function KnowledgeArticle() {
  const { id } = useParams();
  const articleId = Number(id);
  const navigate = useNavigate();
  const toast = useToast();
  const { hasRole } = useAuth();

  const article = useApiResource(() => knowledgeApi.get(articleId), [articleId]);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [busy, setBusy] = useState(false);

  const remove = async () => {
    setBusy(true);
    try {
      await knowledgeApi.remove(articleId);
      toast.success('Article deleted.');
      navigate('/app/knowledge-base');
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setBusy(false); setConfirmDelete(false);
    }
  };

  return (
    <div className="mx-auto max-w-3xl space-y-4">
      <AsyncState {...article} onRetry={article.refetch}>
        {(a) => (
          <>
            <header className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <h1 className="text-xl font-semibold text-fg">{a.title}</h1>
                <p className="mt-1 text-sm text-fg-subtle">
                  {a.categoryName} · updated {formatDateTime(a.updatedAt)} · {a.viewCount} views
                  {a.authorName && ` · by ${a.authorName}`}
                </p>
              </div>
              <div className="flex gap-2">
                <Button variant="secondary" onClick={() => navigate('/app/knowledge-base')}>Back</Button>
                {hasRole('SupportManager', 'Admin') && (
                  <Button variant="secondary" onClick={() => navigate(`/app/knowledge-base/${a.id}/edit`)}>Edit</Button>
                )}
                {hasRole('Admin') && (
                  <Button variant="danger" onClick={() => setConfirmDelete(true)}>Delete</Button>
                )}
              </div>
            </header>

            {a.tags.length > 0 && (
              <div className="flex flex-wrap gap-2">
                {a.tags.map((t) => (
                  <span key={t} className="rounded bg-surface-2 px-2 py-0.5 text-xs text-fg-muted">{t}</span>
                ))}
              </div>
            )}

            <Card>
              <div className="whitespace-pre-wrap text-sm leading-relaxed text-fg">{a.body}</div>
            </Card>
          </>
        )}
      </AsyncState>

      <ConfirmDialog
        open={confirmDelete} title="Delete this article?"
        message="This cannot be undone. Tickets that already link to it will lose the suggestion."
        confirmLabel="Delete" destructive busy={busy}
        onConfirm={remove} onCancel={() => setConfirmDelete(false)}
      />
    </div>
  );
}

export function KnowledgeEditor() {
  const { id } = useParams();
  const isEdit = Boolean(id);
  const navigate = useNavigate();
  const toast = useToast();

  const categories = useApiResource(() => adminApi.categories(), []);
  const existing = useApiResource(
    () => (isEdit ? knowledgeApi.get(Number(id)) : Promise.resolve(null)),
    [id],
  );

  const [form, setForm] = useState({ title: '', body: '', categoryId: '', tags: '', isPublished: true });
  const [loaded, setLoaded] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  // Populate once the existing article arrives, without clobbering later edits.
  if (isEdit && existing.data && !loaded) {
    setForm({
      title: existing.data.title,
      body: existing.data.body,
      categoryId: String(existing.data.categoryId),
      tags: existing.data.tags.join(', '),
      isPublished: existing.data.isPublished,
    });
    setLoaded(true);
  }

  const validate = () => {
    const next: Record<string, string> = {};
    if (form.title.trim().length < 5) next.title = 'The title must be at least 5 characters.';
    if (form.body.trim().length < 20) next.body = 'The article must be at least 20 characters.';
    if (!form.categoryId) next.categoryId = 'Choose a category.';
    setErrors(next);
    return Object.keys(next).length === 0;
  };

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setFormError(null);
    if (!validate()) return;

    const payload = {
      title: form.title.trim(),
      body: form.body.trim(),
      categoryId: Number(form.categoryId),
      tags: form.tags.split(',').map((t) => t.trim()).filter(Boolean).slice(0, 10),
      isPublished: form.isPublished,
    };

    setBusy(true);
    try {
      const saved = isEdit
        ? await knowledgeApi.update(Number(id), payload)
        : await knowledgeApi.create(payload);
      toast.success(isEdit ? 'Article updated.' : 'Article created.');
      navigate(`/app/knowledge-base/${saved.id}`);
    } catch (error) {
      setFormError(errorMessage(error));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="mx-auto max-w-3xl space-y-4">
      <h1 className="text-xl font-semibold text-fg">{isEdit ? 'Edit article' : 'New article'}</h1>

      <Card>
        <form onSubmit={submit} noValidate className="space-y-4">
          {formError && (
            <div role="alert" className="rounded-xl border border-danger/30 bg-danger-soft px-3.5 py-2.5 text-sm text-danger">
              {formError}
            </div>
          )}

          <TextInput id="title" label="Title" required maxLength={200} value={form.title}
                     onChange={(e) => setForm({ ...form, title: e.target.value })} error={errors.title} />

          <Select id="categoryId" label="Category" required value={form.categoryId}
                  onChange={(e) => setForm({ ...form, categoryId: e.target.value })} error={errors.categoryId}>
            <option value="">Choose a category</option>
            {categories.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </Select>

          <TextArea id="body" label="Article" required rows={14} maxLength={20000} value={form.body}
                    onChange={(e) => setForm({ ...form, body: e.target.value })} error={errors.body}
                    hint="Write the steps a support agent should follow." />

          <TextInput id="tags" label="Tags" value={form.tags}
                     onChange={(e) => setForm({ ...form, tags: e.target.value })}
                     hint="Comma separated, up to 10. These improve the agent's keyword matching." />

          <label className="flex items-center gap-2 text-sm text-fg">
            <input type="checkbox" checked={form.isPublished}
                   onChange={(e) => setForm({ ...form, isPublished: e.target.checked })}
                   className="rounded border-line" />
            Published — unpublished drafts are hidden from employees and from the AI search tool
          </label>

          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="secondary" onClick={() => navigate('/app/knowledge-base')} disabled={busy}>Cancel</Button>
            <Button type="submit" disabled={busy}>{busy ? 'Saving…' : 'Save article'}</Button>
          </div>
        </form>
      </Card>
    </div>
  );
}
