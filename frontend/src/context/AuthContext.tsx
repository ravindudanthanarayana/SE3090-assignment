import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { authApi } from '../api/endpoints';
import { setUnauthorizedHandler, tokenStorage } from '../api/client';
import type { Role, User } from '../types';

interface AuthState {
  user: User | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (body: { email: string; password: string; fullName: string; department?: string }) => Promise<void>;
  logout: () => void;
  hasRole: (...roles: Role[]) => boolean;
}

const AuthContext = createContext<AuthState | undefined>(undefined);

/**
 * Authentication state for the whole app.
 *
 * Context API rather than Redux or Zustand: identity plus the JWT is the only genuinely global
 * client state we have, and everything else is server data fetched per page. See ADR-001.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(() => tokenStorage.getUser());
  const [loading, setLoading] = useState(true);

  const logout = useCallback(() => {
    tokenStorage.clear();
    setUser(null);
  }, []);

  // A token that the API rejects must clear the session everywhere at once.
  useEffect(() => setUnauthorizedHandler(logout), [logout]);

  // On a page refresh the stored token may already have expired, so it is revalidated
  // against the API rather than trusted.
  useEffect(() => {
    if (!tokenStorage.get()) {
      setLoading(false);
      return;
    }

    authApi
      .me()
      .then((me) => {
        setUser(me);
        tokenStorage.setUser(me);
      })
      .catch(() => logout())
      .finally(() => setLoading(false));
  }, [logout]);

  const login = useCallback(async (email: string, password: string) => {
    const result = await authApi.login(email, password);
    tokenStorage.set(result.token);
    tokenStorage.setUser(result.user);
    setUser(result.user);
  }, []);

  const register = useCallback(
    async (body: { email: string; password: string; fullName: string; department?: string }) => {
      const result = await authApi.register(body);
      tokenStorage.set(result.token);
      tokenStorage.setUser(result.user);
      setUser(result.user);
    },
    [],
  );

  const hasRole = useCallback(
    (...roles: Role[]) => (user ? roles.includes(user.role) : false),
    [user],
  );

  const value = useMemo(
    () => ({ user, loading, login, register, logout, hasRole }),
    [user, loading, login, register, logout, hasRole],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used inside an AuthProvider.');
  return context;
}
