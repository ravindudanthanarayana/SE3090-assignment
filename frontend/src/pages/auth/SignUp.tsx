import { useState, type FormEvent } from 'react';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { errorMessage } from '../../api/client';
import { TextInput } from '../../components/Form';
import { buttonClasses } from '../../components/Ui';
import { Arrow } from '../../components/marketing/PublicNav';
import { AuthShell } from './AuthShell';
import { usePageMeta } from '../../hooks/usePageMeta';
import { routes } from '../../routes';

export function SignUp() {
  usePageMeta('Create your account | SmartDesk AI', 'Create a SmartDesk AI account and start raising support requests.');

  const { user, register } = useAuth();
  const navigate = useNavigate();

  const [form, setForm] = useState({ fullName: '', email: '', department: '', password: '', confirm: '' });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  if (user) return <Navigate to={routes.app.dashboard} replace />;

  const set = (key: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm((f) => ({ ...f, [key]: e.target.value }));

  const validate = () => {
    const next: Record<string, string> = {};
    if (!form.fullName.trim()) next.fullName = 'Enter your full name.';
    if (!/^\S+@\S+\.\S+$/.test(form.email)) next.email = 'Enter a valid email address.';
    // Mirrors the API's own minimum, so the user is told before the request is sent.
    if (form.password.length < 8) next.password = 'Use at least 8 characters.';
    if (form.password !== form.confirm) next.confirm = 'The passwords do not match.';
    setErrors(next);
    return Object.keys(next).length === 0;
  };

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setFormError(null);
    if (!validate()) return;

    setBusy(true);
    try {
      await register({
        email: form.email.trim(),
        password: form.password,
        fullName: form.fullName.trim(),
        department: form.department.trim() || undefined,
      });
      navigate(routes.app.dashboard, { replace: true });
    } catch (error) {
      setFormError(errorMessage(error));
    } finally {
      setBusy(false);
    }
  };

  return (
    <AuthShell
      title="Create your SmartDesk account"
      subtitle="Raise and track your own support requests. Staff and administrator roles are granted by an administrator."
      footer={
        <>
          Already have an account?{' '}
          <Link to={routes.signIn} className="font-medium text-accent-text hover:underline">
            Sign in
          </Link>
        </>
      }
    >
      <form onSubmit={submit} noValidate className="space-y-4">
        {formError && (
          <div role="alert" className="rounded-xl border border-danger/30 bg-danger-soft px-3.5 py-2.5 text-sm text-danger">
            {formError}
          </div>
        )}

        <TextInput id="fullName" label="Full name" required autoComplete="name"
                   value={form.fullName} onChange={set('fullName')} error={errors.fullName} />
        <TextInput id="email" label="Work email" type="email" autoComplete="email" required
                   value={form.email} onChange={set('email')} error={errors.email}
                   placeholder="you@company.com" />
        <TextInput id="department" label="Department" value={form.department}
                   onChange={set('department')} hint="Optional" />
        <TextInput id="password" label="Password" type="password" autoComplete="new-password" required
                   value={form.password} onChange={set('password')} error={errors.password}
                   hint="At least 8 characters" />
        <TextInput id="confirm" label="Confirm password" type="password" autoComplete="new-password" required
                   value={form.confirm} onChange={set('confirm')} error={errors.confirm} />

        <button type="submit" disabled={busy} className={`${buttonClasses('primary', 'lg')} w-full`}>
          {busy ? 'Creating account…' : <>Create account <Arrow /></>}
        </button>
      </form>
    </AuthShell>
  );
}
