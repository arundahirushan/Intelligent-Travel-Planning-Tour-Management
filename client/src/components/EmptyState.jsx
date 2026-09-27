import React from 'react';

export default function EmptyState({ icon = 'inbox', title, description, action }) {
  return (
    <div className="flex flex-col items-center justify-center text-center p-[var(--space-xl)] bg-white border border-border-neutral rounded-xl border-dashed">
      <span className="material-symbols-outlined text-[3rem] text-text-secondary/40 mb-4">
        {icon}
      </span>
      <h3 className="text-headline-sm font-heading font-bold text-text-secondary mb-2">
        {title}
      </h3>
      {description && (
        <p className="text-body-md text-text-secondary max-w-sm mx-auto mb-6">
          {description}
        </p>
      )}
      {action && (
        <div>
          {action}
        </div>
      )}
    </div>
  );
}
