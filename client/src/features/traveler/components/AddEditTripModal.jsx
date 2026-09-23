import React, { useState, useEffect } from 'react';
import Modal from '../../../components/Modal';
import Input from '../../../components/Input';
import Button from '../../../components/Button';
import ErrorBanner from '../../../components/ErrorBanner';
import LoadingSpinner from '../../../components/LoadingSpinner';
import { createTrip, updateTrip } from '../../../services/travelerApi';

// Format a Date to "YYYY-MM-DD" for <input type="date"> value.
function toDateInputValue(date) {
  if (!date) return '';
  return new Date(date).toISOString().split('T')[0];
}

// AddEditTripModal — create or edit a Trip using the exact CreateTripDto / UpdateTripDto fields.
//
// Props:
//   isOpen    — controls visibility
//   onClose   — close handler
//   onSuccess — called with the new trip's ID (when creating) or undefined (when editing)
//   trip      — the existing TripDetailDto when editing; null when creating
export default function AddEditTripModal({ isOpen, onClose, onSuccess, trip }) {
  const isEdit = !!trip;

  const [formData, setFormData] = useState({
    Title: '',
    StartDate: '',
    EndDate: '',
    Budget: '',
    GroupSize: '',
    Interests: '',
  });
  const [fieldErrors, setFieldErrors] = useState({});
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  // Reset form each time the modal opens.
  useEffect(() => {
    if (isOpen) {
      setError(null);
      setFieldErrors({});
      if (isEdit && trip) {
        setFormData({
          Title: trip.title || '',
          StartDate: toDateInputValue(trip.startDate),
          EndDate: toDateInputValue(trip.endDate),
          Budget: trip.budget?.toString() || '',
          GroupSize: trip.groupSize?.toString() || '',
          Interests: trip.interests || '',
        });
      } else {
        setFormData({ Title: '', StartDate: '', EndDate: '', Budget: '', GroupSize: '', Interests: '' });
      }
    }
  }, [isOpen, isEdit, trip]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    if (fieldErrors[name]) setFieldErrors((prev) => ({ ...prev, [name]: null }));
  };

  // Client-side validation mirrors the backend's Data Annotations and service checks.
  const validate = () => {
    const errs = {};
    if (!formData.Title.trim()) errs.Title = 'Title is required.';
    if (!formData.StartDate) errs.StartDate = 'Start date is required.';
    if (!formData.EndDate) errs.EndDate = 'End date is required.';
    if (formData.StartDate && formData.EndDate && formData.EndDate <= formData.StartDate) {
      errs.EndDate = 'End date must be after start date.';
    }

    const budget = parseFloat(formData.Budget);
    if (!formData.Budget) {
      errs.Budget = 'Budget is required.';
    } else if (isNaN(budget) || budget <= 0) {
      errs.Budget = 'Budget must be greater than 0.';
    }

    const groupSize = parseInt(formData.GroupSize, 10);
    if (!formData.GroupSize) {
      errs.GroupSize = 'Group size is required.';
    } else if (isNaN(groupSize) || groupSize < 1) {
      errs.GroupSize = 'Group size must be at least 1.';
    }

    setFieldErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) return;

    setLoading(true);
    setError(null);

    const payload = {
      Title: formData.Title.trim(),
      StartDate: formData.StartDate,
      EndDate: formData.EndDate,
      Budget: parseFloat(formData.Budget),
      GroupSize: parseInt(formData.GroupSize, 10),
      Interests: formData.Interests.trim() || null,
    };

    try {
      if (isEdit) {
        await updateTrip(trip.id, payload);
        onSuccess(undefined);
      } else {
        const created = await createTrip(payload);
        onSuccess(created.id);
      }
      onClose();
    } catch (err) {
      setError(err.response?.data?.message || 'An error occurred. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={isEdit ? 'Edit Trip' : 'Create Trip'} size="md">
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        {error && <ErrorBanner message={error} />}

        <Input
          label="Trip Title"
          name="Title"
          value={formData.Title}
          onChange={handleChange}
          error={fieldErrors.Title}
          placeholder="e.g. Southern Coast Getaway"
        />

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div className="flex flex-col">
            <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary" htmlFor="trip-start-date">
              Start Date
            </label>
            <input
              id="trip-start-date"
              type="date"
              name="StartDate"
              value={formData.StartDate}
              onChange={handleChange}
              className={`bg-white border rounded-md px-4 py-2.5 font-body text-text outline-none transition-all
                ${fieldErrors.StartDate ? 'border-status-danger focus:ring-1 focus:ring-status-danger' : 'border-border-neutral focus:border-primary focus:ring-1 focus:ring-primary'}`}
            />
            {fieldErrors.StartDate && (
              <span className="mt-1.5 text-xs text-status-danger font-body">{fieldErrors.StartDate}</span>
            )}
          </div>

          <div className="flex flex-col">
            <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary" htmlFor="trip-end-date">
              End Date
            </label>
            <input
              id="trip-end-date"
              type="date"
              name="EndDate"
              value={formData.EndDate}
              min={formData.StartDate || undefined}
              onChange={handleChange}
              className={`bg-white border rounded-md px-4 py-2.5 font-body text-text outline-none transition-all
                ${fieldErrors.EndDate ? 'border-status-danger focus:ring-1 focus:ring-status-danger' : 'border-border-neutral focus:border-primary focus:ring-1 focus:ring-primary'}`}
            />
            {fieldErrors.EndDate && (
              <span className="mt-1.5 text-xs text-status-danger font-body">{fieldErrors.EndDate}</span>
            )}
          </div>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div className="flex flex-col">
            <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary" htmlFor="trip-budget">
              Budget (LKR)
            </label>
            <input
              id="trip-budget"
              type="number"
              name="Budget"
              value={formData.Budget}
              onChange={handleChange}
              min="1"
              step="1"
              className={`bg-white border rounded-md px-4 py-2.5 font-body text-text outline-none transition-all
                ${fieldErrors.Budget ? 'border-status-danger focus:ring-1 focus:ring-status-danger' : 'border-border-neutral focus:border-primary focus:ring-1 focus:ring-primary'}`}
              placeholder="e.g. 150000"
            />
            {fieldErrors.Budget && (
              <span className="mt-1.5 text-xs text-status-danger font-body">{fieldErrors.Budget}</span>
            )}
          </div>

          <div className="flex flex-col">
            <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary" htmlFor="trip-group-size">
              Group Size
            </label>
            <input
              id="trip-group-size"
              type="number"
              name="GroupSize"
              value={formData.GroupSize}
              onChange={handleChange}
              min="1"
              step="1"
              className={`bg-white border rounded-md px-4 py-2.5 font-body text-text outline-none transition-all
                ${fieldErrors.GroupSize ? 'border-status-danger focus:ring-1 focus:ring-status-danger' : 'border-border-neutral focus:border-primary focus:ring-1 focus:ring-primary'}`}
              placeholder="e.g. 2"
            />
            {fieldErrors.GroupSize && (
              <span className="mt-1.5 text-xs text-status-danger font-body">{fieldErrors.GroupSize}</span>
            )}
          </div>
        </div>

        <div className="flex flex-col">
          <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary" htmlFor="trip-interests">
            Interests{' '}
            <span className="text-text-secondary/60 font-normal">(optional — comma separated)</span>
          </label>
          <input
            id="trip-interests"
            type="text"
            name="Interests"
            value={formData.Interests}
            onChange={handleChange}
            className="bg-white border border-border-neutral rounded-md px-4 py-2.5 font-body text-text outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-all"
            placeholder="e.g. beach, hiking, wildlife"
          />
        </div>

        <div className="flex justify-end gap-3 mt-2 border-t border-border-neutral pt-4">
          <Button type="button" variant="secondary" onClick={onClose} disabled={loading}>
            Cancel
          </Button>
          <Button type="submit" disabled={loading}>
            {loading ? <LoadingSpinner size="sm" /> : isEdit ? 'Save Changes' : 'Create Trip'}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
