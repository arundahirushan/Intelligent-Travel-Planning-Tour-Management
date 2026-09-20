import React from 'react';

export default function ErrorBanner({ message, className = '' }) {
  if (!message) return null;
  
  return (
    <div className={`bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-md flex items-start gap-3 ${className}`}>
      <span className="material-symbols-outlined text-red-500 mt-0.5">error</span>
      <p className="font-body text-sm mt-0.5">{message}</p>
    </div>
  );
}
