/**
 * The five-stage workflow, as a diagram.
 *
 * A vertical rail on small screens, a horizontal one from `lg` up. Built from divs and CSS
 * rather than an SVG or a diagramming library so it stays themeable, responsive and readable
 * to a screen reader as an ordered list.
 */
const stages = [
  { name: 'Request',       role: 'A support request arrives',              kind: 'input' as const },
  { name: 'Planner',       role: 'Plans and delegates the workflow',        kind: 'agent' as const },
  { name: 'Triage',        role: 'Classifies category, priority, urgency',  kind: 'agent' as const },
  { name: 'Knowledge & Assignment', role: 'Finds solutions, recommends an owner', kind: 'agent' as const },
  { name: 'Validation',    role: 'Checks rules and SLA risk',               kind: 'agent' as const },
  { name: 'Human approval', role: 'A manager approves, rejects or revises', kind: 'human' as const },
  { name: 'Action',        role: 'Executed, recorded and audited',          kind: 'output' as const },
];

const styles = {
  input:  'border-line bg-surface-2 text-fg-muted',
  agent:  'border-accent/30 bg-accent-soft text-accent-text',
  human:  'border-info/35 bg-info-soft text-info',
  output: 'border-ok/35 bg-ok-soft text-ok',
};

export function AgentFlow() {
  return (
    <ol className="relative flex flex-col gap-3 lg:flex-row lg:items-stretch lg:gap-2">
      {stages.map((stage, index) => (
        <li key={stage.name} className="flex items-start gap-3 lg:min-w-0 lg:flex-1 lg:flex-col lg:items-stretch lg:gap-0">
          {/* Connector: a vertical rail on mobile, a horizontal one on desktop. */}
          <div className="flex flex-col items-center lg:hidden" aria-hidden="true">
            <span className={`grid h-8 w-8 shrink-0 place-items-center rounded-full border text-[11px] font-semibold ${styles[stage.kind]}`}>
              {index + 1}
            </span>
            {index < stages.length - 1 && <span className="mt-1 h-full w-px flex-1 bg-line" />}
          </div>

          <div className="hidden items-center lg:flex" aria-hidden="true">
            <span className={`grid h-7 w-7 shrink-0 place-items-center rounded-full border text-[11px] font-semibold ${styles[stage.kind]}`}>
              {index + 1}
            </span>
            {index < stages.length - 1 && <span className="h-px w-full bg-line" />}
          </div>

          <div className={`min-w-0 flex-1 rounded-xl border px-3.5 py-3 lg:mr-2 lg:mt-3 ${styles[stage.kind]}`}>
            <p className="text-sm font-semibold leading-tight">{stage.name}</p>
            <p className="mt-1 text-xs leading-snug opacity-80">{stage.role}</p>
          </div>
        </li>
      ))}
    </ol>
  );
}
