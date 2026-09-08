import { render, type RenderOptions } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import type { ReactElement, ReactNode } from 'react';
import { AuthProvider } from '../context/AuthContext';
import { ToastProvider } from '../context/ToastContext';
import { ThemeProvider } from '../context/ThemeContext';
import type { Role, User } from '../types';

export const testUser = (role: Role = 'Employee', overrides: Partial<User> = {}): User => ({
  id: 1,
  email: 'test@smartdesk.local',
  fullName: 'Test User',
  department: 'IT',
  role,
  isActive: true,
  createdAt: '2026-01-01T00:00:00Z',
  ...overrides,
});

/** Puts a signed-in session in localStorage before the AuthProvider mounts. */
export function signIn(role: Role = 'Employee') {
  const user = testUser(role);
  localStorage.setItem('smartdesk.token', 'test-token');
  localStorage.setItem('smartdesk.user', JSON.stringify(user));
  return user;
}

interface Options extends Omit<RenderOptions, 'wrapper'> {
  route?: string;
}

/** Renders a component with the router and both providers, which is what most pages need. */
export function renderWithProviders(ui: ReactElement, { route = '/', ...options }: Options = {}) {
  const Wrapper = ({ children }: { children: ReactNode }) => (
    <MemoryRouter initialEntries={[route]}>
      <ThemeProvider>
        <AuthProvider>
          <ToastProvider>{children}</ToastProvider>
        </AuthProvider>
      </ThemeProvider>
    </MemoryRouter>
  );

  return render(ui, { wrapper: Wrapper, ...options });
}
