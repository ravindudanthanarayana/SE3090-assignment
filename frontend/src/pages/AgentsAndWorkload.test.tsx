import { describe, expect, it, vi, beforeEach } from 'vitest';
import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AgentsAndWorkload } from './StaffPages';
import { renderWithProviders, signIn, testUser } from '../test/utils';
import { adminApi, assignmentApi, authApi } from '../api/endpoints';
import type { Category, SupportAgent } from '../types';

vi.mock('../api/endpoints', () => ({
  authApi: { me: vi.fn() },
  adminApi: { categories: vi.fn() },
  assignmentApi: { supportAgents: vi.fn(), upsertSkill: vi.fn(), deleteSkill: vi.fn() },
  reportsApi: {},
  ticketsApi: {},
}));

const mockedAgents = vi.mocked(assignmentApi.supportAgents);
const mockedUpsert = vi.mocked(assignmentApi.upsertSkill);
const mockedDelete = vi.mocked(assignmentApi.deleteSkill);
const mockedCategories = vi.mocked(adminApi.categories);
const mockedMe = vi.mocked(authApi.me);

const category = (id: number, name: string): Category => ({
  id, name, description: null, defaultSlaHours: 8, isActive: true, ticketCount: 0,
});

const priya: SupportAgent = {
  userId: 3, fullName: 'Priya Network', email: 'agent1@smartdesk.local', department: 'IT', isActive: true,
  skills: [{ id: 11, categoryId: 1, categoryName: 'Network', proficiencyLevel: 5 }],
  workload: {
    userId: 3, fullName: 'Priya Network', openCount: 2, inProgressCount: 1,
    atRiskCount: 0, breachedCount: 0, resolvedLast30Days: 4,
  },
};

describe('AgentsAndWorkload skill editing', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockedAgents.mockResolvedValue([priya]);
    mockedCategories.mockResolvedValue([category(1, 'Network'), category(2, 'Hardware')]);
  });

  it('hides Edit skills from a manager, because only an admin may change skills', async () => {
    signIn('SupportManager');
    mockedMe.mockResolvedValue(testUser('SupportManager'));

    renderWithProviders(<AgentsAndWorkload />);

    expect(await screen.findByText('Priya Network')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /edit skills/i })).not.toBeInTheDocument();
  });

  it('lets an admin add a skill, offering only categories the agent does not have', async () => {
    const user = userEvent.setup();
    signIn('Admin');
    mockedMe.mockResolvedValue(testUser('Admin'));
    mockedUpsert.mockResolvedValue({ id: 12, categoryId: 2, categoryName: 'Hardware', proficiencyLevel: 4 });

    renderWithProviders(<AgentsAndWorkload />);
    await user.click(await screen.findByRole('button', { name: /edit skills/i }));

    const dialog = screen.getByRole('dialog', { name: /skills: priya network/i });
    const categorySelect = await within(dialog).findByRole('combobox', { name: /^category$/i });
    expect(within(categorySelect).queryByRole('option', { name: 'Network' })).not.toBeInTheDocument();

    await user.selectOptions(categorySelect, 'Hardware');
    await user.selectOptions(within(dialog).getByRole('combobox', { name: /proficiency level/i }), '4');
    await user.click(within(dialog).getByRole('button', { name: /^add$/i }));

    expect(mockedUpsert).toHaveBeenCalledWith(3, { categoryId: 2, proficiencyLevel: 4 });
    expect(await within(dialog).findByText('Hardware')).toBeInTheDocument();
  });

  it('lets an admin remove a skill', async () => {
    const user = userEvent.setup();
    signIn('Admin');
    mockedMe.mockResolvedValue(testUser('Admin'));
    mockedDelete.mockResolvedValue({} as never);

    renderWithProviders(<AgentsAndWorkload />);
    await user.click(await screen.findByRole('button', { name: /edit skills/i }));

    const dialog = screen.getByRole('dialog');
    await user.click(within(dialog).getByRole('button', { name: /remove network/i }));

    expect(mockedDelete).toHaveBeenCalledWith(3, 11);
    expect(await within(dialog).findByText(/no skills recorded yet/i)).toBeInTheDocument();
  });
});
