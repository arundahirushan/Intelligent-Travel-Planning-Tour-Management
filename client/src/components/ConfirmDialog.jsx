import React, { useState } from 'react';
import Modal from './Modal';
import Button from './Button';
import LoadingSpinner from './LoadingSpinner';

export default function ConfirmDialog({ 
  isOpen, 
  onClose, 
  onConfirm, 
  title, 
  message, 
  confirmLabel = 'Confirm', 
  isDanger = false 
}) {
  const [loading, setLoading] = useState(false);

  const handleConfirm = async () => {
    setLoading(true);
    try {
      await onConfirm();
    } finally {
      setLoading(false);
      onClose();
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={title} size="sm">
      <p className="text-body-md text-text-secondary mb-[var(--space-xl)]">
        {message}
      </p>
      
      <div className="flex items-center justify-end gap-3">
        <Button variant="secondary" onClick={onClose} disabled={loading}>
          Cancel
        </Button>
        <button
          onClick={handleConfirm}
          disabled={loading}
          className={`inline-flex items-center justify-center font-heading text-xs font-bold uppercase tracking-widest rounded-pill transition-all duration-200 px-6 py-2.5 shadow-soft text-white focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed
            ${isDanger 
              ? 'bg-status-danger hover:bg-red-700 focus:ring-status-danger' 
              : 'bg-primary hover:bg-primary-dark focus:ring-primary'
            }`}
        >
          {loading ? <LoadingSpinner size="sm" /> : confirmLabel}
        </button>
      </div>
    </Modal>
  );
}
