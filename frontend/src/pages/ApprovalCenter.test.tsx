import { describe, expect, it, vi, beforeEach } from 'vitest';
import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ApprovalCenter } from './ApprovalCenter';
import { renderWithProviders, signIn, testUser } from '../test/utils';
import { aiApi, authApi } from '../api/endpoints';
import type { Approval, PagedResult } from '../types';

vi.mock('../api/endpoints', () => ({
  authApi: { me: vi.fn() },
  aiApi: { approvals: vi.fn(), decide: vi.fn() },
}));

const mockedApprovals = vi.mocked(aiApi.approvals);
const mockedDecide = vi.mocked(aiApi.decide);
const mockedMe = vi.mocked(authApi.me);

const approval: Approval = {
  id: 7, workflowId: 3, ticketId: 21, ticketNumber: 'TKT-000021',
  ticketTitle: 'VPN rejects my login',
  actionType: 'Escalate',
  proposedActionJson: '{"actionType":"Escalate","ticketId":21,"description":"SLA at risk"}',
  reason: 'The SLA is at risk and the requester is fully blocked.',
  riskLevel: 'High', status: 'Pending',
  requestedAt: '2026-06-01T10:00:00Z', decidedByName: null, decidedAt: null, decisionNote: null,
};

const page = (items: Approval[]): PagedResult<Approval> => ({
  items, page: 1, pageSize: 10, totalCount: items.length, totalPages: 1,
});

describe('ApprovalCenter', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    signIn('SupportManager');
    mockedMe.mockResolvedValue(testUser('SupportManager'));
  });

  it('shows the pending recommendation with the agent reasoning', async () => {
    mockedApprovals.mockResolvedValue(page([approval]));

    renderWithProviders(<ApprovalCenter />);

    expect(await screen.findByText('TKT-000021')).toBeInTheDocument();
    expect(screen.getByText(/the sla is at risk/i)).toBeInTheDocument();
    expect(screen.getByText(/high risk/i)).toBeInTheDocument();
  });

  it('offers all three decisions the spec requires', async () => {
    mockedApprovals.mockResolvedValue(page([approval]));

    renderWithProviders(<ApprovalCenter />);
    await screen.findByText('TKT-000021');

    expect(screen.getByRole('button', { name: /^approve$/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /request revision/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^reject$/i })).toBeInTheDocument();
  });

  it('sends the approval decision to the API with the note', async () => {
    const user = userEvent.setup();
    mockedApprovals.mockResolvedValue(page([approval]));
    mockedDecide.mockResolvedValue({ ...approval, status: 'Approved' });

    renderWithProviders(<ApprovalCenter />);
    await screen.findByText('TKT-000021');

    await user.click(screen.getByRole('button', { name: /^approve$/i }));

    // A confirmation step, because approving actually changes the ticket.
    const dialog = await screen.findByRole('dialog');
    expect(dialog).toHaveTextContent(/apply it to the ticket/i);

    await user.type(screen.getByLabelText(/note/i), 'Agreed.');
    await user.click(within(dialog).getByRole('button', { name: /^approve$/i }));

    await waitFor(() => expect(mockedDecide).toHaveBeenCalledWith(7, 'Approved', 'Agreed.'));
  });

  it('tells the user plainly that rejecting changes nothing', async () => {
    const user = userEvent.setup();
    mockedApprovals.mockResolvedValue(page([approval]));

    renderWithProviders(<ApprovalCenter />);
    await screen.findByText('TKT-000021');

    await user.click(screen.getByRole('button', { name: /^reject$/i }));

    const dialog = await screen.findByRole('dialog');
    expect(dialog).toHaveTextContent(/nothing will be changed on the ticket/i);
  });

  it('surfaces a 403 from the API instead of pretending the decision worked', async () => {
    const user = userEvent.setup();
    mockedApprovals.mockResolvedValue(page([approval]));
    mockedDecide.mockRejectedValue({
      isAxiosError: true,
      response: { status: 403, data: { detail: 'Only a support manager or administrator can decide an AI approval.' } },
    });

    renderWithProviders(<ApprovalCenter />);
    await screen.findByText('TKT-000021');

    await user.click(screen.getByRole('button', { name: /^approve$/i }));
    const dialog = await screen.findByRole('dialog');
    await user.click(within(dialog).getByRole('button', { name: /^approve$/i }));

    expect(await within(dialog).findByRole('alert')).toHaveTextContent(/only a support manager/i);
  });

  it('shows an empty state when nothing is waiting', async () => {
    mockedApprovals.mockResolvedValue(page([]));

    renderWithProviders(<ApprovalCenter />);

    expect(await screen.findByText(/nothing is waiting for your decision/i)).toBeInTheDocument();
  });
});
