import { useCallback, useEffect, useState } from 'react';
import { errorMessage } from '../api/client';

interface ApiResource<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
  refetch: () => void;
}

/**
 * Fetches server data and owns the loading, error and refetch states that every page needs.
 *
 * This is the whole reason we do not need a data-fetching library: one small hook removes the
 * repeated useState/useEffect boilerplate, and pages stay readable.
 *
 * `deps` behaves like a useEffect dependency list - pass the filter values a page is watching.
 */
export function useApiResource<T>(fetcher: () => Promise<T>, deps: unknown[] = []): ApiResource<T> {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  const refetch = useCallback(() => setReloadKey((k) => k + 1), []);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    fetcher()
      .then((result) => {
        // A response that arrives after the inputs changed must not overwrite newer data.
        if (!cancelled) setData(result);
      })
      .catch((err) => {
        if (!cancelled) setError(errorMessage(err));
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...deps, reloadKey]);

  return { data, loading, error, refetch };
}

/** Debounces a value, so typing in a search box does not fire a request per keystroke. */
export function useDebounced<T>(value: T, delayMs = 350): T {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(value), delayMs);
    return () => window.clearTimeout(timer);
  }, [value, delayMs]);

  return debounced;
}
