import { describe, expect, it, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { Route, Routes } from 'react-router-dom';
import { ProtectedRoute } from './ProtectedRoute';
import { renderWithProviders, signIn, testUser } from '../test/utils';
import { authApi } from '../api/endpoints';

vi.mock('../api/endpoints', () => ({
  authApi: { me: vi.fn() },
}));

const mockedMe = vi.mocked(authApi.me);

describe('ProtectedRoute', () => {
  beforeEach(() => vi.clearAllMocks());

  const routes = (
    <Routes>
      <Route path="/signin" element={<p>Sign in page</p>} />
      <Route path="/secret" element={<ProtectedRoute><p>Secret content</p></ProtectedRoute>} />
      <Route path="/admin" element={
        <ProtectedRoute roles={['Admin']}><p>Admin content</p></ProtectedRoute>
      } />
    </Routes>
  );

  it('redirects an anonymous visitor to the login page', async () => {
    renderWithProviders(routes, { route: '/secret' });

    expect(await screen.findByText('Sign in page')).toBeInTheDocument();
    expect(screen.queryByText('Secret content')).not.toBeInTheDocument();
  });

  it('renders the page for a signed-in user', async () => {
    signIn('Employee');
    mockedMe.mockResolvedValue(testUser('Employee'));

    renderWithProviders(routes, { route: '/secret' });

    expect(await screen.findByText('Secret content')).toBeInTheDocument();
  });

  it('blocks a signed-in user who does not hold the required role', async () => {
    signIn('Employee');
    mockedMe.mockResolvedValue(testUser('Employee'));

    renderWithProviders(routes, { route: '/admin' });

    expect(await screen.findByRole('alert')).toHaveTextContent(/do not have access/i);
    expect(screen.queryByText('Admin content')).not.toBeInTheDocument();
  });

  it('allows a user who does hold the required role', async () => {
    signIn('Admin');
    mockedMe.mockResolvedValue(testUser('Admin'));

    renderWithProviders(routes, { route: '/admin' });

    expect(await screen.findByText('Admin content')).toBeInTheDocument();
  });

  it('signs the user out when the API rejects their stored token', async () => {
    signIn('Employee');
    mockedMe.mockRejectedValue(new Error('401'));

    renderWithProviders(routes, { route: '/secret' });

    await waitFor(() => expect(screen.getByText('Sign in page')).toBeInTheDocument());
    expect(localStorage.getItem('smartdesk.token')).toBeNull();
  });
});
