import React from 'react';

export default function Button({ children, variant = 'primary', className = '', ...props }) {
  const baseClasses = 'inline-flex items-center justify-center font-heading text-xs font-bold uppercase tracking-widest rounded-pill transition-all duration-200';
  const sizeClasses = 'px-6 py-2.5 shadow-soft';
  
  const variants = {
    primary: 'bg-primary text-white hover:bg-primary-dark',
    secondary: 'bg-transparent text-text border border-border-neutral hover:bg-surface-neutral shadow-none',
  };

  return (
    <button 
      className={`${baseClasses} ${sizeClasses} ${variants[variant]} ${className}`}
      {...props}
    >
      {children}
    </button>
  );
}
