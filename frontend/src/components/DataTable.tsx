import type { ReactNode } from 'react';
import { EmptyState, Spinner } from './Ui';

export interface Column<T> {
  key: string;
  header: string;
  /** Set when this column maps to a sortBy value the API accepts. */
  sortable?: boolean;
  render: (row: T) => ReactNode;
  className?: string;
}

interface DataTableProps<T> {
  columns: Column<T>[];
  rows: T[];
  rowKey: (row: T) => string | number;
  loading?: boolean;
  emptyTitle?: string;
  emptyHint?: string;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
  onSort?: (key: string) => void;
  onRowClick?: (row: T) => void;
  caption?: string;
}

/**
 * The one table in the app. Sorting is delegated upward so the server does the work; the
 * component only renders the current state and reports clicks.
 *
 * The wrapper scrolls horizontally rather than letting a wide table push the page sideways.
 */
export function DataTable<T>({
  columns, rows, rowKey, loading, emptyTitle = 'No results', emptyHint,
  sortBy, sortDir, onSort, onRowClick, caption,
}: DataTableProps<T>) {
  if (!loading && rows.length === 0) return <EmptyState title={emptyTitle} hint={emptyHint} />;

  return (
    <div className="relative overflow-x-auto rounded-2xl border border-line bg-surface shadow-card">
      {loading && (
        <div className="absolute inset-0 z-10 flex items-center justify-center bg-surface/75 backdrop-blur-[1px]">
          <Spinner />
        </div>
      )}

      <table className="w-full min-w-[640px] text-left text-sm text-fg-muted">
        {caption && <caption className="sr-only">{caption}</caption>}
        <thead className="border-b border-line bg-surface-2 text-xs uppercase tracking-wide text-fg-subtle">
          <tr>
            {columns.map((column) => {
              const active = sortBy === column.key;
              return (
                <th key={column.key} scope="col" className={`px-4 py-3 font-medium ${column.className ?? ''}`}
                    aria-sort={active ? (sortDir === 'asc' ? 'ascending' : 'descending') : undefined}>
                  {column.sortable && onSort ? (
                    <button
                      type="button"
                      onClick={() => onSort(column.key)}
                      className="inline-flex items-center gap-1 transition hover:text-fg"
                    >
                      {column.header}
                      <span aria-hidden="true" className={active ? 'text-accent' : 'text-fg-subtle/40'}>
                        {active && sortDir === 'asc' ? '▲' : '▼'}
                      </span>
                    </button>
                  ) : (
                    column.header
                  )}
                </th>
              );
            })}
          </tr>
        </thead>
        <tbody className="divide-y divide-line">
          {rows.map((row) => (
            <tr
              key={rowKey(row)}
              onClick={onRowClick ? () => onRowClick(row) : undefined}
              className={onRowClick ? 'cursor-pointer transition hover:bg-surface-hover' : undefined}
            >
              {columns.map((column) => (
                <td key={column.key} className={`px-4 py-3 align-middle ${column.className ?? ''}`}>
                  {column.render(row)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
