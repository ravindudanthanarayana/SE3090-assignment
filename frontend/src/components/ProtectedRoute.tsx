import { Navigate, useLocation } from 'react-router-dom';
import type { ReactNode } from 'react';
import { useAuth } from '../context/AuthContext';
import { Spinner } from './Ui';
import type { Role } from '../types';

interface ProtectedRouteProps {
  children: ReactNode;
  /** When set, the signed-in user must hold one of these roles. */
  roles?: Role[];
}

/**
 * Route guard.
 *
 * This is a usability control, not a security control: it stops people navigating to a page they
 * cannot use. The API enforces the same rules independently, so a user who edits their own
 * JavaScript still gets a 403 from the backend.
 */
export function ProtectedRoute({ children, roles }: ProtectedRouteProps) {
  const { user, loading } = useAuth();
  const location = useLocation();

  // Wait for the stored token to be revalidated before deciding, or a refresh would bounce
  // a signed-in user to the login page.
  if (loading) {
    return <div className="grid min-h-screen place-items-center"><Spinner label="Checking your session" /></div>;
  }

  if (!user) {
    return <Navigate to="/signin" replace state={{ from: location.pathname }} />;
  }

  if (roles && !roles.includes(user.role)) {
    return (
      <div className="rounded-xl border border-warn/30 bg-warn-soft p-6" role="alert">
        <h1 className="font-semibold text-warn">You do not have access to this page</h1>
        <p className="mt-1 text-sm text-fg-muted">
          This page is available to: {roles.join(', ')}. You are signed in as {user.role}.
        </p>
      </div>
    );
  }

  return <>{children}</>;
}
