import { Button } from './Ui';

interface PaginationProps {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  onPageChange: (page: number) => void;
}

/** Server-side pagination controls. Disabled rather than hidden at the ends, so the layout is stable. */
export function Pagination({ page, pageSize, totalCount, totalPages, onPageChange }: PaginationProps) {
  if (totalCount === 0) return null;

  const first = (page - 1) * pageSize + 1;
  const last = Math.min(page * pageSize, totalCount);

  return (
    <nav className="flex flex-wrap items-center justify-between gap-3 py-3" aria-label="Pagination">
      <p className="text-sm text-fg-subtle">
        Showing <span className="font-medium">{first}</span>–<span className="font-medium">{last}</span> of{' '}
        <span className="font-medium">{totalCount}</span>
      </p>
      <div className="flex items-center gap-2">
        <Button variant="secondary" onClick={() => onPageChange(page - 1)} disabled={page <= 1}>
          Previous
        </Button>
        <span className="text-sm text-fg-subtle" aria-live="polite">
          Page {page} of {Math.max(totalPages, 1)}
        </span>
        <Button variant="secondary" onClick={() => onPageChange(page + 1)} disabled={page >= totalPages}>
          Next
        </Button>
      </div>
    </nav>
  );
}
