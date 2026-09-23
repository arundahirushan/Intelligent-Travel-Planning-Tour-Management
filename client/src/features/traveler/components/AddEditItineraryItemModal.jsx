import React, { useState, useEffect } from 'react';
import Modal from '../../../components/Modal';
import Button from '../../../components/Button';
import ErrorBanner from '../../../components/ErrorBanner';
import LoadingSpinner from '../../../components/LoadingSpinner';
import { addItineraryItem, updateItineraryItem } from '../../../services/travelerApi';

// AddEditItineraryItemModal — create or update one itinerary item.
//
// Fields match CreateItineraryItemDto / UpdateItineraryItemDto exactly:
//   DestinationId (required), DayNumber (required, 1–totalDays), Notes (optional), SequenceOrder (required, ≥1)
//
// Props:
//   isOpen        — controls visibility
//   onClose       — close handler
//   onSuccess     — called with updated TripDetailDto after save
//   tripId        — the current trip's ID
//   tripStartDate — trip start (for day number validation label)
//   totalDays     — total trip days, used to validate DayNumber
//   destinations  — array of { id, name } for dropdown
//   item          — existing ItineraryItemDto when editing; null when adding
export default function AddEditItineraryItemModal({
  isOpen,
  onClose,
  onSuccess,
  tripId,
  tripStartDate,
  totalDays,
  destinations = [],
  item,
}) {
  const isEdit = !!item;

  const [formData, setFormData] = useState({
    DestinationId: '',
    DayNumber: '',
    Notes: '',
    SequenceOrder: '',
  });
  const [fieldErrors, setFieldErrors] = useState({});
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (isOpen) {
      setError(null);
      setFieldErrors({});
      if (isEdit && item) {
        setFormData({
          DestinationId: item.destinationId?.toString() || '',
          DayNumber: item.dayNumber?.toString() || '',
          Notes: item.notes || '',
          SequenceOrder: item.sequenceOrder?.toString() || '',
        });
      } else {
        setFormData({ DestinationId: '', DayNumber: '', Notes: '', SequenceOrder: '1' });
      }
    }
  }, [isOpen, isEdit, item]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    if (fieldErrors[name]) setFieldErrors((prev) => ({ ...prev, [name]: null }));
  };

  const validate = () => {
    const errs = {};
    if (!formData.DestinationId) errs.DestinationId = 'Destination is required.';

    const day = parseInt(formData.DayNumber, 10);
    if (!formData.DayNumber) {
      errs.DayNumber = 'Day number is required.';
    } else if (isNaN(day) || day < 1) {
      errs.DayNumber = 'Day number must be at least 1.';
    } else if (totalDays && day > totalDays) {
      errs.DayNumber = `Day number must be between 1 and ${totalDays} (total trip days).`;
    }

    const seq = parseInt(formData.SequenceOrder, 10);
    if (!formData.SequenceOrder) {
      errs.SequenceOrder = 'Sequence order is required.';
    } else if (isNaN(seq) || seq < 1) {
      errs.SequenceOrder = 'Sequence order must be at least 1.';
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
      DestinationId: parseInt(formData.DestinationId, 10),
      DayNumber: parseInt(formData.DayNumber, 10),
      Notes: formData.Notes.trim() || null,
      SequenceOrder: parseInt(formData.SequenceOrder, 10),
    };

    try {
      let result;
      if (isEdit) {
        result = await updateItineraryItem(tripId, item.id, payload);
      } else {
        result = await addItineraryItem(tripId, payload);
      }
      onSuccess(result);
      onClose();
    } catch (err) {
      setError(err.response?.data?.message || 'An error occurred. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const selectClass = (hasError) =>
    `bg-white border rounded-md px-4 py-2.5 font-body text-text outline-none transition-all w-full
    ${hasError ? 'border-status-danger focus:ring-1 focus:ring-status-danger' : 'border-border-neutral focus:border-primary focus:ring-1 focus:ring-primary'}`;

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={isEdit ? 'Edit Itinerary Item' : 'Add Itinerary Item'} size="md">
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        {error && <ErrorBanner message={error} />}

        {/* Destination dropdown — uses real backend data */}
        <div className="flex flex-col">
          <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary" htmlFor="itin-dest">
            Destination
          </label>
          <select
            id="itin-dest"
            name="DestinationId"
            value={formData.DestinationId}
            onChange={handleChange}
            className={selectClass(fieldErrors.DestinationId)}
          >
            <option value="">Select a destination…</option>
            {destinations.map((d) => (
              <option key={d.id} value={d.id}>
                {d.name}{d.region ? ` — ${d.region}` : ''}
              </option>
            ))}
          </select>
          {fieldErrors.DestinationId && (
            <span className="mt-1.5 text-xs text-status-danger font-body">{fieldErrors.DestinationId}</span>
          )}
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div className="flex flex-col">
            <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary" htmlFor="itin-day">
              Day Number {totalDays ? `(1 – ${totalDays})` : ''}
            </label>
            <input
              id="itin-day"
              type="number"
              name="DayNumber"
              value={formData.DayNumber}
              onChange={handleChange}
              min="1"
              max={totalDays || undefined}
              step="1"
              className={`bg-white border rounded-md px-4 py-2.5 font-body text-text outline-none transition-all
                ${fieldErrors.DayNumber ? 'border-status-danger focus:ring-1 focus:ring-status-danger' : 'border-border-neutral focus:border-primary focus:ring-1 focus:ring-primary'}`}
            />
            {fieldErrors.DayNumber && (
              <span className="mt-1.5 text-xs text-status-danger font-body">{fieldErrors.DayNumber}</span>
            )}
          </div>

          <div className="flex flex-col">
            <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary" htmlFor="itin-seq">
              Sequence Order
            </label>
            <input
              id="itin-seq"
              type="number"
              name="SequenceOrder"
              value={formData.SequenceOrder}
              onChange={handleChange}
              min="1"
              step="1"
              className={`bg-white border rounded-md px-4 py-2.5 font-body text-text outline-none transition-all
                ${fieldErrors.SequenceOrder ? 'border-status-danger focus:ring-1 focus:ring-status-danger' : 'border-border-neutral focus:border-primary focus:ring-1 focus:ring-primary'}`}
            />
            {fieldErrors.SequenceOrder && (
              <span className="mt-1.5 text-xs text-status-danger font-body">{fieldErrors.SequenceOrder}</span>
            )}
          </div>
        </div>

        <div className="flex flex-col">
          <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary" htmlFor="itin-notes">
            Notes <span className="text-text-secondary/60 font-normal">(optional)</span>
          </label>
          <textarea
            id="itin-notes"
            name="Notes"
            value={formData.Notes}
            onChange={handleChange}
            rows={3}
            className="bg-white border border-border-neutral rounded-md px-4 py-2.5 font-body text-text placeholder:text-text-secondary/60 outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-all"
            placeholder="What's planned for this day?"
          />
        </div>

        <div className="flex justify-end gap-3 border-t border-border-neutral pt-4">
          <Button type="button" variant="secondary" onClick={onClose} disabled={loading}>
            Cancel
          </Button>
          <Button type="submit" disabled={loading}>
            {loading ? <LoadingSpinner size="sm" /> : isEdit ? 'Save Changes' : 'Add Item'}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
