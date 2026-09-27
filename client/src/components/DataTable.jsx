import React from 'react';
import LoadingSpinner from './LoadingSpinner';

export default function DataTable({ 
  columns, 
  rows, 
  page, 
  pageSize, 
  totalCount, 
  onPageChange, 
  isLoading, 
  emptyState 
}) {
  const totalPages = Math.ceil(totalCount / pageSize) || 1;

  if (isLoading) {
    return (
      <div className="w-full bg-white border border-border-neutral rounded-xl overflow-hidden py-12 flex justify-center">
        <LoadingSpinner size="md" />
      </div>
    );
  }

  if (rows.length === 0 && emptyState) {
    return emptyState;
  }

  return (
    <div className="w-full bg-white border border-border-neutral rounded-xl overflow-hidden shadow-soft flex flex-col">
      <div className="overflow-x-auto">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="bg-canvas border-b border-border-neutral">
              {columns.map((col, i) => (
                <th key={col.key || i} className="p-[var(--space-md)] font-heading text-label-uppercase text-text-secondary whitespace-nowrap">
                  {col.label}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.map((row, rowIndex) => (
              <tr key={row.id || rowIndex} className="border-b border-border-neutral last:border-b-0 hover:bg-surface-neutral/40 transition-colors">
                {columns.map((col, colIndex) => (
                  <td key={col.key || colIndex} className="p-[var(--space-md)] font-body text-body-md text-text">
                    {col.render ? col.render(row) : row[col.key]}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {onPageChange && (
        <div className="flex items-center justify-between p-[var(--space-md)] border-t border-border-neutral bg-canvas mt-auto">
          <button
            onClick={() => onPageChange(page - 1)}
            disabled={page <= 1}
            className="px-4 py-2 text-label-button text-text border border-border-neutral rounded-md bg-white hover:bg-surface-neutral disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            Previous
          </button>
          <span className="text-body-sm text-text-secondary">
            Page {page} of {totalPages}
          </span>
          <button
            onClick={() => onPageChange(page + 1)}
            disabled={page >= totalPages}
            className="px-4 py-2 text-label-button text-text border border-border-neutral rounded-md bg-white hover:bg-surface-neutral disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            Next
          </button>
        </div>
      )}
    </div>
  );
}
