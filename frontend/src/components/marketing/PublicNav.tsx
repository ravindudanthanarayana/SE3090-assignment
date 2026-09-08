import { useEffect, useState } from 'react';
import { Link, NavLink, useLocation } from 'react-router-dom';
import { Logo } from '../brand/Logo';
import { ThemeToggle } from '../ThemeToggle';
import { buttonClasses } from '../Ui';
import { cn } from '@/lib/utils';
import { useAuth } from '../../context/AuthContext';
import { routes } from '../../routes';

const links = [
  { to: routes.features, label: 'Product' },
  { to: routes.solutions, label: 'Solutions' },
  { to: routes.aiAgents, label: 'AI Agents' },
  { to: routes.howItWorks, label: 'How it works' },
  { to: routes.about, label: 'Company' },
];

export function PublicNav() {
  const [open, setOpen] = useState(false);
  const [scrolled, setScrolled] = useState(false);
  const location = useLocation();
  const { user } = useAuth();

  useEffect(() => setOpen(false), [location.pathname]);

  // A hairline border only once the page has moved, so the hero reads as edge-to-edge.
  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 8);
    onScroll();
    window.addEventListener('scroll', onScroll, { passive: true });
    return () => window.removeEventListener('scroll', onScroll);
  }, []);

  const linkClass = ({ isActive }: { isActive: boolean }) =>
    `rounded-full px-3.5 py-2 text-sm font-medium transition ${
      isActive ? 'text-fg' : 'text-fg-muted hover:text-fg'
    }`;

  return (
    <header className="fixed inset-x-0 top-0 z-50 px-3 pt-3 sm:px-5 sm:pt-4">
      {/* A floating island rather than a full-width bar. It gains depth once the page moves,
          so the hero reads as edge-to-edge on first paint. */}
      <div
        className={`mx-auto flex h-14 max-w-6xl items-center gap-4 rounded-full border px-3 backdrop-blur-xl transition-all duration-300 sm:px-4 ${
          scrolled
            ? 'border-line bg-surface/85 shadow-float'
            : 'border-line/60 bg-surface/60 shadow-card'
        }`}
      >
        <div className="pl-1">
          <Logo />
        </div>

        <nav aria-label="Main" className="ml-4 hidden items-center gap-0.5 lg:flex">
          {links.map((link) => (
            <NavLink key={link.to} to={link.to} className={linkClass}>
              {link.label}
            </NavLink>
          ))}
        </nav>

        <div className="ml-auto flex items-center gap-2">
          <ThemeToggle />

          {/* A signed-in visitor browsing the marketing site gets a way back into their workspace. */}
          {user ? (
            <Link to={routes.app.dashboard} className={cn(buttonClasses('primary', 'sm'), 'hidden sm:inline-flex')}>
              Go to workspace
              <Arrow />
            </Link>
          ) : (
            <>
              <Link
                to={routes.signIn}
                className="hidden rounded-full px-3.5 py-2 text-sm font-medium text-fg-muted transition hover:text-fg sm:block"
              >
                Sign in
              </Link>
              <Link to={routes.signUp} className={cn(buttonClasses('primary', 'sm'), 'hidden sm:inline-flex')}>
                Get started
                <Arrow />
              </Link>
            </>
          )}

          <button
            type="button"
            onClick={() => setOpen((o) => !o)}
            aria-expanded={open}
            aria-controls="mobile-nav"
            aria-label="Toggle navigation"
            className="inline-flex h-9 w-9 items-center justify-center rounded-full border border-line text-fg-muted transition hover:bg-surface-hover lg:hidden"
          >
            {open ? <Close /> : <Burger />}
          </button>
        </div>
      </div>

      {open && (
        <div
          id="mobile-nav"
          className="mx-auto mt-2 max-w-6xl overflow-hidden rounded-2xl border border-line bg-surface/95 shadow-float backdrop-blur-xl lg:hidden"
        >
          <nav aria-label="Main" className="px-3 py-3">
            <ul className="space-y-1">
              {links.map((link) => (
                <li key={link.to}>
                  <NavLink
                    to={link.to}
                    className={({ isActive }) =>
                      `block rounded-full px-4 py-2.5 text-sm font-medium transition ${
                        isActive ? 'bg-surface-2 text-fg' : 'text-fg-muted hover:bg-surface-hover'
                      }`
                    }
                  >
                    {link.label}
                  </NavLink>
                </li>
              ))}
            </ul>

            <div className="mt-3 flex flex-col gap-2 border-t border-line pt-3">
              {user ? (
                <Link to={routes.app.dashboard} className={buttonClasses('primary')}>
                  Go to workspace <Arrow />
                </Link>
              ) : (
                <>
                  <Link to={routes.signIn} className={buttonClasses('secondary')}>Sign in</Link>
                  <Link to={routes.signUp} className={buttonClasses('primary')}>
                    Get started <Arrow />
                  </Link>
                </>
              )}
            </div>
          </nav>
        </div>
      )}
    </header>
  );
}

export const Arrow = () => (
  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor"
       strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
    <path d="M5 12h14M13 6l6 6-6 6" />
  </svg>
);

const Burger = () => (
  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"
       strokeWidth="2" strokeLinecap="round" aria-hidden="true">
    <path d="M3 6h18M3 12h18M3 18h18" />
  </svg>
);

const Close = () => (
  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"
       strokeWidth="2" strokeLinecap="round" aria-hidden="true">
    <path d="M18 6 6 18M6 6l12 12" />
  </svg>
);
