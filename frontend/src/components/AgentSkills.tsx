import { useState } from 'react';
import { adminApi, assignmentApi } from '../api/endpoints';
import { errorMessage } from '../api/client';
import { useApiResource } from '../hooks/useApiResource';
import { Button } from './Ui';
import { Modal } from './Modal';
import type { AgentSkill, Category, SupportAgent } from '../types';

/** Proficiency labels for the 1-5 scale the assignment scorer multiplies by its skill weight. */
const SKILL_LEVELS: { value: number; label: string }[] = [
  { value: 1, label: '1 - Novice' },
  { value: 2, label: '2 - Basic' },
  { value: 3, label: '3 - Competent' },
  { value: 4, label: '4 - Advanced' },
  { value: 5, label: '5 - Expert' },
];

export interface SkillDraft {
  categoryId: number;
  proficiencyLevel: number;
}

const selectClasses =
  'w-full rounded-xl border border-line bg-surface px-3 py-2 text-sm text-fg transition ' +
  'focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/25 disabled:opacity-60';

function useActiveCategories() {
  const categories = useApiResource(() => adminApi.categories(), []);
  const active = (categories.data ?? []).filter((c) => c.isActive);
  return { ...categories, active };
}

/** One "category + level + Add" row, offering only the categories the agent does not have yet. */
function AddSkillRow({ available, busy, onAdd }: {
  available: Category[]; busy?: boolean; onAdd: (draft: SkillDraft) => void;
}) {
  const [categoryId, setCategoryId] = useState<number | ''>('');
  const [level, setLevel] = useState(3);

  if (available.length === 0) {
    return <p className="text-xs text-fg-subtle">Every active category already has a skill level.</p>;
  }

  const add = () => {
    if (categoryId === '') return;
    onAdd({ categoryId, proficiencyLevel: level });
    setCategoryId('');
    setLevel(3);
  };

  return (
    <div className="grid grid-cols-[1fr_auto] gap-2 sm:grid-cols-[1fr_9rem_auto]">
      <select aria-label="Category" value={categoryId} disabled={busy} className={selectClasses}
              onChange={(e) => setCategoryId(e.target.value === '' ? '' : Number(e.target.value))}>
        <option value="">Choose a category…</option>
        {available.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
      </select>
      <select aria-label="Proficiency level" value={level} disabled={busy} className={selectClasses}
              onChange={(e) => setLevel(Number(e.target.value))}>
        {SKILL_LEVELS.map((l) => <option key={l.value} value={l.value}>{l.label}</option>)}
      </select>
      <Button type="button" variant="secondary" onClick={add} disabled={busy || categoryId === ''}
              className="col-span-2 sm:col-span-1">
        Add
      </Button>
    </div>
  );
}

/**
 * Skills picked while creating a new support agent. Nothing is saved here - the parent form
 * creates the user first, then saves these through PUT /api/support-agents/{id}/skills.
 */
export function SkillDraftList({ value, onChange, disabled }: {
  value: SkillDraft[]; onChange: (next: SkillDraft[]) => void; disabled?: boolean;
}) {
  const categories = useActiveCategories();
  const nameOf = (id: number) => categories.active.find((c) => c.id === id)?.name ?? `Category ${id}`;
  const available = categories.active.filter((c) => !value.some((s) => s.categoryId === c.id));

  return (
    <fieldset className="space-y-2 rounded-2xl border border-line p-3">
      <legend className="px-1 text-sm font-medium text-fg">Skills</legend>
      <p className="text-xs text-fg-subtle">
        The assignment scorer ranks agents by these levels, so add at least one.
      </p>

      {categories.error && <p role="alert" className="text-xs text-danger">{categories.error}</p>}

      {value.map((skill) => (
        <div key={skill.categoryId} className="grid grid-cols-[1fr_9rem_auto] items-center gap-2">
          <span className="text-sm text-fg">{nameOf(skill.categoryId)}</span>
          <select aria-label={`${nameOf(skill.categoryId)} level`} value={skill.proficiencyLevel}
                  disabled={disabled} className={selectClasses}
                  onChange={(e) => onChange(value.map((s) => s.categoryId === skill.categoryId
                    ? { ...s, proficiencyLevel: Number(e.target.value) } : s))}>
            {SKILL_LEVELS.map((l) => <option key={l.value} value={l.value}>{l.label}</option>)}
          </select>
          <Button type="button" variant="ghost" size="sm" disabled={disabled}
                  aria-label={`Remove ${nameOf(skill.categoryId)}`}
                  onClick={() => onChange(value.filter((s) => s.categoryId !== skill.categoryId))}>
            ✕
          </Button>
        </div>
      ))}

      {!categories.loading && (
        <AddSkillRow available={available} busy={disabled} onAdd={(draft) => onChange([...value, draft])} />
      )}
    </fieldset>
  );
}

/**
 * Edits an existing agent's skills. Every change is saved immediately: PUT upserts one
 * category's level, DELETE removes it. Admin only - the API returns 403 for anyone else.
 */
export function SkillEditorModal({ agent, onClose, onChanged }: {
  agent: SupportAgent | null; onClose: () => void; onChanged: () => void;
}) {
  const categories = useActiveCategories();
  const [skills, setSkills] = useState<AgentSkill[]>([]);
  const [agentId, setAgentId] = useState<number | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Load the agent's skills whenever a different agent is opened.
  const currentId = agent?.userId ?? null;
  if (currentId !== agentId) {
    setAgentId(currentId);
    setSkills(agent?.skills ?? []);
    setError(null);
  }

  if (!agent) return null;

  const available = categories.active.filter((c) => !skills.some((s) => s.categoryId === c.id));

  const run = async (action: () => Promise<void>) => {
    setBusy(true);
    setError(null);
    try {
      await action();
      onChanged();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  const save = (draft: SkillDraft) => run(async () => {
    const saved = await assignmentApi.upsertSkill(agent.userId, draft);
    setSkills((current) => {
      const others = current.filter((s) => s.categoryId !== saved.categoryId);
      return [...others, saved].sort((a, b) => a.categoryName.localeCompare(b.categoryName));
    });
  });

  const remove = (skill: AgentSkill) => run(async () => {
    await assignmentApi.deleteSkill(agent.userId, skill.id);
    setSkills((current) => current.filter((s) => s.id !== skill.id));
  });

  return (
    <Modal open title={`Skills: ${agent.fullName}`} onClose={onClose}
           footer={<Button variant="secondary" onClick={onClose} disabled={busy}>Done</Button>}>
      <div className="space-y-4">
        <p className="text-sm text-fg-muted">
          Changes save immediately and feed the next assignment recommendation
          (score = skill × 10 − open tickets × 3).
        </p>

        {error && <p role="alert" className="rounded bg-danger-soft px-3 py-2 text-sm text-danger">{error}</p>}
        {categories.error && <p role="alert" className="text-xs text-danger">{categories.error}</p>}

        {skills.length === 0 ? (
          <p className="text-sm text-fg-subtle">No skills recorded yet.</p>
        ) : (
          <ul className="space-y-2">
            {skills.map((skill) => (
              <li key={skill.id} className="grid grid-cols-[1fr_9rem_auto] items-center gap-2">
                <span className="text-sm text-fg">{skill.categoryName}</span>
                <select aria-label={`${skill.categoryName} level`} value={skill.proficiencyLevel}
                        disabled={busy} className={selectClasses}
                        onChange={(e) => save({ categoryId: skill.categoryId, proficiencyLevel: Number(e.target.value) })}>
                  {SKILL_LEVELS.map((l) => <option key={l.value} value={l.value}>{l.label}</option>)}
                </select>
                <Button type="button" variant="ghost" size="sm" disabled={busy}
                        aria-label={`Remove ${skill.categoryName}`} onClick={() => remove(skill)}>
                  ✕
                </Button>
              </li>
            ))}
          </ul>
        )}

        <div className="border-t border-line pt-4">
          <p className="mb-2 text-sm font-medium text-fg">Add a skill</p>
          {!categories.loading && <AddSkillRow available={available} busy={busy} onAdd={save} />}
        </div>
      </div>
    </Modal>
  );
}
