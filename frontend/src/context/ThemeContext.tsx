import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';

type Theme = 'light' | 'dark';

interface ThemeState {
  theme: Theme;
  toggle: () => void;
  setTheme: (theme: Theme) => void;
}

const STORAGE_KEY = 'smartdesk.theme';
const ThemeContext = createContext<ThemeState | undefined>(undefined);

function readStored(): Theme | null {
  try {
    const value = localStorage.getItem(STORAGE_KEY);
    return value === 'dark' || value === 'light' ? value : null;
  } catch {
    // Private browsing can throw on localStorage access.
    return null;
  }
}

function systemPrefersDark(): boolean {
  return typeof window !== 'undefined' && window.matchMedia?.('(prefers-color-scheme: dark)').matches;
}

function apply(theme: Theme) {
  const root = document.documentElement;
  root.classList.toggle('dark', theme === 'dark');
  // Tells the browser to theme its own scrollbars, form controls and autofill.
  root.style.colorScheme = theme;
}

/**
 * Light/dark theme for the whole product.
 *
 * The initial value is resolved in an inline script in index.html so the correct theme is on
 * <html> before first paint; this provider reads the same sources and keeps them in sync.
 * An explicit choice is remembered; with no choice stored, we follow the operating system
 * and keep following it if the user changes it.
 */
export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setThemeState] = useState<Theme>(() =>
    readStored() ?? (systemPrefersDark() ? 'dark' : 'light'),
  );

  useEffect(() => apply(theme), [theme]);

  // Follow the OS only while the user has not expressed a preference of their own.
  useEffect(() => {
    if (readStored()) return;

    // Guarded: some embedded browsers and test environments do not implement matchMedia,
    // and a missing API must not take the whole app down.
    const media = window.matchMedia?.('(prefers-color-scheme: dark)');
    if (!media?.addEventListener) return;

    const onChange = (event: MediaQueryListEvent) => setThemeState(event.matches ? 'dark' : 'light');

    media.addEventListener('change', onChange);
    return () => media.removeEventListener('change', onChange);
  }, []);

  const setTheme = useCallback((next: Theme) => {
    setThemeState(next);
    try {
      localStorage.setItem(STORAGE_KEY, next);
    } catch {
      // Not fatal: the theme still applies for this session.
    }
  }, []);

  const toggle = useCallback(
    () => setTheme(theme === 'dark' ? 'light' : 'dark'),
    [theme, setTheme],
  );

  const value = useMemo(() => ({ theme, toggle, setTheme }), [theme, toggle, setTheme]);

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme(): ThemeState {
  const context = useContext(ThemeContext);
  if (!context) throw new Error('useTheme must be used inside a ThemeProvider.');
  return context;
}
