import React, { useState, useEffect } from 'react';
import Modal from '../../../components/Modal';
import Input from '../../../components/Input';
import Button from '../../../components/Button';
import ErrorBanner from '../../../components/ErrorBanner';
import LoadingSpinner from '../../../components/LoadingSpinner';
import { submitContractRequest } from '../../../services/supplierApi';

/**
 * Modal for submitting a contract request.
 * Props:
 *   isOpen      — boolean
 *   onClose     — () => void
 *   onSuccess   — () => void
 *   requestType — 'New' | 'Renewal'
 *   existingContractId — number | null (required for Renewal)
 *   existingEndDate    — string | null (used to show hint for Renewal)
 *
 * NOTE: Only 'New' type is offered in the current frontend because there is no
 * Supplier-authorized endpoint to retrieve the current contract ID. Renewal is
 * available as a code path for future use once that endpoint exists.
 */
export default function ContractRequestModal({
  isOpen,
  onClose,
  onSuccess,
  requestType = 'New',
  existingContractId = null,
  existingEndDate = null,
}) {
  const isRenewal = requestType === 'Renewal';

  const [formData, setFormData] = useState({
    RequestedStartDate: '',
    RequestedEndDate: '',
    RequestedTerms: '',
  });
  const [fieldErrors, setFieldErrors] = useState({});
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  // Reset form when modal opens
  useEffect(() => {
    if (isOpen) {
      setError(null);
      setFieldErrors({});
      setFormData({
        RequestedStartDate: '',
        RequestedEndDate: '',
        RequestedTerms: '',
      });
    }
  }, [isOpen]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    if (fieldErrors[name]) {
      setFieldErrors((prev) => ({ ...prev, [name]: null }));
    }
  };

  const validate = () => {
    const errors = {};
    if (!formData.RequestedEndDate) {
      errors.RequestedEndDate = 'Requested end date is required.';
    } else {
      const endDate = new Date(formData.RequestedEndDate);
      const today = new Date();
      today.setHours(0, 0, 0, 0);

      if (isRenewal && existingEndDate) {
        const existingEnd = new Date(existingEndDate);
        if (endDate <= existingEnd) {
          errors.RequestedEndDate = `End date must be after the existing contract end date (${new Date(existingEndDate).toLocaleDateString()}).`;
        }
      } else if (!isRenewal) {
        const startDate = formData.RequestedStartDate ? new Date(formData.RequestedStartDate) : today;
        if (endDate <= startDate) {
          errors.RequestedEndDate = 'End date must be after the start date.';
        }
      }
    }

    if (formData.RequestedTerms.length > 2000) {
      errors.RequestedTerms = 'Terms cannot exceed 2000 characters.';
    }

    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) return;

    setLoading(true);
    setError(null);

    const payload = {
      RequestType: requestType,
      RequestedEndDate: new Date(formData.RequestedEndDate).toISOString(),
      ...(formData.RequestedStartDate && { RequestedStartDate: new Date(formData.RequestedStartDate).toISOString() }),
      ...(formData.RequestedTerms.trim() && { RequestedTerms: formData.RequestedTerms.trim() }),
      ...(isRenewal && existingContractId && { ExistingContractId: existingContractId }),
    };

    try {
      await submitContractRequest(payload);
      onSuccess();
      onClose();
    } catch (err) {
      setError(err.response?.data?.message || err.message || 'An unexpected error occurred.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={isRenewal ? 'Request Contract Renewal' : 'Request New Contract'}
      size="md"
    >
      {/* Context explanation */}
      <div className="bg-surface-blue border border-border-blue rounded-lg p-4 mb-5">
        <p className="text-body-sm text-primary font-semibold mb-1">
          {isRenewal ? '↻ Renewal Request' : '✦ New Contract Request'}
        </p>
        <p className="text-body-sm text-text-secondary">
          {isRenewal
            ? 'This will ask an admin to extend your existing contract. The new end date must be later than your current contract end date.'
            : 'This will ask an admin to issue you a new supplier contract. Once approved, you can start listing supply items.'}
        </p>
        {isRenewal && existingEndDate && (
          <p className="mt-2 text-body-sm text-text-secondary">
            Current contract expires:{' '}
            <span className="font-semibold text-text">{new Date(existingEndDate).toLocaleDateString()}</span>
          </p>
        )}
      </div>

      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        {error && <ErrorBanner message={error} />}

        {/* Start date — only for New requests, optional */}
        {!isRenewal && (
          <div className="flex flex-col">
            <label htmlFor="req-start-date" className="mb-1.5 font-heading text-sm font-semibold text-text-secondary">
              Requested Start Date <span className="text-text-secondary/50 font-normal">(optional, defaults to today)</span>
            </label>
            <input
              id="req-start-date"
              type="date"
              name="RequestedStartDate"
              value={formData.RequestedStartDate}
              onChange={handleChange}
              className="bg-white border border-border-neutral rounded-md px-4 py-2.5 font-body text-text outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-all"
            />
          </div>
        )}

        {/* End date — required */}
        <div className="flex flex-col">
          <label htmlFor="req-end-date" className="mb-1.5 font-heading text-sm font-semibold text-text-secondary">
            Requested End Date <span className="text-status-danger">*</span>
          </label>
          <input
            id="req-end-date"
            type="date"
            name="RequestedEndDate"
            value={formData.RequestedEndDate}
            onChange={handleChange}
            className={`bg-white border rounded-md px-4 py-2.5 font-body text-text outline-none transition-all
              ${fieldErrors.RequestedEndDate
                ? 'border-status-danger focus:ring-1 focus:ring-status-danger'
                : 'border-border-neutral focus:border-primary focus:ring-1 focus:ring-primary'
              }`}
          />
          {fieldErrors.RequestedEndDate && (
            <span className="mt-1.5 text-xs text-status-danger font-body">{fieldErrors.RequestedEndDate}</span>
          )}
        </div>

        {/* Terms — optional */}
        <div className="flex flex-col">
          <label htmlFor="req-terms" className="mb-1.5 font-heading text-sm font-semibold text-text-secondary">
            Requested Terms <span className="text-text-secondary/50 font-normal">(optional)</span>
          </label>
          <textarea
            id="req-terms"
            name="RequestedTerms"
            value={formData.RequestedTerms}
            onChange={handleChange}
            rows={3}
            className={`bg-white border rounded-md px-4 py-2.5 font-body text-text placeholder:text-text-secondary/60 outline-none transition-all resize-none
              ${fieldErrors.RequestedTerms
                ? 'border-status-danger focus:ring-1 focus:ring-status-danger'
                : 'border-border-neutral focus:border-primary focus:ring-1 focus:ring-primary'
              }`}
            placeholder="Any specific terms or conditions you'd like to request..."
          />
          {fieldErrors.RequestedTerms && (
            <span className="mt-1.5 text-xs text-status-danger font-body">{fieldErrors.RequestedTerms}</span>
          )}
        </div>

        <div className="flex justify-end gap-3 mt-2 border-t border-border-neutral pt-4">
          <Button type="button" variant="secondary" onClick={onClose} disabled={loading}>
            Cancel
          </Button>
          <Button type="submit" disabled={loading}>
            {loading ? <LoadingSpinner size="sm" /> : 'Submit Request'}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
