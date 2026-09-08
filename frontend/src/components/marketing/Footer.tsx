import { Link } from 'react-router-dom';
import { Logo } from '../brand/Logo';
import { routes } from '../../routes';

/**
 * Footer link groups.
 *
 * Every entry points at a route that genuinely exists. Where a page does not exist yet, the
 * link goes to the closest real destination rather than a dead `#` - a footer full of links
 * that go nowhere is the fastest way to make a site feel fake.
 */
const groups = [
  {
    title: 'Product',
    links: [
      { label: 'Features', to: routes.features },
      { label: 'AI agents', to: routes.aiAgents },
      { label: 'Ticket management', to: `${routes.features}#ticket-management` },
      { label: 'Knowledge base', to: `${routes.features}#knowledge` },
      { label: 'SLA monitoring', to: `${routes.features}#sla` },
      { label: 'Approvals', to: `${routes.aiAgents}#approvals` },
    ],
  },
  {
    title: 'Solutions',
    links: [
      { label: 'IT support teams', to: `${routes.solutions}#it-support` },
      { label: 'Service operations', to: `${routes.solutions}#service-ops` },
      { label: 'Support managers', to: `${routes.solutions}#managers` },
      { label: 'Enterprise support', to: `${routes.solutions}#enterprise` },
    ],
  },
  {
    title: 'Resources',
    links: [
      { label: 'How it works', to: routes.howItWorks },
      { label: 'API documentation', to: `${routes.howItWorks}#api` },
      { label: 'Security', to: `${routes.features}#security` },
      { label: 'Contact support', to: routes.contact },
    ],
  },
  {
    title: 'Company',
    links: [
      { label: 'About', to: routes.about },
      { label: 'Contact', to: routes.contact },
      { label: 'Sign in', to: routes.signIn },
      { label: 'Get started', to: routes.signUp },
    ],
  },
];

export function Footer() {
  return (
    <footer className="mx-2 mb-2 rounded-[1.75rem] bg-bg-subtle sm:mx-4 sm:mb-4 sm:rounded-[2.25rem] lg:rounded-[2.75rem]">
      <div className="mx-auto max-w-7xl px-4 py-14 sm:px-6 lg:px-8 lg:py-16">
        <div className="grid gap-10 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-[minmax(0,1.4fr)_repeat(4,minmax(0,1fr))]">
          <div className="max-w-xs sm:col-span-2 md:col-span-3 lg:col-span-1">
            <Logo />
            <p className="mt-4 text-sm leading-relaxed text-fg-subtle">
              AI-powered IT support management for modern teams. Automate triage, surface the right
              knowledge, and keep people in control of high-impact decisions.
            </p>

            <div className="mt-5 flex items-center gap-2">
              <a
                href="https://github.com"
                target="_blank"
                rel="noreferrer noopener"
                aria-label="SmartDesk AI on GitHub"
                className="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-line text-fg-subtle transition hover:bg-surface-hover hover:text-fg"
              >
                <GitHubIcon />
              </a>
              <a
                href="https://linkedin.com"
                target="_blank"
                rel="noreferrer noopener"
                aria-label="SmartDesk AI on LinkedIn"
                className="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-line text-fg-subtle transition hover:bg-surface-hover hover:text-fg"
              >
                <LinkedInIcon />
              </a>
            </div>
          </div>

          {groups.map((group) => (
            <nav key={group.title} aria-label={group.title}>
              <h2 className="text-xs font-semibold uppercase tracking-wider text-fg">{group.title}</h2>
              <ul className="mt-4 space-y-2.5">
                {group.links.map((link) => (
                  <li key={link.label}>
                    <Link to={link.to} className="text-sm text-fg-subtle transition hover:text-fg">
                      {link.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </nav>
          ))}
        </div>

        <div className="mt-12 flex flex-col gap-4 border-t border-line pt-6 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm text-fg-subtle">
            © {new Date().getFullYear()} SmartDesk AI. All rights reserved.
          </p>
          <ul className="flex flex-wrap gap-x-6 gap-y-2">
            <li><Link to={routes.about} className="text-sm text-fg-subtle transition hover:text-fg">Privacy</Link></li>
            <li><Link to={routes.about} className="text-sm text-fg-subtle transition hover:text-fg">Terms</Link></li>
            <li><Link to={`${routes.features}#security`} className="text-sm text-fg-subtle transition hover:text-fg">Security</Link></li>
          </ul>
        </div>
      </div>
    </footer>
  );
}

const GitHubIcon = () => (
  <svg width="17" height="17" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
    <path d="M12 2C6.48 2 2 6.58 2 12.25c0 4.53 2.87 8.37 6.84 9.73.5.1.68-.22.68-.49l-.01-1.72c-2.78.62-3.37-1.37-3.37-1.37-.45-1.19-1.11-1.5-1.11-1.5-.91-.64.07-.63.07-.63 1 .07 1.53 1.06 1.53 1.06.89 1.56 2.34 1.11 2.91.85.09-.66.35-1.11.63-1.37-2.22-.26-4.56-1.14-4.56-5.05 0-1.12.39-2.03 1.03-2.75-.1-.26-.45-1.3.1-2.71 0 0 .84-.28 2.75 1.05a9.3 9.3 0 0 1 5 0c1.91-1.33 2.75-1.05 2.75-1.05.55 1.41.2 2.45.1 2.71.64.72 1.03 1.63 1.03 2.75 0 3.92-2.34 4.79-4.57 5.04.36.32.68.94.68 1.9l-.01 2.82c0 .27.18.6.69.49A10.06 10.06 0 0 0 22 12.25C22 6.58 17.52 2 12 2z" />
  </svg>
);

const LinkedInIcon = () => (
  <svg width="17" height="17" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
    <path d="M4.98 3.5a2.5 2.5 0 1 1 0 5 2.5 2.5 0 0 1 0-5zM3 9h4v12H3zM9 9h3.8v1.7h.05c.53-.95 1.83-1.95 3.76-1.95 4.02 0 4.76 2.5 4.76 5.76V21h-4v-5.6c0-1.34-.03-3.07-1.9-3.07-1.9 0-2.19 1.46-2.19 2.97V21H9z" />
  </svg>
);
