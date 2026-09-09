import { describe, expect, it, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { SignIn } from './SignIn';
import { renderWithProviders, testUser } from '../../test/utils';
import { authApi } from '../../api/endpoints';

vi.mock('../../api/endpoints', () => ({
  authApi: { login: vi.fn(), me: vi.fn(), register: vi.fn() },
}));

const mockedLogin = vi.mocked(authApi.login);

describe('Sign in page', () => {
  beforeEach(() => vi.clearAllMocks());

  it('shows validation messages and does not call the API when fields are empty', async () => {
    const user = userEvent.setup();
    renderWithProviders(<SignIn />);

    await user.click(screen.getByRole('button', { name: /sign in/i }));

    expect(await screen.findByText(/enter your email address/i)).toBeInTheDocument();
    expect(screen.getByText(/enter your password/i)).toBeInTheDocument();
    expect(mockedLogin).not.toHaveBeenCalled();
  });

  it('rejects a malformed email address before sending a request', async () => {
    const user = userEvent.setup();
    renderWithProviders(<SignIn />);

    await user.type(screen.getByLabelText(/email address/i), 'not-an-email');
    await user.type(screen.getByLabelText(/password/i), 'Password123!');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    expect(await screen.findByText(/valid email address/i)).toBeInTheDocument();
    expect(mockedLogin).not.toHaveBeenCalled();
  });

  it('submits valid credentials to the API', async () => {
    const user = userEvent.setup();
    mockedLogin.mockResolvedValue({
      token: 'jwt-token',
      expiresAt: '2026-12-31T00:00:00Z',
      user: testUser('SupportManager'),
    });

    renderWithProviders(<SignIn />);

    await user.type(screen.getByLabelText(/email address/i), 'manager@smartdesk.local');
    await user.type(screen.getByLabelText(/password/i), 'Password123!');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() =>
      expect(mockedLogin).toHaveBeenCalledWith('manager@smartdesk.local', 'Password123!'),
    );
    // The token is persisted so a refresh keeps the user signed in.
    await waitFor(() => expect(localStorage.getItem('smartdesk.token')).toBe('jwt-token'));
  });

  it('surfaces the API error message when the credentials are wrong', async () => {
    const user = userEvent.setup();
    mockedLogin.mockRejectedValue({
      isAxiosError: true,
      response: { status: 403, data: { detail: 'Invalid email address or password.' } },
    });

    renderWithProviders(<SignIn />);

    await user.type(screen.getByLabelText(/email address/i), 'manager@smartdesk.local');
    await user.type(screen.getByLabelText(/password/i), 'wrong');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent(/invalid email address or password/i);
  });
});
