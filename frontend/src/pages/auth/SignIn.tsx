import { useState, type FormEvent } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { errorMessage } from '../../api/client';
import { TextInput } from '../../components/Form';
import { buttonClasses } from '../../components/Ui';
import { Arrow } from '../../components/marketing/PublicNav';
import { AuthShell } from './AuthShell';
import { usePageMeta } from '../../hooks/usePageMeta';
import { routes } from '../../routes';

export function SignIn() {
  usePageMeta('Sign in | SmartDesk AI', 'Sign in to your SmartDesk AI workspace.');

  const { user, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation() as { state?: { from?: string } };

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [errors, setErrors] = useState<{ email?: string; password?: string }>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  if (user) return <Navigate to={location.state?.from ?? routes.app.dashboard} replace />;

  /** Client-side validation is for fast feedback; the API validates everything again. */
  const validate = () => {
    const next: typeof errors = {};
    if (!email.trim()) next.email = 'Enter your email address.';
    else if (!/^\S+@\S+\.\S+$/.test(email)) next.email = 'Enter a valid email address.';
    if (!password) next.password = 'Enter your password.';
    setErrors(next);
    return Object.keys(next).length === 0;
  };

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setFormError(null);
    if (!validate()) return;

    setBusy(true);
    try {
      await login(email.trim(), password);
      navigate(location.state?.from ?? routes.app.dashboard, { replace: true });
    } catch (error) {
      setFormError(errorMessage(error));
    } finally {
      setBusy(false);
    }
  };

  return (
    <AuthShell
      title="Welcome back"
      subtitle="Sign in to your SmartDesk workspace."
      footer={
        <>
          Don’t have an account?{' '}
          <Link to={routes.signUp} className="font-medium text-accent-text hover:underline">
            Create an account
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

        <TextInput
          id="email" label="Email address" type="email" autoComplete="email" required
          value={email} onChange={(e) => setEmail(e.target.value)} error={errors.email}
          placeholder="you@company.com"
        />

        <div>
          <TextInput
            id="password" label="Password" type="password" autoComplete="current-password" required
            value={password} onChange={(e) => setPassword(e.target.value)} error={errors.password}
          />
          <div className="mt-1.5 text-right">
            {/* Password reset is not implemented in the API, so this points at the people who can
                actually reset it rather than a route that does nothing. */}
            <Link to={routes.contact} className="text-xs text-fg-subtle transition hover:text-fg">
              Forgot password?
            </Link>
          </div>
        </div>

        <button type="submit" disabled={busy} className={`${buttonClasses('primary', 'lg')} w-full`}>
          {busy ? 'Signing in…' : <>Sign in <Arrow /></>}
        </button>
      </form>
    </AuthShell>
  );
}
