import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { Suspense, useEffect, useState, type ReactNode } from 'react';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { Logo } from './brand/Logo';
import { ThemeToggle } from './ThemeToggle';
import { routes } from '../routes';
import { Spinner } from './Ui';
import type { Role } from '../types';

interface NavItem {
  to: string;
  label: string;
  icon: ReactNode;
  /** Which roles may see this link. The backend enforces the same rules independently. */
  roles: Role[];
}

interface NavGroup {
  label: string;
  items: NavItem[];
}

const ALL: Role[] = ['Employee', 'SupportAgent', 'SupportManager', 'Admin'];
const STAFF: Role[] = ['SupportAgent', 'SupportManager', 'Admin'];
const MANAGER: Role[] = ['SupportManager', 'Admin'];
const ADMIN: Role[] = ['Admin'];

const navigation: NavGroup[] = [
  {
    label: 'Workspace',
    items: [
      { to: routes.app.dashboard, label: 'Dashboard', roles: ALL, icon: <IconGrid /> },
      { to: routes.app.tickets, label: 'Tickets', roles: ALL, icon: <IconTicket /> },
      { to: routes.app.knowledge, label: 'Knowledge base', roles: ALL, icon: <IconBook /> },
    ],
  },
  {
    label: 'Operations',
    items: [
      { to: routes.app.assignments, label: 'Assignments', roles: MANAGER, icon: <IconInbox /> },
      { to: routes.app.agents, label: 'Agents & workload', roles: MANAGER, icon: <IconUsers /> },
      { to: routes.app.sla, label: 'SLA', roles: STAFF, icon: <IconClock /> },
      { to: routes.app.reports, label: 'Reports', roles: MANAGER, icon: <IconChart /> },
    ],
  },
  {
    label: 'Agentic AI',
    items: [
      { to: routes.app.workflows, label: 'AI workflows', roles: STAFF, icon: <IconSparkle /> },
      { to: routes.app.approvals, label: 'Approval centre', roles: MANAGER, icon: <IconCheck /> },
      { to: routes.app.audit, label: 'Audit trail', roles: MANAGER, icon: <IconList /> },
    ],
  },
  {
    label: 'Administration',
    items: [
      { to: routes.app.users, label: 'Users', roles: ADMIN, icon: <IconUser /> },
      { to: routes.app.categories, label: 'Categories', roles: ADMIN, icon: <IconTag /> },
    ],
  },
];

/**
 * The authenticated workspace shell.
 *
 * Deliberately different from the marketing site's chrome - no public navigation appears here -
 * while sharing the same tokens, logo and components so it reads as the same product.
 */
export function Layout() {
  const { user, logout } = useAuth();
  const { toasts, dismiss } = useToast();
  const navigate = useNavigate();
  const location = useLocation();
  const [sidebarOpen, setSidebarOpen] = useState(false);

  // Navigating on a phone should close the drawer, or the new page is hidden behind it.
  useEffect(() => setSidebarOpen(false), [location.pathname]);

  const groups = navigation
    .map((group) => ({ ...group, items: group.items.filter((i) => user && i.roles.includes(user.role)) }))
    .filter((group) => group.items.length > 0);

  const handleLogout = () => {
    logout();
    navigate(routes.home);
  };

  return (
    <div className="min-h-screen bg-bg-subtle">
      {/* Mobile scrim */}
      {sidebarOpen && (
        <div
          className="fixed inset-0 z-30 bg-black/40 lg:hidden"
          onClick={() => setSidebarOpen(false)}
          aria-hidden="true"
        />
      )}

      {/* Sidebar */}
      <aside
        className={`fixed inset-y-0 left-0 z-40 flex w-64 flex-col border-r border-line bg-surface
                    transition-transform duration-200 lg:translate-x-0
                    ${sidebarOpen ? 'translate-x-0' : '-translate-x-full'}`}
      >
        <div className="flex h-16 items-center border-b border-line px-5">
          <Logo to={routes.app.dashboard} markClassName="h-6 w-6" textClassName="text-[15px]" />
        </div>

        <nav aria-label="Workspace" className="flex-1 overflow-y-auto px-3 py-4">
          {groups.map((group) => (
            <div key={group.label} className="mb-5">
              <p className="mb-1.5 px-3 text-[11px] font-semibold uppercase tracking-wider text-fg-subtle">
                {group.label}
              </p>
              <ul className="space-y-0.5">
                {group.items.map((item) => (
                  <li key={item.to}>
                    <NavLink
                      to={item.to}
                      className={({ isActive }) =>
                        `flex items-center gap-2.5 rounded-full px-3.5 py-2 text-sm font-medium transition ${
                          isActive
                            ? 'bg-accent-soft text-accent-text'
                            : 'text-fg-muted hover:bg-surface-hover hover:text-fg'
                        }`
                      }
                    >
                      <span className="shrink-0 opacity-80" aria-hidden="true">{item.icon}</span>
                      {item.label}
                    </NavLink>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </nav>

        <div className="border-t border-line p-3">
          <div className="flex items-center gap-3 rounded-full px-2 py-2">
            <span
              className="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-accent-soft text-xs font-semibold text-accent-text"
              aria-hidden="true"
            >
              {initials(user?.fullName)}
            </span>
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-medium text-fg">{user?.fullName}</p>
              <p className="truncate text-xs text-fg-subtle">{user?.role}</p>
            </div>
          </div>
          <button
            type="button"
            onClick={handleLogout}
            className="mt-1 w-full rounded-full px-3.5 py-2 text-left text-sm font-medium text-fg-muted transition hover:bg-surface-hover hover:text-fg"
          >
            Sign out
          </button>
        </div>
      </aside>

      {/* Main column */}
      <div className="lg:pl-64">
        <header className="sticky top-0 z-20 flex h-16 items-center gap-3 border-b border-line bg-surface/85 px-4 backdrop-blur sm:px-6">
          <button
            type="button"
            className="rounded-full p-2 text-fg-muted transition hover:bg-surface-hover lg:hidden"
            aria-label="Open navigation"
            aria-expanded={sidebarOpen}
            onClick={() => setSidebarOpen(true)}
          >
            <IconMenu />
          </button>

          <div className="lg:hidden">
            <Logo to={routes.app.dashboard} markClassName="h-6 w-6" textClassName="text-[15px]" />
          </div>

          <div className="ml-auto flex items-center gap-2">
            <ThemeToggle />
          </div>
        </header>

        <main className="mx-auto w-full max-w-7xl px-4 py-6 sm:px-6 lg:py-8">
          <Suspense fallback={<div className="grid min-h-[50vh] place-items-center"><Spinner /></div>}>
            <Outlet />
          </Suspense>
        </main>
      </div>

      {/* aria-live so a screen reader announces success and error feedback */}
      <div className="fixed bottom-4 right-4 z-50 space-y-2" aria-live="polite">
        {toasts.map((toast) => (
          <div
            key={toast.id}
            role={toast.kind === 'error' ? 'alert' : 'status'}
            className={`flex max-w-sm items-start gap-3 rounded-2xl border px-4 py-3 text-sm shadow-float ${
              toast.kind === 'success' ? 'border-ok/30 bg-ok-soft text-ok'
              : toast.kind === 'error' ? 'border-danger/30 bg-danger-soft text-danger'
              : 'border-line bg-surface text-fg-muted'
            }`}
          >
            <span className="flex-1">{toast.message}</span>
            <button type="button" onClick={() => dismiss(toast.id)} aria-label="Dismiss"
                    className="opacity-60 transition hover:opacity-100">
              ✕
            </button>
          </div>
        ))}
      </div>
    </div>
  );
}

function initials(name?: string) {
  if (!name) return '?';
  return name.split(' ').filter(Boolean).slice(0, 2).map((p) => p[0]).join('').toUpperCase();
}

/* Inline icons: no icon library, so the bundle stays small and every glyph is themeable. */
const ic = { width: 16, height: 16, viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor',
  strokeWidth: 1.8, strokeLinecap: 'round' as const, strokeLinejoin: 'round' as const, 'aria-hidden': true };

function IconGrid() { return <svg {...ic}><rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/></svg>; }
function IconTicket() { return <svg {...ic}><path d="M3 9V7a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v2a2 2 0 0 0 0 6v2a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-2a2 2 0 0 0 0-6z"/><path d="M13 5v14"/></svg>; }
function IconBook() { return <svg {...ic}><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"/><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"/></svg>; }
function IconInbox() { return <svg {...ic}><path d="M22 12h-6l-2 3h-4l-2-3H2"/><path d="M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z"/></svg>; }
function IconUsers() { return <svg {...ic}><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75"/></svg>; }
function IconUser() { return <svg {...ic}><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>; }
function IconClock() { return <svg {...ic}><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></svg>; }
function IconChart() { return <svg {...ic}><path d="M3 3v18h18"/><path d="M7 15l3-4 3 3 5-7"/></svg>; }
function IconSparkle() { return <svg {...ic}><path d="M12 3l1.9 4.8L19 9.7l-4.8 1.9L12 16.4l-1.9-4.8L5 9.7l5.1-1.9z"/><path d="M18 15l.9 2.2L21 18l-2.1.8L18 21l-.9-2.2L15 18l2.1-.8z"/></svg>; }
function IconCheck() { return <svg {...ic}><path d="M21 11.5V12a9 9 0 1 1-5.3-8.2"/><path d="M9 11l3 3L22 4"/></svg>; }
function IconList() { return <svg {...ic}><path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01"/></svg>; }
function IconTag() { return <svg {...ic}><path d="M20.6 13.4 12 22l-9-9V4a1 1 0 0 1 1-1h9z"/><circle cx="7.5" cy="7.5" r="1.3"/></svg>; }
function IconMenu() { return <svg {...ic} width="20" height="20"><path d="M3 6h18M3 12h18M3 18h18"/></svg>; }
