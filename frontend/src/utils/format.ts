/** Shared display helpers, so dates and JSON look the same everywhere. */

export function formatDateTime(iso: string | null | undefined): string {
  if (!iso) return '—';
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return '—';
  return date.toLocaleString(undefined, {
    year: 'numeric', month: 'short', day: '2-digit', hour: '2-digit', minute: '2-digit',
  });
}

export function formatRelative(iso: string | null | undefined): string {
  if (!iso) return '—';
  const then = new Date(iso).getTime();
  if (Number.isNaN(then)) return '—';

  const diffMinutes = Math.round((then - Date.now()) / 60000);
  const absolute = Math.abs(diffMinutes);

  if (absolute < 60) return relative(diffMinutes, 'minute');
  if (absolute < 60 * 24) return relative(Math.round(diffMinutes / 60), 'hour');
  return relative(Math.round(diffMinutes / (60 * 24)), 'day');
}

function relative(value: number, unit: Intl.RelativeTimeFormatUnit) {
  return new Intl.RelativeTimeFormat(undefined, { numeric: 'auto' }).format(value, unit);
}

export function formatHours(hours: number): string {
  if (hours < 0) return `${Math.abs(hours).toFixed(1)}h overdue`;
  if (hours < 1) return `${Math.round(hours * 60)}m left`;
  return `${hours.toFixed(1)}h left`;
}

/** Pretty-prints the JSON the agents persisted, falling back to the raw text if it will not parse. */
export function prettyJson(raw: string | null | undefined): string {
  if (!raw) return '';
  try {
    return JSON.stringify(JSON.parse(raw), null, 2);
  } catch {
    return raw;
  }
}

export function safeParse<T>(raw: string | null | undefined): T | null {
  if (!raw) return null;
  try {
    return JSON.parse(raw) as T;
  } catch {
    return null;
  }
}
