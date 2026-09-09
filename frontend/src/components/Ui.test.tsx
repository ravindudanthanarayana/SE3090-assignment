import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AsyncState, PriorityBadge, SlaBadge, StatusBadge } from './Ui';

describe('AsyncState', () => {
  it('shows a spinner while the first load is in flight', () => {
    render(
      <AsyncState loading error={null} data={null}>
        {() => <p>content</p>}
      </AsyncState>,
    );

    expect(screen.getByRole('status')).toHaveTextContent(/loading/i);
    expect(screen.queryByText('content')).not.toBeInTheDocument();
  });

  it('shows the error and offers a retry', async () => {
    const onRetry = vi.fn();
    const user = userEvent.setup();

    render(
      <AsyncState loading={false} error="Could not reach the server." data={null} onRetry={onRetry}>
        {() => <p>content</p>}
      </AsyncState>,
    );

    expect(screen.getByRole('alert')).toHaveTextContent(/could not reach the server/i);

    await user.click(screen.getByRole('button', { name: /try again/i }));
    expect(onRetry).toHaveBeenCalledOnce();
  });

  it('shows the empty state when the data arrived but is empty', () => {
    render(
      <AsyncState
        loading={false} error={null} data={[]}
        isEmpty={(rows: unknown[]) => rows.length === 0}
        emptyTitle="No tickets yet" emptyHint="Raise one to get started."
      >
        {() => <p>content</p>}
      </AsyncState>,
    );

    expect(screen.getByText('No tickets yet')).toBeInTheDocument();
    expect(screen.getByText('Raise one to get started.')).toBeInTheDocument();
    expect(screen.queryByText('content')).not.toBeInTheDocument();
  });

  it('renders the content once data is present', () => {
    render(
      <AsyncState loading={false} error={null} data={{ name: 'SmartDesk' }}>
        {(data) => <p>{data.name}</p>}
      </AsyncState>,
    );

    expect(screen.getByText('SmartDesk')).toBeInTheDocument();
  });
});

describe('Badges', () => {
  it('never relies on colour alone - each badge carries readable text', () => {
    render(
      <>
        <StatusBadge status="InProgress" />
        <PriorityBadge priority="Critical" />
        <SlaBadge state="Breached" />
      </>,
    );

    expect(screen.getByText('In progress')).toBeInTheDocument();
    expect(screen.getByText('Critical')).toBeInTheDocument();
    expect(screen.getByText('Breached')).toBeInTheDocument();
  });
});
