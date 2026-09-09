import { Link } from 'react-router-dom';
import { buttonClasses } from '../../components/Ui';
import { Section, SectionHeading, Eyebrow, SLAB } from '../../components/marketing/Section';
import { AgentFlow } from '../../components/marketing/AgentFlow';
import { AppPreview } from '../../components/marketing/AppPreview';
import { ContainerScroll } from "@/components/ui/container-scroll-animation";
import { Arrow } from '../../components/marketing/PublicNav';
import { usePageMeta } from '../../hooks/usePageMeta';
import { routes } from '../../routes';

export function Landing() {
  usePageMeta(
    'SmartDesk AI | AI-Powered IT Support Management',
    'Manage support requests, automate intelligent triage and keep humans in control of high-impact decisions with collaborative AI agents.',
  );

  return (
    <>
      <Hero />
      <TrustStrip />
      <Overview />
      <Features />
      <Agents />
      <HumanApproval />
      <HowItWorks />
      <Platform />
      <Security />
      <ProductShowcase />
      <CallToAction />
    </>
  );
}

/* ------------------------------------------------------------------- Hero */

function Hero() {
  return (
    <section className="px-2 pt-2 sm:px-4">
      <div className={`relative flex flex-col overflow-hidden bg-bg-subtle sd-glow ${SLAB}`}>
        <ContainerScroll
          titleComponent={
            <div className="mx-auto max-w-3xl">
              <span className="inline-flex items-center gap-2 rounded-full border border-line bg-surface px-3 py-1 text-xs font-medium text-fg-muted">
                <span className="h-1.5 w-1.5 rounded-full bg-accent" aria-hidden="true" />
                Five specialised agents, one governed workflow
              </span>

              <h1 className="mt-6 text-4xl font-semibold leading-[1.08] tracking-tight text-fg sm:text-5xl lg:text-[3.5rem]">
                AI-powered IT support,
                <br />
                built for <span className="text-accent">faster resolution</span>.
              </h1>

              <p className="mx-auto mt-6 max-w-xl text-lg leading-relaxed text-fg-muted">
                SmartDesk AI helps teams manage support requests, automate intelligent triage, and
                make better support decisions with collaborative AI agents — while your people stay
                in control of every high-impact action.
              </p>

              <div className="mt-9 flex flex-wrap justify-center gap-3">
                <Link to={routes.signUp} className={buttonClasses('primary', 'lg')}>
                  Get started <Arrow />
                </Link>
                <Link to={routes.howItWorks} className={buttonClasses('secondary', 'lg')}>
                  Explore the platform
                </Link>
              </div>

              <p className="mt-6 text-sm text-fg-subtle">
                Role-based access, backend-enforced approvals and a complete audit trail — built in.
              </p>
            </div>
          }
        >
          {/*
            The live workspace, not a screenshot: it renders with the real components and the real
            design tokens, so it themes correctly and can never go stale.

            To use a static image instead, drop the file in `frontend/public/` and swap this block
            for:
              <img src="/hero.png" alt="The SmartDesk AI workspace"
                   width={1400} height={720} draggable={false}
                   className="mx-auto h-full rounded-2xl object-cover object-left-top" />
          */}
          <HeroWorkspace />
        </ContainerScroll>
      </div>
    </section>
  );
}

/** The workspace as it appears inside the scroll container: sidebar rail plus the live preview. */
function HeroWorkspace() {
  return (
    <div className="flex h-full overflow-hidden rounded-2xl bg-bg-subtle text-left">
      <aside className="hidden w-48 shrink-0 flex-col border-r border-line bg-surface p-3 lg:flex">
        <div className="flex items-center gap-2 px-2 py-2">
          <img src="/logo-mark.png" alt="" aria-hidden="true" className="h-5 w-5 object-contain" />
          <span className="text-sm font-semibold text-fg">
            SmartDesk<span className="text-accent"> AI</span>
          </span>
        </div>
        <nav className="mt-4 space-y-0.5" aria-hidden="true">
          {[
            ['Dashboard', false], ['Tickets', false], ['Knowledge base', false],
            ['Assignments', false], ['SLA', false], ['AI workflows', true],
            ['Approval centre', false], ['Audit trail', false],
          ].map(([label, active]) => (
            <div
              key={label as string}
              className={`rounded-lg px-3 py-1.5 text-xs font-medium ${
                active ? 'bg-accent-soft text-accent-text' : 'text-fg-subtle'
              }`}
            >
              {label as string}
            </div>
          ))}
        </nav>
      </aside>

      <div className="min-w-0 flex-1 overflow-y-auto p-3 sm:p-4">
        <AppPreview initialTab="AI workflow" />
      </div>
    </div>
  );
}

/* ------------------------------------------------------------ Trust strip */

/* Conceptual statements about how the product is built - never invented customers,
   logos, statistics or certifications. */
const principles = [
  'Secure by design',
  'AI-assisted workflows',
  'Centralised support operations',
  'Human oversight by default',
];

function TrustStrip() {
  return (
    <section className="px-2 pt-3 sm:px-4">
      <div className={`bg-bg-subtle px-4 py-10 sm:px-6 lg:px-8 ${SLAB}`}>
       <div className="mx-auto max-w-7xl">
        <p className="text-center text-xs font-semibold uppercase tracking-[0.14em] text-fg-subtle">
          Built for modern support teams
        </p>
        <ul className="mt-6 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          {principles.map((item) => (
            <li key={item} className="flex items-center justify-center gap-2 rounded-2xl border border-line bg-surface px-4 py-3 text-sm font-medium text-fg-muted">
              <CheckDot />
              {item}
            </li>
          ))}
        </ul>
       </div>
      </div>
    </section>
  );
}

/* --------------------------------------------------------------- Overview */

const overviewItems = [
  'Ticket management', 'AI triage', 'Knowledge discovery', 'Intelligent assignment',
  'SLA monitoring', 'Human approval', 'Full auditability',
];

function Overview() {
  return (
    <Section>
      <div className="grid gap-12 lg:grid-cols-2 lg:items-center lg:gap-16">
        <SectionHeading
          eyebrow="The platform"
          title={<>Smarter support operations.<br />Human decisions, AI-powered workflows.</>}
          description={
            <>
              Most help desks lose time before anyone starts fixing anything — requests arrive as
              unstructured text, get mis-categorised, sit unassigned and quietly breach their SLA.
              SmartDesk AI does that triage work automatically, and then stops and asks a person
              before it changes anything that matters.
            </>
          }
        />
        <ul className="grid gap-2.5 sm:grid-cols-2">
          {overviewItems.map((item) => (
            <li key={item} className="flex items-center gap-2.5 rounded-2xl border border-line bg-surface px-4 py-3.5 text-sm font-medium text-fg">
              <CheckDot />
              {item}
            </li>
          ))}
        </ul>
      </div>
    </Section>
  );
}

/* --------------------------------------------------------------- Features */

const features = [
  {
    title: 'AI ticket triage',
    body: 'Analyse each incoming request and identify its category, priority, urgency and the entities involved — before anyone reads it.',
  },
  {
    title: 'Intelligent assignment',
    body: 'Recommend the right support agent using their recorded skills for the category and their current open workload.',
  },
  {
    title: 'Knowledge intelligence',
    body: 'Search your published knowledge base for the articles that actually match the request, and attach them with concrete next steps.',
  },
  {
    title: 'SLA monitoring',
    body: 'Track every open ticket against a deadline computed from its category and priority, and surface the ones approaching a breach.',
  },
  {
    title: 'Human-in-the-loop',
    body: 'High-impact actions pause for a manager. Approve, reject or send back for revision — nothing executes on its own.',
  },
  {
    title: 'Complete audit trail',
    body: 'Every agent step, tool call, validation result and approval decision is recorded with its timing and outcome.',
  },
];

function Features() {
  return (
    <Section tone="subtle" id="features">
      <SectionHeading
        eyebrow="Capabilities"
        title="Everything the desk needs, working together"
        description="Each capability is useful on its own. The value comes from them sharing one ticket, one set of business rules and one audit trail."
      />
      <div className="mt-14 grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {features.map((feature) => (
          <article key={feature.title} className="rounded-2xl border border-line bg-surface p-6 shadow-card transition hover:border-line-strong">
            <h3 className="text-base font-semibold text-fg">{feature.title}</h3>
            <p className="mt-2.5 text-sm leading-relaxed text-fg-muted">{feature.body}</p>
          </article>
        ))}
      </div>
    </Section>
  );
}

/* ----------------------------------------------------------------- Agents */

const agents = [
  { name: 'Planner agent', body: 'Turns the objective into an ordered plan and delegates each step to the right specialist. Holds no tools of its own — planning needs no system access.' },
  { name: 'Triage agent', body: 'Reads the request and returns a structured classification: category, priority, urgency score and the entities it found.' },
  { name: 'Knowledge & assignment agent', body: 'Searches published articles for a real match, then ranks support agents by skill and current workload to recommend an owner.' },
  { name: 'Validation & escalation agent', body: 'The safety gate. Checks the other agents against business rules and the SLA clock, and decides whether escalation is warranted.' },
];

function Agents() {
  return (
    <Section id="agents">
      <SectionHeading
        eyebrow="Agentic AI"
        title={<>Four specialised agents.<br />One intelligent support workflow.</>}
        description="Each agent has a single responsibility, its own input and output contract, and its own narrow list of permitted tools. None of them can change a record directly."
      />

      <div className="mt-14 grid gap-4 md:grid-cols-2">
        {agents.map((agent, index) => (
          <article key={agent.name} className="rounded-2xl border border-line bg-surface p-6 shadow-card">
            <div className="flex items-center gap-3">
              <span className="grid h-8 w-8 place-items-center rounded-lg bg-accent-soft text-sm font-semibold text-accent-text">
                {index + 1}
              </span>
              <h3 className="text-base font-semibold text-fg">{agent.name}</h3>
            </div>
            <p className="mt-3 text-sm leading-relaxed text-fg-muted">{agent.body}</p>
          </article>
        ))}
      </div>

      <div className="mt-12 rounded-2xl border border-line bg-surface p-6 shadow-card sm:p-8">
        <h3 className="text-sm font-semibold text-fg">How a request moves through the system</h3>
        <p className="mt-1.5 text-sm text-fg-subtle">
          A coordinator runs the plan. Deterministic business rules — not the model — decide what may be applied.
        </p>
        <div className="mt-7">
          <AgentFlow />
        </div>
      </div>
    </Section>
  );
}

/* --------------------------------------------------------- Human approval */

const approvalCapabilities = [
  { title: 'Analyse', body: 'Read the request and the live state of the desk.' },
  { title: 'Recommend', body: 'Produce a structured, explainable proposal.' },
  { title: 'Validate', body: 'Check it against business rules and the SLA.' },
  { title: 'Request approval', body: 'Pause and hand the decision to a manager.' },
  { title: 'Execute', body: 'Apply an approved action in a single transaction.' },
  { title: 'Record', body: 'Write the decision and its outcome to the audit trail.' },
];

function HumanApproval() {
  return (
    <Section tone="subtle" id="approvals">
      <div className="grid gap-12 lg:grid-cols-2 lg:items-center lg:gap-16">
        <div>
          <SectionHeading
            eyebrow="Human in the loop"
            title={<>AI recommends.<br />Your team decides.</>}
            description="SmartDesk never lets a high-impact action execute on its own. Escalating a ticket or changing who owns it pauses the workflow until an authorised person makes the call — and that rule is enforced in the backend, not just hidden in the interface."
          />
          <dl className="mt-9 grid gap-x-6 gap-y-4 sm:grid-cols-2">
            {approvalCapabilities.map((item) => (
              <div key={item.title}>
                <dt className="flex items-center gap-2 text-sm font-semibold text-fg">
                  <CheckDot />
                  {item.title}
                </dt>
                <dd className="mt-1 pl-6 text-sm text-fg-subtle">{item.body}</dd>
              </div>
            ))}
          </dl>
        </div>

        <AppPreview initialTab="Approvals" />
      </div>
    </Section>
  );
}

/* ----------------------------------------------------------- How it works */

const steps = [
  { n: '01', title: 'Create a request', body: 'Someone raises a ticket from the web app or the mobile app. It is stored immediately — a slow or failing model can never block it.' },
  { n: '02', title: 'AI analyses the issue', body: 'The triage agent classifies the request and extracts the details that matter.' },
  { n: '03', title: 'Agents collaborate', body: 'Knowledge, assignment and validation agents build on each other through a shared, persisted workflow state.' },
  { n: '04', title: 'A human reviews', body: 'Anything high-impact waits in the approval centre for a manager to approve, reject or send back.' },
  { n: '05', title: 'Execute and record', body: 'The approved action is applied in one transaction, then written to the ticket history and the audit trail.' },
];

function HowItWorks() {
  return (
    <Section id="how-it-works">
      <SectionHeading
        eyebrow="How it works"
        title="From request to resolution, with oversight at the right moment"
        align="center"
      />
      <ol className="mt-14 grid gap-6 sm:grid-cols-2 lg:grid-cols-5 lg:gap-4">
        {steps.map((step) => (
          <li key={step.n} className="relative">
            <p className="text-3xl font-semibold tabular-nums text-accent/35">{step.n}</p>
            <h3 className="mt-2 text-base font-semibold text-fg">{step.title}</h3>
            <p className="mt-2 text-sm leading-relaxed text-fg-muted">{step.body}</p>
          </li>
        ))}
      </ol>
    </Section>
  );
}

/* --------------------------------------------------------------- Platform */

const capabilities = [
  { title: 'Ticket management', body: 'Full lifecycle with a validated status machine, comments and field-level history.' },
  { title: 'Assignment', body: 'Skill and workload scoring, reassignment, and a complete assignment trail.' },
  { title: 'Knowledge base', body: 'Categorised articles with tags, search, and per-ticket relevance ranking.' },
  { title: 'SLA monitoring', body: 'Deadlines derived from category and priority, with at-risk and breach reporting.' },
  { title: 'AI workflows', body: 'Persisted state for every run: plan, steps, tool calls, timings and retries.' },
  { title: 'Approvals', body: 'A dedicated review queue with approve, reject and request-revision decisions.' },
  { title: 'Reporting', body: 'Role-aware dashboards, SLA breakdowns and support agent performance.' },
  { title: 'Audit logs', body: 'A filterable trail distinguishing user, system and approved-agent actions.' },
];

function Platform() {
  return (
    <Section tone="subtle">
      <SectionHeading
        eyebrow="One platform"
        title={<>Everything your support team needs.<br />One connected platform.</>}
        align="center"
      />
      <div className="mt-14 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {capabilities.map((item) => (
          <article key={item.title} className="rounded-2xl border border-line bg-surface p-5 shadow-card">
            <h3 className="text-sm font-semibold text-fg">{item.title}</h3>
            <p className="mt-2 text-sm leading-relaxed text-fg-subtle">{item.body}</p>
          </article>
        ))}
      </div>
    </Section>
  );
}

/* --------------------------------------------------------------- Security */

const controls = [
  { title: 'Role-based access', body: 'Four roles with distinct permissions, checked on every request.' },
  { title: 'JWT authentication', body: 'Signed tokens with issuer, audience, lifetime and signature all validated.' },
  { title: 'Backend authorization', body: 'Resource ownership is enforced in the service layer, not only in the interface.' },
  { title: 'Controlled AI tools', body: 'Agents reach a short allow-list of narrow tools. No SQL, no arbitrary requests.' },
  { title: 'Structured AI output', body: 'Responses must parse into a declared contract; unknown fields are rejected.' },
  { title: 'Backend validation', body: 'Deterministic business rules re-check every identifier against the live database.' },
  { title: 'Human approval', body: 'High-impact actions require an authorised decision before anything is executed.' },
  { title: 'Audit logging', body: 'Every workflow, decision and administrative action is recorded.' },
  { title: 'Secret protection', body: 'Keys live in environment configuration; they are never logged or persisted.' },
  { title: 'Prompt-injection safeguards', body: 'Request text is treated as untrusted data and can never widen an agent’s permissions.' },
];

function Security() {
  return (
    <Section id="security">
      <SectionHeading
        eyebrow="Security"
        title="AI assistance with control built in"
        description="Every control listed here is implemented in the product. We make no compliance-certification claims."
      />
      <div className="mt-14 grid gap-x-8 gap-y-6 sm:grid-cols-2 lg:grid-cols-3">
        {controls.map((control) => (
          <div key={control.title} className="border-l-2 border-line pl-4">
            <h3 className="text-sm font-semibold text-fg">{control.title}</h3>
            <p className="mt-1.5 text-sm leading-relaxed text-fg-subtle">{control.body}</p>
          </div>
        ))}
      </div>
    </Section>
  );
}

/* -------------------------------------------------------- Product preview */

function ProductShowcase() {
  return (
    <Section tone="subtle">
      <SectionHeading
        eyebrow="Product"
        title="See the workspace"
        description="The dashboard, ticket queue, agent workflow timeline and approval centre — the actual interface your team works in."
        align="center"
      />
      <div className="mt-14">
        <AppPreview initialTab="Dashboard" />
      </div>
    </Section>
  );
}

/* -------------------------------------------------------------------- CTA */

function CallToAction() {
  return (
    <section className="px-2 pb-3 sm:px-4">
      <div className={`bg-bg-subtle sd-glow ${SLAB}`}>
       <div className="mx-auto max-w-4xl px-4 py-24 text-center sm:px-6 lg:py-32">
        <h2 className="text-3xl font-semibold leading-tight tracking-tight text-fg sm:text-4xl lg:text-5xl">
          Ready to make support smarter?
        </h2>
        <p className="mx-auto mt-6 max-w-2xl text-lg leading-relaxed text-fg-muted">
          Bring AI-powered workflows and human oversight together in one support platform.
        </p>
        <div className="mt-10 flex flex-wrap justify-center gap-3">
          <Link to={routes.signUp} className={buttonClasses('primary', 'lg')}>
            Get started <Arrow />
          </Link>
          <Link to={routes.contact} className={buttonClasses('secondary', 'lg')}>
            Contact us
          </Link>
        </div>
       </div>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ Shared */

export function CheckDot() {
  return (
    <span className="grid h-4 w-4 shrink-0 place-items-center rounded-full bg-accent-soft" aria-hidden="true">
      <svg width="9" height="9" viewBox="0 0 24 24" fill="none" stroke="currentColor"
           strokeWidth="3.5" strokeLinecap="round" strokeLinejoin="round" className="text-accent">
        <path d="M20 6 9 17l-5-5" />
      </svg>
    </span>
  );
}

export { Section, SectionHeading, Eyebrow };
