import { describe, expect, it, vi, beforeEach } from 'vitest';
import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { TicketList } from './TicketList';
import { renderWithProviders, signIn, testUser } from '../test/utils';
import { adminApi, authApi, ticketsApi } from '../api/endpoints';
import type { PagedResult, TicketListItem } from '../types';

vi.mock('../api/endpoints', () => ({
  authApi: { me: vi.fn() },
  adminApi: { categories: vi.fn() },
  ticketsApi: { list: vi.fn() },
}));

const mockedList = vi.mocked(ticketsApi.list);
const mockedCategories = vi.mocked(adminApi.categories);
const mockedMe = vi.mocked(authApi.me);

function ticket(overrides: Partial<TicketListItem> = {}): TicketListItem {
  return {
    id: 1, ticketNumber: 'TKT-000001', title: 'VPN will not connect', categoryName: 'Network',
    status: 'New', priority: 'High', createdByName: 'Dev Employee', assignedToName: null,
    assignedToUserId: null, slaDueAt: '2026-06-02T00:00:00Z', slaState: 'OnTrack',
    isEscalated: false, createdAt: '2026-06-01T00:00:00Z', updatedAt: '2026-06-01T00:00:00Z',
    ...overrides,
  };
}

function page(items: TicketListItem[], overrides: Partial<PagedResult<TicketListItem>> = {}): PagedResult<TicketListItem> {
  return { items, page: 1, pageSize: 10, totalCount: items.length, totalPages: 1, ...overrides };
}

describe('TicketList', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    signIn('SupportManager');
    mockedMe.mockResolvedValue(testUser('SupportManager'));
    mockedCategories.mockResolvedValue([
      { id: 1, name: 'Network', description: null, defaultSlaHours: 8, isActive: true, ticketCount: 3 },
      { id: 2, name: 'Hardware', description: null, defaultSlaHours: 24, isActive: true, ticketCount: 1 },
    ]);
  });

  it('renders the tickets the API returned', async () => {
    mockedList.mockResolvedValue(page([
      ticket(),
      ticket({ id: 2, ticketNumber: 'TKT-000002', title: 'Printer jam', priority: 'Low', status: 'Assigned' }),
    ]));

    renderWithProviders(<TicketList />);

    expect(await screen.findByText('TKT-000001')).toBeInTheDocument();
    expect(screen.getByText('Printer jam')).toBeInTheDocument();
  });

  it('sends the search term to the API rather than filtering in the browser', async () => {
    const user = userEvent.setup();
    mockedList.mockResolvedValue(page([ticket()]));

    renderWithProviders(<TicketList />);
    await screen.findByText('TKT-000001');

    await user.type(screen.getByLabelText(/search/i), 'printer');

    // The input is debounced, so the request carries the whole term, not one per keystroke.
    await waitFor(() =>
      expect(mockedList).toHaveBeenLastCalledWith(expect.objectContaining({ search: 'printer' })),
    );
  });

  it('sends the status filter to the API and resets to the first page', async () => {
    const user = userEvent.setup();
    mockedList.mockResolvedValue(page([ticket()], { page: 3, totalPages: 5, totalCount: 42 }));

    renderWithProviders(<TicketList />);
    await screen.findByText('TKT-000001');

    await user.selectOptions(screen.getByLabelText(/^status$/i), 'Resolved');

    await waitFor(() =>
      expect(mockedList).toHaveBeenLastCalledWith(expect.objectContaining({ status: 'Resolved', page: 1 })),
    );
  });

  it('asks the API to sort when a sortable column header is clicked', async () => {
    const user = userEvent.setup();
    mockedList.mockResolvedValue(page([ticket()]));

    renderWithProviders(<TicketList />);
    await screen.findByText('TKT-000001');

    await user.click(screen.getByRole('button', { name: /priority/i }));

    await waitFor(() =>
      expect(mockedList).toHaveBeenLastCalledWith(expect.objectContaining({ sortBy: 'priority', sortDir: 'desc' })),
    );
  });

  it('pages forward through the results', async () => {
    const user = userEvent.setup();
    mockedList.mockResolvedValue(page([ticket()], { page: 1, totalPages: 3, totalCount: 25 }));

    renderWithProviders(<TicketList />);
    await screen.findByText('TKT-000001');

    await user.click(screen.getByRole('button', { name: /next/i }));

    await waitFor(() =>
      expect(mockedList).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2 })),
    );
  });

  it('shows an empty state rather than a blank table when nothing matches', async () => {
    mockedList.mockResolvedValue(page([]));

    renderWithProviders(<TicketList />);

    expect(await screen.findByText(/no tickets match your filters/i)).toBeInTheDocument();
  });

  it('shows an error with a retry when the API call fails', async () => {
    mockedList.mockRejectedValue({ isAxiosError: true, response: { status: 500, data: { detail: 'Boom' } } });

    renderWithProviders(<TicketList />);

    const alert = await screen.findByRole('alert');
    expect(within(alert).getByText('Boom')).toBeInTheDocument();
    expect(within(alert).getByRole('button', { name: /try again/i })).toBeInTheDocument();
  });
});
