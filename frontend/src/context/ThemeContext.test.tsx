import { describe, expect, it, beforeEach, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ThemeProvider, useTheme } from './ThemeContext';
import { ThemeToggle } from '../components/ThemeToggle';

function Probe() {
  const { theme } = useTheme();
  return <p data-testid="theme">{theme}</p>;
}

function renderWithTheme() {
  return render(
    <ThemeProvider>
      <ThemeToggle />
      <Probe />
    </ThemeProvider>,
  );
}

/** Lets a test pretend the operating system is in dark mode. */
function mockSystemDark(matches: boolean) {
  window.matchMedia = ((query: string) => ({
    matches, media: query, onchange: null,
    addEventListener: () => {}, removeEventListener: () => {},
    addListener: () => {}, removeListener: () => {}, dispatchEvent: () => false,
  })) as unknown as typeof window.matchMedia;
}

describe('Theme system', () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove('dark');
    mockSystemDark(false);
    vi.clearAllMocks();
  });

  it('follows the operating system when the visitor has no stored preference', () => {
    mockSystemDark(true);
    renderWithTheme();

    expect(screen.getByTestId('theme')).toHaveTextContent('dark');
    expect(document.documentElement).toHaveClass('dark');
  });

  it('uses light when the operating system prefers light', () => {
    renderWithTheme();

    expect(screen.getByTestId('theme')).toHaveTextContent('light');
    expect(document.documentElement).not.toHaveClass('dark');
  });

  it('honours a stored preference over the operating system', () => {
    localStorage.setItem('smartdesk.theme', 'dark');
    mockSystemDark(false);

    renderWithTheme();

    expect(screen.getByTestId('theme')).toHaveTextContent('dark');
  });

  it('toggles the theme and applies the class to the document', async () => {
    const user = userEvent.setup();
    renderWithTheme();

    expect(document.documentElement).not.toHaveClass('dark');

    await user.click(screen.getByRole('button', { name: /switch to dark theme/i }));

    expect(screen.getByTestId('theme')).toHaveTextContent('dark');
    expect(document.documentElement).toHaveClass('dark');
  });

  it('persists the choice so it survives a reload', async () => {
    const user = userEvent.setup();
    renderWithTheme();

    await user.click(screen.getByRole('button', { name: /switch to dark theme/i }));

    expect(localStorage.getItem('smartdesk.theme')).toBe('dark');
  });

  it('sets colorScheme so the browser themes its own controls and scrollbars', async () => {
    const user = userEvent.setup();
    renderWithTheme();

    await user.click(screen.getByRole('button', { name: /switch to dark theme/i }));
    expect(document.documentElement.style.colorScheme).toBe('dark');
  });

  it('does not crash when matchMedia is unavailable', () => {
    // Some embedded browsers omit it entirely.
    (window as { matchMedia?: unknown }).matchMedia = undefined;

    expect(() => renderWithTheme()).not.toThrow();
    expect(screen.getByTestId('theme')).toHaveTextContent('light');
  });
});
