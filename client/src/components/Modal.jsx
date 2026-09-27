import React, { useEffect, useRef } from 'react';
import { createPortal } from 'react-dom';

export default function Modal({ isOpen, onClose, title, children, size = 'md' }) {
  const closeButtonRef = useRef(null);

  useEffect(() => {
    const handleEscape = (e) => {
      if (e.key === 'Escape') onClose();
    };

    if (isOpen) {
      document.addEventListener('keydown', handleEscape);
      // Prevent body scrolling
      document.body.style.overflow = 'hidden';
      // Auto-focus close button for accessibility
      setTimeout(() => closeButtonRef.current?.focus(), 10);
    }

    return () => {
      document.removeEventListener('keydown', handleEscape);
      document.body.style.overflow = 'unset';
    };
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  const sizeClasses = {
    sm: 'max-w-sm',
    md: 'max-w-lg',
    lg: 'max-w-2xl',
  };

  return createPortal(
    <div className="fixed inset-0 z-50 flex items-center justify-center p-[var(--space-md)]">
      {/* Backdrop */}
      <div 
        className="absolute inset-0 bg-text/40 backdrop-blur-sm transition-opacity"
        onClick={onClose}
        aria-hidden="true"
      />
      
      {/* Modal Dialog */}
      <div 
        className={`relative w-full ${sizeClasses[size]} bg-white rounded-xl shadow-soft flex flex-col max-h-[90vh]`}
        role="dialog"
        aria-modal="true"
        aria-labelledby="modal-title"
      >
        {/* Header */}
        <div className="flex items-center justify-between p-[var(--space-lg)] border-b border-border-neutral shrink-0">
          <h2 id="modal-title" className="text-headline-sm font-heading font-bold text-text">
            {title}
          </h2>
          <button
            ref={closeButtonRef}
            onClick={onClose}
            className="w-8 h-8 flex items-center justify-center text-text-secondary hover:text-text hover:bg-surface-neutral rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-primary"
            aria-label="Close modal"
          >
            <span className="material-symbols-outlined">close</span>
          </button>
        </div>

        {/* Content */}
        <div className="p-[var(--space-lg)] overflow-y-auto">
          {children}
        </div>
      </div>
    </div>,
    document.body
  );
}
