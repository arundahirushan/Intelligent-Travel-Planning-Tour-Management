import React from 'react';

export default function Input({ label, error, className = '', ...props }) {
  const id = props.id || props.name;
  
  return (
    <div className={`flex flex-col mb-4 ${className}`}>
      {label && (
        <label htmlFor={id} className="mb-1.5 font-heading text-sm font-semibold text-text-secondary">
          {label}
        </label>
      )}
      <input
        id={id}
        className={`bg-white border rounded-md px-4 py-2.5 font-body text-text placeholder:text-text-secondary/60 outline-none transition-all
          ${error ? 'border-red-500 focus:ring-1 focus:ring-red-500' : 'border-border-neutral focus:border-primary focus:ring-1 focus:ring-primary'}
        `}
        {...props}
      />
      {error && (
        <span className="mt-1.5 text-xs text-red-500 font-body">
          {error}
        </span>
      )}
    </div>
  );
}
