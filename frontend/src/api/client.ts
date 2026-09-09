import axios, { AxiosError } from 'axios';

/**
 * The single axios instance every page uses.
 *
 * The base URL comes from VITE_API_URL so the same build can point at localhost or the deployed
 * API without a code change. The token is attached by an interceptor rather than by each caller,
 * and a 401 clears the session in one place.
 */
export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5299',
  headers: { 'Content-Type': 'application/json' },
});

const TOKEN_KEY = 'smartdesk.token';
const USER_KEY = 'smartdesk.user';

export const tokenStorage = {
  get: () => localStorage.getItem(TOKEN_KEY),
  set: (token: string) => localStorage.setItem(TOKEN_KEY, token),
  clear: () => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  },
  getUser: () => {
    const raw = localStorage.getItem(USER_KEY);
    try {
      return raw ? JSON.parse(raw) : null;
    } catch {
      // A corrupted entry must not break the whole app on load.
      return null;
    }
  },
  setUser: (user: unknown) => localStorage.setItem(USER_KEY, JSON.stringify(user)),
};

api.interceptors.request.use((config) => {
  const token = tokenStorage.get();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

let onUnauthorized: (() => void) | null = null;

/** Lets AuthProvider react to an expired or revoked token without importing router internals here. */
export function setUnauthorizedHandler(handler: () => void) {
  onUnauthorized = handler;
}

api.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    if (error.response?.status === 401) {
      tokenStorage.clear();
      onUnauthorized?.();
    }
    return Promise.reject(error);
  },
);

/**
 * Turns any failure into a readable sentence. The API returns RFC 7807 ProblemDetails, so the
 * useful text is in `detail`; network failures have no response at all.
 */
export function errorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const problem = error.response?.data as { detail?: string; title?: string } | undefined;
    if (problem?.detail) return problem.detail;
    if (problem?.title) return problem.title;
    if (error.response?.status === 403) return 'You do not have permission to do that.';
    if (!error.response) return 'Could not reach the server. Check that the API is running.';
    return `Request failed with status ${error.response.status}.`;
  }
  return error instanceof Error ? error.message : 'An unexpected error occurred.';
}
