import { Link } from 'react-router-dom';
import { lazy, Suspense, useState, type FormEvent } from 'react';
import { buttonClasses } from '../../components/Ui';
import { Section, SectionHeading, SLAB } from '../../components/marketing/Section';
import { AgentFlow } from '../../components/marketing/AgentFlow';
import { AppPreview } from '../../components/marketing/AppPreview';
import { motion } from 'motion/react';

// dotted-map embeds the whole world dataset (~154 KB gzipped). Every public page shares this
// module, so importing the map eagerly would make /features and /contact pay for a graphic only
// /about renders. Loading it lazily keeps that cost on the one page that shows it.
const WorldMap = lazy(() => import('@/components/ui/world-map'));
import { Arrow } from '../../components/marketing/PublicNav';
import { CheckDot } from './Landing';
import { TextArea, TextInput } from '../../components/Form';
import { usePageMeta } from '../../hooks/usePageMeta';
import { routes } from '../../routes';

/** Shared page intro for the secondary marketing pages. */
function PageHero({ eyebrow, title, description }: { eyebrow: string; title: string; description: string }) {
  return (
    <section className="px-2 pt-2 sm:px-4">
      <div className={`bg-bg-subtle px-4 pb-20 pt-14 sm:px-6 lg:px-8 lg:pb-24 lg:pt-20 sd-glow ${SLAB}`}>
        <div className="mx-auto max-w-7xl">
         <div className="max-w-3xl">
          <p className="text-xs font-semibold uppercase tracking-[0.14em] text-accent-text">{eyebrow}</p>
          <h1 className="mt-4 text-4xl font-semibold leading-[1.1] tracking-tight text-fg sm:text-5xl">{title}</h1>
          <p className="mt-6 text-lg leading-relaxed text-fg-muted">{description}</p>
         </div>
        </div>
      </div>
    </section>
  );
}

function ClosingCta() {
  return (
    <Section tone="subtle">
      <div className="mx-auto max-w-2xl text-center">
        <h2 className="text-2xl font-semibold tracking-tight text-fg sm:text-3xl">
          Start with your own support desk
        </h2>
        <p className="mt-4 text-fg-muted">
          Create an account and raise your first ticket — the agent workflow runs on it immediately.
        </p>
        <div className="mt-8 flex flex-wrap justify-center gap-3">
          <Link to={routes.signUp} className={buttonClasses('primary', 'lg')}>Get started <Arrow /></Link>
          <Link to={routes.contact} className={buttonClasses('secondary', 'lg')}>Contact us</Link>
        </div>
      </div>
    </Section>
  );
}

/* ------------------------------------------------------------- /features */

const featureGroups = [
  {
    id: 'ticket-management',
    title: 'Ticket management',
    body: 'Every request gets a number, a category, an owner and an SLA deadline. Status moves through a validated machine, so a ticket cannot skip from New to Closed — and every field change is recorded with who made it and when.',
    points: ['Validated status transitions', 'Field-level history', 'Internal notes hidden from requesters', 'Server-side search, filter, sort and pagination'],
  },
  {
    id: 'assignment',
    title: 'Assignment and workload',
    body: 'Support agents have recorded skill levels per category. The scorer weighs that against how much open work each person already has, and shows the arithmetic so a manager can check it rather than trust it.',
    points: ['Skill levels per category', 'Live workload counts', 'Explainable candidate ranking', 'Full reassignment trail'],
  },
  {
    id: 'knowledge',
    title: 'Knowledge base',
    body: 'Articles are categorised and tagged, and ranked against a specific ticket rather than searched blindly. The same ranking backs both the manager-facing view and the agent tool, so they can never disagree.',
    points: ['Category and tag organisation', 'Ticket-aware relevance ranking', 'Draft articles hidden from employees and from AI', 'Articles attached to tickets as suggested solutions'],
  },
  {
    id: 'sla',
    title: 'SLA and escalation',
    body: 'Each category carries a base SLA window, compressed by the ticket’s priority. Anything inside the final quarter of its window is at risk; anything past its deadline is breached — and both are surfaced before someone notices by accident.',
    points: ['Deadlines computed from category and priority', 'At-risk and breach detection', 'Per-category breakdown', 'Approval-gated escalation'],
  },
  {
    id: 'security',
    title: 'Security and auditability',
    body: 'Authorisation is enforced in the service layer, so hiding a button in the interface is never the control. Every workflow, tool call, validation result and approval decision is written to an audit trail that distinguishes a person from the system from an approved agent action.',
    points: ['Role-based access with resource ownership checks', 'BCrypt password hashing', 'Complete audit trail', 'Secrets in environment configuration only'],
  },
];

export function FeaturesPage() {
  usePageMeta('Features | SmartDesk AI', 'Ticket management, intelligent assignment, knowledge ranking, SLA monitoring and a complete audit trail.');

  return (
    <>
      <PageHero
        eyebrow="Product"
        title="Everything the support desk runs on"
        description="SmartDesk AI is one platform with one set of business rules. The AI is a participant in it, not a separate product bolted on the side."
      />
      {featureGroups.map((group, index) => (
        <Section key={group.id} id={group.id} tone={index % 2 === 1 ? 'subtle' : 'default'}>
          <div className="grid gap-10 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)] lg:items-start lg:gap-16">
            <SectionHeading title={group.title} description={group.body} />
            <ul className="grid gap-2.5 self-center">
              {group.points.map((point) => (
                <li key={point} className="flex items-start gap-2.5 rounded-2xl border border-line bg-surface px-4 py-3.5 text-sm text-fg">
                  <span className="mt-0.5"><CheckDot /></span>
                  {point}
                </li>
              ))}
            </ul>
          </div>
        </Section>
      ))}
      <ClosingCta />
    </>
  );
}

/* ------------------------------------------------------------ /solutions */

const solutions = [
  {
    id: 'it-support',
    role: 'IT support teams',
    body: 'Stop triaging by hand. Requests arrive already classified, with relevant knowledge attached and a recommended owner — so the first thing an agent does is fix the problem, not categorise it.',
  },
  {
    id: 'service-ops',
    role: 'Service operations',
    body: 'See the whole desk at once: what is open, what is breaching, who is overloaded and which categories generate the most work.',
  },
  {
    id: 'managers',
    role: 'Support managers',
    body: 'Review AI recommendations in one queue, with the reasoning and the risk level attached. Approve, reject or send back for revision — and see the outcome recorded either way.',
  },
  {
    id: 'enterprise',
    role: 'Enterprise support',
    body: 'Role-based access, backend-enforced authorisation and a complete audit trail, so AI participation in the process is reviewable rather than opaque.',
  },
];

export function SolutionsPage() {
  usePageMeta('Solutions | SmartDesk AI', 'How SmartDesk AI supports IT support teams, service operations, support managers and enterprise support.');

  return (
    <>
      <PageHero
        eyebrow="Solutions"
        title="Built around how support teams actually work"
        description="The same platform, seen from four different desks."
      />
      <Section>
        <div className="grid gap-4 md:grid-cols-2">
          {solutions.map((solution) => (
            <article key={solution.id} id={solution.id} className="rounded-2xl border border-line bg-surface p-7 shadow-card">
              <h2 className="text-lg font-semibold text-fg">{solution.role}</h2>
              <p className="mt-3 text-sm leading-relaxed text-fg-muted">{solution.body}</p>
            </article>
          ))}
        </div>
      </Section>
      <Section tone="subtle">
        <SectionHeading title="The workspace" description="Role-aware navigation means each person sees only what their role can act on." align="center" />
        <div className="mt-12"><AppPreview initialTab="Tickets" /></div>
      </Section>
      <ClosingCta />
    </>
  );
}

/* ------------------------------------------------------------ /ai-agents */

const agentDetail = [
  { name: 'Planner agent', owner: 'Coordination', tools: 'None', body: 'Receives the objective and produces an ordered plan naming which specialists should run. It holds no tools at all — planning requires no system access, and giving it none is the least-privilege choice.' },
  { name: 'Triage agent', owner: 'Domain analysis', tools: 'GetTicket', body: 'Reads the request text and returns a structured classification. It cannot see staffing or workload, because nothing about classification requires them.' },
  { name: 'Solution agent', owner: 'Knowledge retrieval', tools: 'GetTicket, SearchKnowledgeBase', body: 'Searches published articles and proposes concrete troubleshooting steps. Any article id it cites that the search did not actually return is discarded.' },
  { name: 'Assignment agent', owner: 'Action', tools: 'GetSupportAgents, GetAgentWorkload, ScoreAssignmentCandidates', body: 'Recommends an owner from a system-computed candidate ranking. It can only name someone who appeared in that ranking — and it recommends, never assigns.' },
  { name: 'Validation & escalation agent', owner: 'Safety', tools: 'CheckSla', body: 'The gate on the other agents’ work. It uses the system’s own SLA facts rather than estimating them, and its escalation decision is a recommendation that still requires approval.' },
];

export function AiAgentsPage() {
  usePageMeta('AI Agents | SmartDesk AI', 'Five specialised agents with defined contracts, controlled tool permissions and a human approval gate.');

  return (
    <>
      <PageHero
        eyebrow="Agentic AI"
        title="Specialised agents, controlled tools, human oversight"
        description="Not a chatbot. A multi-step workflow where each agent has one job, a declared input and output contract, and a short list of tools it is permitted to call."
      />

      <Section>
        <SectionHeading title="The workflow" description="A deterministic coordinator runs the plan. The model never controls the loop, never chooses permissions and never writes to the database." />
        <div className="mt-12 rounded-2xl border border-line bg-surface p-6 shadow-card sm:p-8">
          <AgentFlow />
        </div>
      </Section>

      <Section tone="subtle">
        <SectionHeading title="The agents" description="Distinctness is structural: different prompt, different contract, different tool allow-list, its own recorded execution step." />
        <div className="mt-12 overflow-hidden rounded-2xl border border-line bg-surface">
          <ul className="divide-y divide-line">
            {agentDetail.map((agent) => (
              <li key={agent.name} className="p-6">
                <div className="flex flex-wrap items-center gap-3">
                  <h3 className="text-base font-semibold text-fg">{agent.name}</h3>
                  <span className="rounded-full bg-accent-soft px-2.5 py-0.5 text-xs font-medium text-accent-text">
                    {agent.owner}
                  </span>
                </div>
                <p className="mt-3 text-sm leading-relaxed text-fg-muted">{agent.body}</p>
                <p className="mt-3 text-xs text-fg-subtle">
                  <span className="font-medium">Permitted tools:</span>{' '}
                  <code className="rounded bg-surface-2 px-1.5 py-0.5">{agent.tools}</code>
                </p>
              </li>
            ))}
          </ul>
        </div>
      </Section>

      <Section id="approvals">
        <div className="grid gap-12 lg:grid-cols-2 lg:items-center lg:gap-16">
          <SectionHeading
            title="Every high-impact action stops here"
            description="Escalating a ticket or transferring ownership pauses the workflow and creates a pending approval. Nothing is applied until an authorised person decides — and that is enforced in the backend, so it holds no matter which client is calling."
          />
          <AppPreview initialTab="Approvals" />
        </div>
      </Section>

      <ClosingCta />
    </>
  );
}

/* --------------------------------------------------------- /how-it-works */

export function HowItWorksPage() {
  usePageMeta('How it works | SmartDesk AI', 'From request to resolution: triage, collaboration, human approval and an auditable outcome.');

  const stages = [
    { n: '01', title: 'A request arrives', body: 'Someone raises a ticket. It is committed to the database first, so a slow or unavailable model can never stop a person reporting a problem.' },
    { n: '02', title: 'The workflow starts', body: 'A workflow record is created and runs in the background. The person who raised the ticket is not left waiting on it.' },
    { n: '03', title: 'Agents collaborate', body: 'The planner delegates; triage classifies; the solution agent retrieves knowledge; the assignment agent ranks owners; the validation agent checks the result.' },
    { n: '04', title: 'Rules decide what is allowed', body: 'Deterministic business rules re-check every identifier against the live database. Safe changes are applied; anything high-impact becomes a pending approval.' },
    { n: '05', title: 'A human decides', body: 'A manager reviews the recommendation and its reasoning, then approves, rejects or requests a revision.' },
    { n: '06', title: 'The outcome is recorded', body: 'An approved action is applied in a single transaction, then written to the ticket history and the audit trail. A failure is recorded just as clearly, with the ticket left untouched.' },
  ];

  return (
    <>
      <PageHero
        eyebrow="How it works"
        title="What happens between a request and a resolution"
        description="Six stages, one persisted workflow, and a person in charge of anything that matters."
      />
      <Section>
        <ol className="grid gap-8 sm:grid-cols-2 lg:grid-cols-3">
          {stages.map((stage) => (
            <li key={stage.n}>
              <p className="text-3xl font-semibold tabular-nums text-accent/35">{stage.n}</p>
              <h2 className="mt-2 text-base font-semibold text-fg">{stage.title}</h2>
              <p className="mt-2 text-sm leading-relaxed text-fg-muted">{stage.body}</p>
            </li>
          ))}
        </ol>
      </Section>

      <Section tone="subtle" id="api">
        <div className="grid gap-12 lg:grid-cols-2 lg:items-center lg:gap-16">
          <SectionHeading
            title="One API behind every client"
            description="The web workspace and the mobile app talk to the same REST API, with the same authentication, the same permissions and the same business rules. There is no separate path that skips a check."
          />
          <div className="rounded-2xl border border-line bg-surface p-6 shadow-card">
            <p className="text-xs font-semibold uppercase tracking-wide text-fg-subtle">Representative endpoints</p>
            <ul className="mt-4 space-y-2.5 font-mono text-xs">
              {[
                ['POST', '/api/auth/login'],
                ['GET', '/api/tickets?search=&status=&page='],
                ['POST', '/api/tickets/{id}/status'],
                ['POST', '/api/ai/workflows'],
                ['GET', '/api/ai/workflows/{id}'],
                ['POST', '/api/ai/approvals/{id}/decision'],
              ].map(([method, path]) => (
                <li key={path} className="flex items-center gap-3">
                  <span className="w-11 shrink-0 rounded bg-accent-soft px-1.5 py-0.5 text-center text-[10px] font-semibold text-accent-text">
                    {method}
                  </span>
                  <code className="truncate text-fg-muted">{path}</code>
                </li>
              ))}
            </ul>
          </div>
        </div>
      </Section>

      <ClosingCta />
    </>
  );
}

/* ---------------------------------------------------------------- /about */

export function AboutPage() {
  usePageMeta('About | SmartDesk AI', 'Why SmartDesk AI exists and the principles behind how it is built.');

  const principles = [
    { title: 'AI recommends, people decide', body: 'Automation should remove the tedious part of the work, not the accountability. Anything with real consequences waits for a person.' },
    { title: 'Rules belong outside the model', body: 'SLA deadlines, assignment scores and permission checks are ordinary code. The model advises; deterministic rules decide.' },
    { title: 'Everything is reviewable', body: 'Every agent step, tool call and approval decision is recorded with its timing and outcome, so AI participation can be audited rather than assumed.' },
    { title: 'Fail safely and say so', body: 'When something goes wrong the workflow records the failure and leaves the ticket untouched, rather than half-applying a change.' },
  ];

  return (
    <>
      <PageHero
        eyebrow="Company"
        title="Support software that keeps people in charge"
        description="SmartDesk AI was built on a simple conviction: AI is genuinely useful for the analysis work in a help desk, and genuinely dangerous when it is allowed to act unsupervised."
      />
      <DistributedDesk />

      <Section>
        <SectionHeading title="What we build on" />
        <div className="mt-12 grid gap-4 md:grid-cols-2">
          {principles.map((principle) => (
            <article key={principle.title} className="rounded-2xl border border-line bg-surface p-7 shadow-card">
              <h2 className="text-base font-semibold text-fg">{principle.title}</h2>
              <p className="mt-3 text-sm leading-relaxed text-fg-muted">{principle.body}</p>
            </article>
          ))}
        </div>
      </Section>
      <ClosingCta />
    </>
  );
}

/**
 * The map section on the Company page.
 *
 * Careful framing: the arcs describe how the product behaves for a distributed team - one queue,
 * one SLA clock, timestamps in UTC - not a claim about offices, customers or global presence,
 * none of which exist. The cities are illustrative of time-zone spread, nothing more.
 */
function DistributedDesk() {
  return (
    <Section tone="subtle">
      <div className="mx-auto max-w-3xl text-center">
        <p className="text-xs font-semibold uppercase tracking-[0.14em] text-accent-text">
          Distributed by default
        </p>
        <h2 className="mt-3 text-3xl font-semibold tracking-tight text-fg sm:text-4xl">
          One desk,{' '}
          <span className="text-fg-subtle">
            {'every time zone'.split('').map((character, idx) => (
              <motion.span
                key={idx}
                className="inline-block whitespace-pre"
                initial={{ x: -10, opacity: 0 }}
                whileInView={{ x: 0, opacity: 1 }}
                viewport={{ once: true }}
                transition={{ duration: 0.5, delay: idx * 0.03 }}
              >
                {character}
              </motion.span>
            ))}
          </span>
        </h2>
        <p className="mx-auto mt-5 max-w-2xl text-lg leading-relaxed text-fg-muted">
          Requests arrive around the clock from wherever your people work. SmartDesk keeps a single
          queue, a single SLA clock and a single audit trail across all of them — every deadline is
          computed in UTC, so an overnight handover never quietly resets someone else's timer.
        </p>
      </div>

      <div className="mt-12">
        <Suspense fallback={<div className="aspect-[2/1] w-full animate-pulse rounded-2xl bg-surface-2" />}>
        <WorldMap
          dots={[
            { start: { lat: 37.7749, lng: -122.4194 }, end: { lat: 40.7128, lng: -74.006 } },
            { start: { lat: 40.7128, lng: -74.006 }, end: { lat: 51.5074, lng: -0.1278 } },
            { start: { lat: 51.5074, lng: -0.1278 }, end: { lat: 6.9271, lng: 79.8612 } },
            { start: { lat: 6.9271, lng: 79.8612 }, end: { lat: 1.3521, lng: 103.8198 } },
            { start: { lat: 1.3521, lng: 103.8198 }, end: { lat: -33.8688, lng: 151.2093 } },
            { start: { lat: 51.5074, lng: -0.1278 }, end: { lat: -1.2921, lng: 36.8219 } },
          ]}
        />
        </Suspense>
      </div>
    </Section>
  );
}

/* -------------------------------------------------------------- /contact */

export function ContactPage() {
  usePageMeta('Contact | SmartDesk AI', 'Get in touch with the SmartDesk AI team.');

  const [sent, setSent] = useState(false);
  const [form, setForm] = useState({ name: '', email: '', message: '' });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const submit = (event: FormEvent) => {
    event.preventDefault();
    const next: Record<string, string> = {};
    if (!form.name.trim()) next.name = 'Enter your name.';
    if (!/^\S+@\S+\.\S+$/.test(form.email)) next.email = 'Enter a valid email address.';
    if (form.message.trim().length < 10) next.message = 'Tell us a little more.';
    setErrors(next);
    if (Object.keys(next).length === 0) setSent(true);
  };

  return (
    <>
      <PageHero
        eyebrow="Contact"
        title="Talk to us"
        description="Questions about the platform, the agent workflow or how the approval gate works? Send a note."
      />
      <Section>
        <div className="grid gap-12 lg:grid-cols-2 lg:gap-16">
          <div>
            <h2 className="text-lg font-semibold text-fg">Already using SmartDesk?</h2>
            <p className="mt-3 text-sm leading-relaxed text-fg-muted">
              Sign in to your workspace to raise a ticket — support requests are handled in the
              product itself, where they get triaged and tracked like any other request.
            </p>
            <Link to={routes.signIn} className={`${buttonClasses('secondary')} mt-5`}>
              Sign in to your workspace
            </Link>
          </div>

          {sent ? (
            <div className="rounded-xl border border-ok/30 bg-ok-soft p-8" role="status">
              <h2 className="text-base font-semibold text-ok">Thanks — your message is ready to send</h2>
              <p className="mt-2 text-sm text-fg-muted">
                This form validates in the browser but is not wired to a mailbox. Support requests
                raised inside the product go through the real API and the full agent workflow.
              </p>
            </div>
          ) : (
            <form onSubmit={submit} noValidate className="rounded-2xl border border-line bg-surface p-7 shadow-card">
              <div className="space-y-4">
                <TextInput id="contact-name" label="Name" required value={form.name}
                           onChange={(e) => setForm({ ...form, name: e.target.value })} error={errors.name} />
                <TextInput id="contact-email" label="Work email" type="email" required value={form.email}
                           onChange={(e) => setForm({ ...form, email: e.target.value })} error={errors.email} />
                <TextArea id="contact-message" label="Message" rows={5} required value={form.message}
                          onChange={(e) => setForm({ ...form, message: e.target.value })} error={errors.message} />
              </div>
              <button type="submit" className={`${buttonClasses('primary')} mt-6 w-full`}>
                Send message <Arrow />
              </button>
            </form>
          )}
        </div>
      </Section>
    </>
  );
}
