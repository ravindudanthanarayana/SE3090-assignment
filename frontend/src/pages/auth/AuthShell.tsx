import { Link } from 'react-router-dom';
import type { ReactNode } from 'react';
import { Logo } from '../../components/brand/Logo';
import { ThemeToggle } from '../../components/ThemeToggle';
import { CheckDot } from '../public/Landing';
import { routes } from '../../routes';

/**
 * Chrome shared by sign in and sign up, so authentication clearly belongs to the same
 * company website rather than looking like a detached form.
 */
export function AuthShell({
  title, subtitle, children, footer,
}: { title: string; subtitle: string; children: ReactNode; footer: ReactNode }) {
  return (
    <div className="grid min-h-screen bg-bg lg:grid-cols-2">
      {/* Form column */}
      <div className="flex flex-col px-4 py-8 sm:px-8">
        <div className="flex items-center justify-between">
          <Logo />
          <ThemeToggle />
        </div>

        <div className="flex flex-1 items-center justify-center py-10">
          <div className="w-full max-w-sm">
            <h1 className="text-2xl font-semibold tracking-tight text-fg">{title}</h1>
            <p className="mt-2 text-sm text-fg-muted">{subtitle}</p>

            <div className="mt-8">{children}</div>

            <div className="mt-6 text-sm text-fg-subtle">{footer}</div>
          </div>
        </div>

        <Link
          to={routes.home}
          className="inline-flex items-center gap-1.5 self-start text-sm text-fg-subtle transition hover:text-fg"
        >
          <span aria-hidden="true">←</span> Back to SmartDesk AI
        </Link>
      </div>

      {/* Brand column, hidden on small screens where it would only push the form down */}
      <aside className="relative hidden border-l border-line sd-glow lg:flex lg:flex-col lg:justify-center lg:px-14">
        <blockquote className="max-w-md">
          <p className="text-2xl font-semibold leading-snug tracking-tight text-fg">
            “AI recommends. Your team decides.”
          </p>
          <p className="mt-5 leading-relaxed text-fg-muted">
            SmartDesk AI triages every request, finds the relevant knowledge and recommends an
            owner — then pauses and waits for a person before anything high-impact happens.
          </p>
        </blockquote>

        <ul className="mt-10 max-w-md space-y-3">
          {[
            'Five specialised agents, one governed workflow',
            'High-impact actions require human approval',
            'Every decision recorded in a complete audit trail',
          ].map((item) => (
            <li key={item} className="flex items-start gap-2.5 text-sm text-fg-muted">
              <span className="mt-0.5"><CheckDot /></span>
              {item}
            </li>
          ))}
        </ul>
      </aside>
    </div>
  );
}
