import React from 'react';

export default function ProgressBar({ percentage, colorClass = 'bg-primary', label }) {
  const clamped = Math.max(0, Math.min(100, percentage || 0));

  return (
    <div className="flex items-center gap-3 w-full">
      <div className="flex-1 h-1.5 bg-surface-neutral rounded-pill overflow-hidden">
        <div 
          className={`h-full ${colorClass} rounded-pill transition-all duration-500 ease-out`}
          style={{ width: `${clamped}%` }}
        />
      </div>
      {label && (
        <span className="text-body-sm text-text-secondary whitespace-nowrap">
          {label}
        </span>
      )}
    </div>
  );
}
