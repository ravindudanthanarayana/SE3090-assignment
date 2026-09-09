import type { ReactNode } from 'react';
import type { ApprovalStatus, SlaState, TicketPriority, TicketStatus, WorkflowStatus } from '../types';

/* ------------------------------------------------------------------ Buttons */

type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger';
type ButtonSize = 'sm' | 'md' | 'lg';

const buttonBase =
  'inline-flex items-center justify-center gap-2 rounded-full font-medium transition ' +
  'disabled:cursor-not-allowed disabled:opacity-60';

const buttonVariants: Record<ButtonVariant, string> = {
  primary: 'bg-accent-solid text-accent-fg hover:bg-accent-hover shadow-card',
  secondary: 'border border-line bg-surface text-fg hover:bg-surface-hover',
  ghost: 'text-fg-muted hover:bg-surface-hover hover:text-fg',
  danger: 'bg-danger text-white hover:opacity-90',
};

// Pills need more horizontal room than a rectangle to look balanced.
const buttonSizes: Record<ButtonSize, string> = {
  sm: 'h-8 px-4 text-[13px]',
  md: 'h-10 px-5 text-sm',
  lg: 'h-12 px-7 text-[15px]',
};

export function Button({
  variant = 'primary', size = 'md', className = '', ...props
}: React.ButtonHTMLAttributes<HTMLButtonElement> & { variant?: ButtonVariant; size?: ButtonSize }) {
  return (
    <button
      {...props}
      className={`${buttonBase} ${buttonVariants[variant]} ${buttonSizes[size]} ${className}`}
    />
  );
}

/** Same visual language as Button, for react-router Links and anchors. */
export function buttonClasses(variant: ButtonVariant = 'primary', size: ButtonSize = 'md') {
  return `${buttonBase} ${buttonVariants[variant]} ${buttonSizes[size]}`;
}

/* ------------------------------------------------------------------- Badges */

const chip =
  'inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset';

/* Colour is never the only signal - every badge also carries its own words. */
const statusStyles: Record<TicketStatus, string> = {
  New: 'bg-surface-2 text-fg-muted ring-line-strong',
  Assigned: 'bg-info-soft text-info ring-info/25',
  InProgress: 'bg-info-soft text-info ring-info/25',
  OnHold: 'bg-warn-soft text-warn ring-warn/25',
  Escalated: 'bg-accent-soft text-accent-text ring-accent/30',
  Resolved: 'bg-ok-soft text-ok ring-ok/25',
  Closed: 'bg-surface-2 text-fg-subtle ring-line',
  Cancelled: 'bg-surface-2 text-fg-subtle ring-line',
};

const priorityStyles: Record<TicketPriority, string> = {
  Low: 'bg-surface-2 text-fg-subtle ring-line',
  Medium: 'bg-info-soft text-info ring-info/25',
  High: 'bg-warn-soft text-warn ring-warn/25',
  Critical: 'bg-danger-soft text-danger ring-danger/30',
};

const slaStyles: Record<SlaState, string> = {
  OnTrack: 'bg-ok-soft text-ok ring-ok/25',
  AtRisk: 'bg-warn-soft text-warn ring-warn/25',
  Breached: 'bg-danger-soft text-danger ring-danger/30',
  NotApplicable: 'bg-surface-2 text-fg-subtle ring-line',
};

const slaLabels: Record<SlaState, string> = {
  OnTrack: 'On track', AtRisk: 'At risk', Breached: 'Breached', NotApplicable: 'Closed',
};

const workflowStyles: Record<WorkflowStatus, string> = {
  Planned: 'bg-surface-2 text-fg-muted ring-line-strong',
  Running: 'bg-info-soft text-info ring-info/25',
  AwaitingApproval: 'bg-warn-soft text-warn ring-warn/25',
  Completed: 'bg-ok-soft text-ok ring-ok/25',
  Failed: 'bg-danger-soft text-danger ring-danger/30',
  Rejected: 'bg-surface-2 text-fg-subtle ring-line',
};

const approvalStyles: Record<ApprovalStatus, string> = {
  Pending: 'bg-warn-soft text-warn ring-warn/25',
  Approved: 'bg-ok-soft text-ok ring-ok/25',
  Rejected: 'bg-danger-soft text-danger ring-danger/30',
  RevisionRequested: 'bg-info-soft text-info ring-info/25',
};

/** "AwaitingApproval" reads badly; "Awaiting approval" does not. */
function spaced(value: string) {
  const withSpaces = value.replace(/([a-z])([A-Z])/g, '$1 $2');
  return withSpaces.charAt(0) + withSpaces.slice(1).toLowerCase();
}

export const StatusBadge = ({ status }: { status: TicketStatus }) => (
  <span className={`${chip} ${statusStyles[status]}`}>{spaced(status)}</span>
);
export const PriorityBadge = ({ priority }: { priority: TicketPriority }) => (
  <span className={`${chip} ${priorityStyles[priority]}`}>{priority}</span>
);
export const SlaBadge = ({ state }: { state: SlaState }) => (
  <span className={`${chip} ${slaStyles[state]}`}>{slaLabels[state]}</span>
);
export const WorkflowBadge = ({ status }: { status: WorkflowStatus }) => (
  <span className={`${chip} ${workflowStyles[status]}`}>{spaced(status)}</span>
);
export const ApprovalBadge = ({ status }: { status: ApprovalStatus }) => (
  <span className={`${chip} ${approvalStyles[status]}`}>{spaced(status)}</span>
);

/** Small neutral label, used for roles, tags and counts. */
export const Tag = ({ children }: { children: ReactNode }) => (
  <span className="inline-flex items-center rounded-full bg-surface-2 px-2.5 py-0.5 text-xs text-fg-muted">
    {children}
  </span>
);

/* ------------------------------------------------------------- Async states */

export const Spinner = ({ label = 'Loading' }: { label?: string }) => (
  <div className="flex items-center gap-2.5 text-sm text-fg-subtle" role="status" aria-live="polite">
    <span className="h-4 w-4 animate-spin rounded-full border-2 border-line-strong border-t-accent" />
    {label}
  </div>
);

export const ErrorMessage = ({ message, onRetry }: { message: string; onRetry?: () => void }) => (
  <div className="rounded-2xl border border-danger/30 bg-danger-soft p-5" role="alert">
    <p className="font-semibold text-danger">Something went wrong</p>
    <p className="mt-1 text-sm text-fg-muted">{message}</p>
    {onRetry && (
      <Button variant="secondary" size="sm" onClick={onRetry} className="mt-3">
        Try again
      </Button>
    )}
  </div>
);

export const EmptyState = ({ title, hint, action }: { title: string; hint?: string; action?: ReactNode }) => (
  <div className="rounded-2xl border border-dashed border-line-strong bg-surface p-12 text-center">
    <p className="font-medium text-fg">{title}</p>
    {hint && <p className="mx-auto mt-1.5 max-w-md text-sm text-fg-subtle">{hint}</p>}
    {action && <div className="mt-5 flex justify-center">{action}</div>}
  </div>
);

interface AsyncStateProps<T> {
  loading: boolean;
  error: string | null;
  data: T | null;
  onRetry?: () => void;
  isEmpty?: (data: T) => boolean;
  emptyTitle?: string;
  emptyHint?: string;
  emptyAction?: ReactNode;
  children: (data: T) => ReactNode;
}

/**
 * Renders exactly one of: loading, error, empty or content.
 * Every page uses this, which is why the four required states stay consistent instead of
 * being reinvented - or forgotten - screen by screen.
 */
export function AsyncState<T>({
  loading, error, data, onRetry, isEmpty,
  emptyTitle = 'Nothing to show yet', emptyHint, emptyAction, children,
}: AsyncStateProps<T>) {
  if (loading && data === null) return <div className="p-8"><Spinner /></div>;
  if (error) return <ErrorMessage message={error} onRetry={onRetry} />;
  if (data === null) return <EmptyState title={emptyTitle} hint={emptyHint} action={emptyAction} />;
  if (isEmpty?.(data)) return <EmptyState title={emptyTitle} hint={emptyHint} action={emptyAction} />;
  return <>{children(data)}</>;
}

/* -------------------------------------------------------------- Containers */

export const Card = ({
  title, description, action, children, className = '', padded = true,
}: {
  title?: string; description?: string; action?: ReactNode;
  children: ReactNode; className?: string; padded?: boolean;
}) => (
  <section className={`rounded-2xl border border-line bg-surface shadow-card ${className}`}>
    {title && (
      <header className="flex flex-wrap items-center justify-between gap-3 border-b border-line px-5 py-3.5">
        <div>
          <h2 className="text-sm font-semibold text-fg">{title}</h2>
          {description && <p className="mt-0.5 text-xs text-fg-subtle">{description}</p>}
        </div>
        {action}
      </header>
    )}
    <div className={padded ? 'p-5' : ''}>{children}</div>
  </section>
);

export const StatCard = ({
  label, value, tone = 'default', hint,
}: {
  label: string; value: number | string; tone?: 'default' | 'warn' | 'danger' | 'good' | 'accent';
  hint?: string;
}) => {
  const tones = {
    default: 'text-fg', warn: 'text-warn', danger: 'text-danger',
    good: 'text-ok', accent: 'text-accent',
  };
  return (
    <div className="rounded-2xl border border-line bg-surface p-4 shadow-card transition hover:border-line-strong">
      <p className="text-xs font-medium uppercase tracking-wide text-fg-subtle">{label}</p>
      <p className={`mt-1.5 text-2xl font-semibold tabular-nums ${tones[tone]}`}>{value}</p>
      {hint && <p className="mt-1 text-xs text-fg-subtle">{hint}</p>}
    </div>
  );
};

/** Consistent page heading used by every application screen. */
export const PageHeader = ({
  title, description, action,
}: { title: string; description?: string; action?: ReactNode }) => (
  <header className="flex flex-wrap items-start justify-between gap-4">
    <div>
      <h1 className="text-xl font-semibold tracking-tight text-fg">{title}</h1>
      {description && <p className="mt-1 max-w-2xl text-sm text-fg-subtle">{description}</p>}
    </div>
    {action}
  </header>
);
