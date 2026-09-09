import type { ReactNode, SelectHTMLAttributes, TextareaHTMLAttributes, InputHTMLAttributes } from 'react';

const fieldClasses =
  'w-full rounded-xl border border-line bg-surface px-3 py-2 text-sm text-fg shadow-card ' +
  'placeholder:text-fg-subtle/70 transition ' +
  'focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/25 ' +
  'disabled:bg-surface-2 disabled:text-fg-subtle';

interface FieldWrapperProps {
  label: string;
  htmlFor: string;
  error?: string;
  hint?: string;
  required?: boolean;
  children: ReactNode;
}

/** Label, hint and error in one place, wired up with the aria attributes a screen reader needs. */
export function Field({ label, htmlFor, error, hint, required, children }: FieldWrapperProps) {
  return (
    <div>
      <label htmlFor={htmlFor} className="mb-1.5 block text-sm font-medium text-fg">
        {label}
        {required && <span className="ml-0.5 text-danger" aria-hidden="true">*</span>}
      </label>
      {children}
      {hint && !error && <p className="mt-1.5 text-xs text-fg-subtle">{hint}</p>}
      {error && (
        <p id={`${htmlFor}-error`} role="alert" className="mt-1.5 text-xs text-danger">
          {error}
        </p>
      )}
    </div>
  );
}

export const TextInput = ({
  label, error, hint, id, required, ...props
}: InputHTMLAttributes<HTMLInputElement> & { label: string; error?: string; hint?: string; id: string }) => (
  <Field label={label} htmlFor={id} error={error} hint={hint} required={required}>
    <input
      {...props}
      id={id}
      required={required}
      aria-invalid={error ? true : undefined}
      aria-describedby={error ? `${id}-error` : undefined}
      className={`${fieldClasses} ${error ? 'border-danger focus:ring-danger/25' : ''}`}
    />
  </Field>
);

export const TextArea = ({
  label, error, hint, id, required, ...props
}: TextareaHTMLAttributes<HTMLTextAreaElement> & { label: string; error?: string; hint?: string; id: string }) => (
  <Field label={label} htmlFor={id} error={error} hint={hint} required={required}>
    <textarea
      {...props}
      id={id}
      required={required}
      aria-invalid={error ? true : undefined}
      aria-describedby={error ? `${id}-error` : undefined}
      className={`${fieldClasses} ${error ? 'border-danger focus:ring-danger/25' : ''}`}
    />
  </Field>
);

export const Select = ({
  label, error, hint, id, required, children, ...props
}: SelectHTMLAttributes<HTMLSelectElement> & { label: string; error?: string; hint?: string; id: string }) => (
  <Field label={label} htmlFor={id} error={error} hint={hint} required={required}>
    <select
      {...props}
      id={id}
      required={required}
      aria-invalid={error ? true : undefined}
      aria-describedby={error ? `${id}-error` : undefined}
      className={`${fieldClasses} ${error ? 'border-danger focus:ring-danger/25' : ''}`}
    >
      {children}
    </select>
  </Field>
);
