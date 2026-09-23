import React from 'react';

export default function SearchFilterBar({ 
  searchValue, 
  onSearchChange, 
  onReset, 
  showingFrom, 
  showingTo, 
  totalCount,
  children 
}) {
  return (
    <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4 mb-6">
      <div className="flex flex-col md:flex-row items-stretch md:items-center gap-3 w-full md:w-auto">
        <div className="relative flex-1 md:w-64">
          <span className="absolute left-3 top-1/2 -translate-y-1/2 material-symbols-outlined text-text-secondary/60">
            search
          </span>
          <input
            type="text"
            placeholder="Search..."
            value={searchValue}
            onChange={(e) => onSearchChange(e.target.value)}
            className="w-full pl-10 pr-4 py-2 border border-border-neutral rounded-md bg-white text-body-md text-text placeholder:text-text-secondary/60 focus:outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-all"
          />
        </div>
        
        {children}
        
        {onReset && (searchValue || (children && React.Children.count(children) > 0)) && (
          <button
            onClick={onReset}
            className="text-body-sm font-heading font-bold text-text-secondary hover:text-text transition-colors whitespace-nowrap"
          >
            Reset Filters
          </button>
        )}
      </div>

      {(totalCount !== undefined) && (
        <div className="text-body-sm text-text-secondary whitespace-nowrap">
          Showing {showingFrom}–{showingTo} of {totalCount} results
        </div>
      )}
    </div>
  );
}
