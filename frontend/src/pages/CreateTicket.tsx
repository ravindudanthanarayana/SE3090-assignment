import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { adminApi, ticketsApi } from '../api/endpoints';
import { useApiResource } from '../hooks/useApiResource';
import { errorMessage } from '../api/client';
import { useToast } from '../context/ToastContext';
import { Select, TextArea, TextInput } from '../components/Form';
import { Button, Card } from '../components/Ui';
import type { TicketPriority } from '../types';

/**
 * Raising a ticket is what starts the Agentic AI workflow, so the form says so plainly rather
 * than surprising the user when their priority or category changes a few seconds later.
 */
export function CreateTicket() {
  const navigate = useNavigate();
  const toast = useToast();
  const categories = useApiResource(() => adminApi.categories(), []);

  const [form, setForm] = useState({ title: '', description: '', categoryId: '', priority: 'Medium' as TicketPriority });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const validate = () => {
    const next: Record<string, string> = {};
    if (form.title.trim().length < 5) next.title = 'Give the ticket a title of at least 5 characters.';
    if (form.description.trim().length < 10) next.description = 'Describe the problem in at least 10 characters.';
    if (!form.categoryId) next.categoryId = 'Choose a category.';
    setErrors(next);
    return Object.keys(next).length === 0;
  };

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setFormError(null);
    if (!validate()) return;

    setBusy(true);
    try {
      const ticket = await ticketsApi.create({
        title: form.title.trim(),
        description: form.description.trim(),
        categoryId: Number(form.categoryId),
        priority: form.priority,
      });
      toast.success(`${ticket.ticketNumber} raised. The AI workflow is now triaging it.`);
      navigate(`/app/tickets/${ticket.id}`);
    } catch (error) {
      setFormError(errorMessage(error));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="mx-auto max-w-2xl space-y-4">
      <header>
        <h1 className="text-xl font-semibold text-fg">Raise a ticket</h1>
        <p className="mt-1 text-sm text-fg-subtle">
          When you submit this, the agent workflow classifies it, searches the knowledge base and
          recommends an owner. A manager approves anything high-impact before it takes effect.
        </p>
      </header>

      <Card>
        <form onSubmit={submit} noValidate className="space-y-4">
          {formError && (
            <div role="alert" className="rounded-xl border border-danger/30 bg-danger-soft px-3.5 py-2.5 text-sm text-danger">
              {formError}
            </div>
          )}

          <TextInput
            id="title" label="What is the problem?" required maxLength={200}
            value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })}
            error={errors.title} placeholder="e.g. VPN client rejects my login"
          />

          <TextArea
            id="description" label="Describe what is happening" required rows={6} maxLength={5000}
            value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })}
            error={errors.description}
            hint="Include what you tried, any error message, and when it started."
            placeholder="Since changing my password this morning the VPN client refuses my credentials…"
          />

          <div className="grid gap-4 sm:grid-cols-2">
            <Select
              id="categoryId" label="Category" required
              value={form.categoryId} onChange={(e) => setForm({ ...form, categoryId: e.target.value })}
              error={errors.categoryId}
            >
              <option value="">Choose a category</option>
              {categories.data?.filter((c) => c.isActive).map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </Select>

            <Select
              id="priority" label="How urgent is it?"
              value={form.priority}
              onChange={(e) => setForm({ ...form, priority: e.target.value as TicketPriority })}
              hint="Triage may adjust this."
            >
              <option value="Low">Low – a question or minor annoyance</option>
              <option value="Medium">Medium – slowed down but working</option>
              <option value="High">High – I am blocked</option>
              <option value="Critical">Critical – a whole team or service is down</option>
            </Select>
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="secondary" onClick={() => navigate('/app/tickets')} disabled={busy}>
              Cancel
            </Button>
            <Button type="submit" disabled={busy}>{busy ? 'Submitting…' : 'Raise ticket'}</Button>
          </div>
        </form>
      </Card>
    </div>
  );
}
