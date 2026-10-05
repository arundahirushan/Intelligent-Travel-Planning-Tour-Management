/**
 * CreateTripWizard.jsx
 *
 * Two-step modal wizard for new trip creation:
 *
 * Step 1 — "Choose Destinations"
 *   - Loads all available destinations from GET /destinations
 *   - Shows them as photo cards in a 2-column grid
 *   - At least one must be selected; "Next →" enabled once ≥ 1 selected
 *
 * Step 2 — "Trip Details"
 *   - Existing trip creation form (title, dates, budget, group size, interests, pickup)
 *   - On submit: creates the trip, then bulk-adds selected destinations as
 *     itinerary items (Day 1, sequence 1/2/3…)
 *
 * Props:
 *   isOpen    — controls visibility
 *   onClose   — close handler
 *   onSuccess — called with the new trip's ID after everything is saved
 *
 * Edit flow is NOT handled here — use AddEditTripModal directly for edits.
 */

import React, { useState, useEffect, useCallback } from 'react';
import { createPortal } from 'react-dom';
import Button from '../../../components/Button';
import Input from '../../../components/Input';
import ErrorBanner from '../../../components/ErrorBanner';
import LoadingSpinner from '../../../components/LoadingSpinner';
import PickupLocationPickerModal from '../../../components/PickupLocationPickerModal';
import {
  getDestinations,
  createTrip,
  addItineraryItem,
} from '../../../services/travelerApi';

// ── Helpers ───────────────────────────────────────────────────────────────────

function toDateInputValue(date) {
  if (!date) return '';
  return new Date(date).toISOString().split('T')[0];
}

// Fallback gradient colours when a destination has no ImageUrl.
const GRADIENT_PALETTE = [
  'from-blue-500 to-indigo-600',
  'from-emerald-500 to-teal-600',
  'from-orange-400 to-rose-500',
  'from-violet-500 to-purple-700',
  'from-cyan-400 to-blue-600',
  'from-amber-400 to-orange-600',
  'from-pink-500 to-fuchsia-600',
  'from-green-500 to-emerald-700',
];

function gradientFor(id) {
  return GRADIENT_PALETTE[id % GRADIENT_PALETTE.length];
}

// ── Shared Modal Shell ────────────────────────────────────────────────────────

function WizardShell({ onClose, children }) {
  // Keyboard dismiss
  useEffect(() => {
    const handler = (e) => { if (e.key === 'Escape') onClose(); };
    document.addEventListener('keydown', handler);
    document.body.style.overflow = 'hidden';
    return () => {
      document.removeEventListener('keydown', handler);
      document.body.style.overflow = 'unset';
    };
  }, [onClose]);

  return createPortal(
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-text/40 backdrop-blur-sm"
        onClick={onClose}
        aria-hidden="true"
      />
      {/* Panel */}
      <div
        className="relative w-full max-w-lg bg-white rounded-2xl shadow-soft flex flex-col max-h-[90vh] overflow-hidden"
        role="dialog"
        aria-modal="true"
      >
        {children}
      </div>
    </div>,
    document.body
  );
}

// ── Step 1 — Destination Picker ───────────────────────────────────────────────

function DestinationPicker({ selectedIds, onToggle, onNext, onClose }) {
  const [destinations, setDestinations] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [search, setSearch] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getDestinations({ pageSize: 200 });
      setDestinations(data.items || []);
    } catch {
      setError('Could not load destinations. Check your connection and try again.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const filtered = destinations.filter((d) =>
    !search || d.name.toLowerCase().includes(search.toLowerCase()) ||
    d.region?.toLowerCase().includes(search.toLowerCase())
  );

  const canContinue = selectedIds.size > 0;

  return (
    <>
      {/* Header */}
      <div className="flex items-center justify-between px-6 pt-6 pb-4 border-b border-border-neutral shrink-0">
        <div>
          <p className="text-label-uppercase text-primary tracking-widest text-xs mb-0.5">Step 1 of 2</p>
          <h2 className="text-headline-sm font-heading font-bold text-text">Choose Destinations</h2>
          <p className="text-body-sm text-text-secondary mt-0.5">
            Select the places you want to visit. You can reorder them later.
          </p>
        </div>
        <button
          onClick={onClose}
          className="w-9 h-9 flex items-center justify-center text-text-secondary hover:text-text hover:bg-surface-neutral rounded-full transition-colors"
          aria-label="Close"
        >
          <span className="material-symbols-outlined">close</span>
        </button>
      </div>

      {/* Search */}
      <div className="px-6 pt-4 pb-2 shrink-0">
        <div className="relative">
          <span className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-text-secondary text-base pointer-events-none">
            search
          </span>
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search destinations…"
            className="w-full pl-10 pr-4 py-2.5 bg-surface-neutral border border-border-neutral rounded-lg font-body text-sm text-text outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-all"
            aria-label="Search destinations"
          />
        </div>
      </div>

      {/* Selection count */}
      <div className="px-6 py-2 shrink-0">
        {selectedIds.size > 0 ? (
          <p className="text-body-sm font-heading font-bold text-primary">
            {selectedIds.size} destination{selectedIds.size !== 1 ? 's' : ''} selected
          </p>
        ) : (
          <p className="text-body-sm text-text-secondary">Select at least one destination to continue.</p>
        )}
      </div>

      {/* Grid */}
      <div className="flex-1 overflow-y-auto px-6 pb-2">
        {error && <ErrorBanner message={error} />}
        {loading ? (
          <div className="flex justify-center items-center h-48">
            <LoadingSpinner size="lg" />
          </div>
        ) : filtered.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-48 text-center">
            <span className="material-symbols-outlined text-4xl text-text-secondary/40 mb-3">search_off</span>
            <p className="text-body-sm text-text-secondary">
              {search ? `No destinations match "${search}"` : 'No destinations available.'}
            </p>
          </div>
        ) : (
          <div className="grid grid-cols-2 gap-3">
            {filtered.map((dest) => {
              const selected = selectedIds.has(dest.id);
              return (
                <button
                  key={dest.id}
                  onClick={() => onToggle(dest.id)}
                  aria-pressed={selected}
                  aria-label={`${selected ? 'Deselect' : 'Select'} ${dest.name}`}
                  className={`relative rounded-xl overflow-hidden text-left transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2
                    ${selected
                      ? 'ring-2 ring-primary shadow-md scale-[1.02]'
                      : 'ring-1 ring-border-neutral hover:ring-primary/50 hover:scale-[1.01]'
                    }`}
                >
                  {/* Photo / gradient */}
                  <div className="relative h-28 w-full">
                    {dest.imageUrl ? (
                      <img
                        src={dest.imageUrl}
                        alt={dest.name}
                        className="w-full h-full object-cover"
                        onError={(e) => {
                          e.target.style.display = 'none';
                          e.target.nextSibling.style.display = 'flex';
                        }}
                      />
                    ) : null}
                    <div
                      className={`w-full h-full bg-gradient-to-br ${gradientFor(dest.id)} flex items-center justify-center ${dest.imageUrl ? 'hidden' : 'flex'}`}
                    >
                      <span className="material-symbols-outlined text-white text-3xl opacity-60">landscape</span>
                    </div>

                    {/* Overlay gradient for text readability */}
                    <div className="absolute inset-0 bg-gradient-to-t from-black/70 via-black/10 to-transparent" />

                    {/* Selected tick */}
                    {selected && (
                      <div className="absolute top-2 right-2 w-6 h-6 bg-primary rounded-full flex items-center justify-center shadow-md">
                        <span className="material-symbols-outlined text-white text-sm">check</span>
                      </div>
                    )}

                    {/* Name & region */}
                    <div className="absolute bottom-0 left-0 right-0 p-2.5">
                      <p className="font-heading font-bold text-white text-sm leading-tight line-clamp-1">
                        {dest.name}
                      </p>
                      {dest.region && (
                        <p className="text-white/80 text-xs mt-0.5 line-clamp-1">{dest.region}</p>
                      )}
                    </div>
                  </div>
                </button>
              );
            })}
          </div>
        )}
      </div>

      {/* Footer */}
      <div className="flex items-center justify-between px-6 py-4 border-t border-border-neutral shrink-0 bg-white">
        <Button variant="secondary" onClick={onClose}>
          Cancel
        </Button>
        <Button
          onClick={onNext}
          disabled={!canContinue}
          aria-disabled={!canContinue}
          aria-label={canContinue ? 'Next step: trip details' : 'Select at least one destination to continue'}
        >
          <span className="flex items-center gap-2">
            Next
            <span className="material-symbols-outlined text-base">arrow_forward</span>
          </span>
        </Button>
      </div>
    </>
  );
}

// ── Step 2 — Trip Details Form ────────────────────────────────────────────────

function TripDetailsForm({ selectedIds, onBack, onClose, onSuccess }) {
  const [formData, setFormData] = useState({
    Title: '',
    StartDate: '',
    EndDate: '',
    Budget: '',
    GroupSize: '',
    Interests: '',
    PickupLatitude: null,
    PickupLongitude: null,
    PickupNote: '',
  });
  const [fieldErrors, setFieldErrors] = useState({});
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [pickupModalOpen, setPickupModalOpen] = useState(false);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    if (fieldErrors[name]) setFieldErrors((prev) => ({ ...prev, [name]: null }));
  };

  const validate = () => {
    const errs = {};
    if (!formData.Title.trim()) errs.Title = 'Title is required.';
    if (!formData.StartDate) errs.StartDate = 'Start date is required.';
    if (!formData.EndDate) errs.EndDate = 'End date is required.';
    if (formData.StartDate && formData.EndDate && formData.EndDate <= formData.StartDate) {
      errs.EndDate = 'End date must be after start date.';
    }
    const budget = parseFloat(formData.Budget);
    if (!formData.Budget) errs.Budget = 'Budget is required.';
    else if (isNaN(budget) || budget <= 0) errs.Budget = 'Budget must be greater than 0.';
    const groupSize = parseInt(formData.GroupSize, 10);
    if (!formData.GroupSize) errs.GroupSize = 'Group size is required.';
    else if (isNaN(groupSize) || groupSize < 1) errs.GroupSize = 'Group size must be at least 1.';
    if (formData.PickupLatitude === null || formData.PickupLongitude === null) {
      errs.PickupLocation = 'Pickup location is required for trip planning.';
    }
    setFieldErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) return;
    setLoading(true);
    setError(null);

    try {
      // 1. Create the trip
      const created = await createTrip({
        Title: formData.Title.trim(),
        StartDate: formData.StartDate,
        EndDate: formData.EndDate,
        Budget: parseFloat(formData.Budget),
        GroupSize: parseInt(formData.GroupSize, 10),
        Interests: formData.Interests.trim() || null,
        PickupLatitude: formData.PickupLatitude,
        PickupLongitude: formData.PickupLongitude,
        PickupNote: formData.PickupNote || null,
      });

      // 2. Add selected destinations as itinerary items (Day 1, sequential order).
      //    We fire them in sequence to avoid racing on SequenceOrder.
      const destIds = Array.from(selectedIds);
      for (let i = 0; i < destIds.length; i++) {
        try {
          await addItineraryItem(created.id, {
            DestinationId: destIds[i],
            DayNumber: 1,
            SequenceOrder: i + 1,
            Notes: null,
          });
        } catch {
          // Non-fatal: itinerary items can be managed in the Itinerary tab.
        }
      }

      onSuccess(created.id);
    } catch (err) {
      setError(err.response?.data?.message || 'An error occurred. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const fieldClass = (name) =>
    `bg-white border rounded-md px-4 py-2.5 font-body text-text outline-none transition-all ${
      fieldErrors[name]
        ? 'border-status-danger focus:ring-1 focus:ring-status-danger'
        : 'border-border-neutral focus:border-primary focus:ring-1 focus:ring-primary'
    }`;

  return (
    <>
      {/* Header */}
      <div className="flex items-center justify-between px-6 pt-6 pb-4 border-b border-border-neutral shrink-0">
        <div>
          <p className="text-label-uppercase text-primary tracking-widest text-xs mb-0.5">Step 2 of 2</p>
          <h2 className="text-headline-sm font-heading font-bold text-text">Trip Details</h2>
          <p className="text-body-sm text-text-secondary mt-0.5">
            {selectedIds.size} destination{selectedIds.size !== 1 ? 's' : ''} selected
          </p>
        </div>
        <button
          onClick={onClose}
          className="w-9 h-9 flex items-center justify-center text-text-secondary hover:text-text hover:bg-surface-neutral rounded-full transition-colors"
          aria-label="Close"
        >
          <span className="material-symbols-outlined">close</span>
        </button>
      </div>

      {/* Form */}
      <div className="flex-1 overflow-y-auto px-6 py-4">
        <form id="trip-details-form" onSubmit={handleSubmit} className="flex flex-col gap-4">
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
              <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary" htmlFor="wizard-start-date">
                Start Date
              </label>
              <input
                id="wizard-start-date"
                type="date"
                name="StartDate"
                value={formData.StartDate}
                onChange={handleChange}
                className={fieldClass('StartDate')}
              />
              {fieldErrors.StartDate && (
                <span className="mt-1.5 text-xs text-status-danger font-body">{fieldErrors.StartDate}</span>
              )}
            </div>
            <div className="flex flex-col">
              <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary" htmlFor="wizard-end-date">
                End Date
              </label>
              <input
                id="wizard-end-date"
                type="date"
                name="EndDate"
                value={formData.EndDate}
                min={formData.StartDate || undefined}
                onChange={handleChange}
                className={fieldClass('EndDate')}
              />
              {fieldErrors.EndDate && (
                <span className="mt-1.5 text-xs text-status-danger font-body">{fieldErrors.EndDate}</span>
              )}
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="flex flex-col">
              <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary" htmlFor="wizard-budget">
                Budget (LKR)
              </label>
              <input
                id="wizard-budget"
                type="number"
                name="Budget"
                value={formData.Budget}
                onChange={handleChange}
                min="1"
                step="1"
                placeholder="e.g. 150000"
                className={fieldClass('Budget')}
              />
              {fieldErrors.Budget && (
                <span className="mt-1.5 text-xs text-status-danger font-body">{fieldErrors.Budget}</span>
              )}
            </div>
            <div className="flex flex-col">
              <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary" htmlFor="wizard-group-size">
                Group Size
              </label>
              <input
                id="wizard-group-size"
                type="number"
                name="GroupSize"
                value={formData.GroupSize}
                onChange={handleChange}
                min="1"
                step="1"
                placeholder="e.g. 2"
                className={fieldClass('GroupSize')}
              />
              {fieldErrors.GroupSize && (
                <span className="mt-1.5 text-xs text-status-danger font-body">{fieldErrors.GroupSize}</span>
              )}
            </div>
          </div>

          <div className="flex flex-col">
            <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary">
              Interests{' '}
              <span className="text-text-secondary/60 font-normal">(optional)</span>
            </label>
            <div className="flex overflow-x-auto pt-1 pb-2 px-1 -mx-1 gap-2">
              {['Beach', 'Wildlife', 'Culture', 'Nature', 'Adventure'].map(interest => {
                const isSelected = formData.Interests.split(',').map(s => s.trim()).includes(interest);
                return (
                  <button
                    key={interest}
                    type="button"
                    aria-pressed={isSelected}
                    onClick={() => {
                      setFormData(prev => {
                        const current = (prev.Interests || '').split(',').map(s => s.trim()).filter(Boolean);
                        const selected = current.includes(interest);
                        const next = selected ? current.filter(i => i !== interest) : [...current, interest];
                        return { ...prev, Interests: next.join(', ') };
                      });
                    }}
                    className={`whitespace-nowrap px-4 py-1.5 rounded-full text-center transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-1 shrink-0 font-body text-sm
                      ${isSelected
                        ? 'ring-2 ring-primary shadow-md scale-[1.02] bg-primary/5 font-semibold text-primary'
                        : 'ring-1 ring-border-neutral hover:ring-primary/50 hover:scale-[1.01] text-text-secondary hover:text-text'
                      }`}
                  >
                    {interest}
                  </button>
                );
              })}
            </div>
          </div>

          <div className="flex flex-col">
            <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary">
              Pickup Location <span className="text-status-danger">*</span>
            </label>
            <div className="flex items-center gap-3">
              <Button type="button" variant="secondary" onClick={() => setPickupModalOpen(true)}>
                <span className="flex items-center gap-2">
                  <span className="material-symbols-outlined text-sm">location_on</span>
                  {formData.PickupLatitude ? 'Edit Pickup Location' : 'Set Pickup Location'}
                </span>
              </Button>
              {formData.PickupLatitude && (
                <span className="text-body-sm text-text-secondary">
                  {formData.PickupLatitude.toFixed(4)}, {formData.PickupLongitude.toFixed(4)}
                </span>
              )}
            </div>
            {fieldErrors.PickupLocation && (
              <span className="mt-1.5 text-xs text-status-danger font-body">{fieldErrors.PickupLocation}</span>
            )}
          </div>
        </form>
      </div>

      {/* Footer */}
      <div className="flex items-center justify-between px-6 py-4 border-t border-border-neutral shrink-0 bg-white">
        <button
          type="button"
          onClick={onBack}
          disabled={loading}
          className="inline-flex items-center gap-1.5 text-body-sm font-heading font-bold text-text-secondary hover:text-primary transition-colors disabled:opacity-50"
          aria-label="Go back to destination selection"
        >
          <span className="material-symbols-outlined text-base">arrow_back</span>
          Back
        </button>
        <Button
          type="submit"
          form="trip-details-form"
          disabled={loading}
        >
          {loading ? (
            <LoadingSpinner size="sm" />
          ) : (
            <span className="flex items-center gap-2">
              <span className="material-symbols-outlined text-base">check</span>
              Create Trip
            </span>
          )}
        </Button>
      </div>

      <PickupLocationPickerModal
        isOpen={pickupModalOpen}
        onClose={() => setPickupModalOpen(false)}
        initialLat={formData.PickupLatitude}
        initialLng={formData.PickupLongitude}
        initialNote={formData.PickupNote}
        onConfirm={({ lat, lng, note }) => {
          setFormData((prev) => ({
            ...prev,
            PickupLatitude: lat,
            PickupLongitude: lng,
            PickupNote: note,
          }));
          setFieldErrors((prev) => ({ ...prev, PickupLocation: null }));
        }}
      />
    </>
  );
}

// ── Wizard Root ───────────────────────────────────────────────────────────────

export default function CreateTripWizard({ isOpen, onClose, onSuccess }) {
  const [step, setStep] = useState(1);
  const [selectedIds, setSelectedIds] = useState(new Set());

  // Reset wizard state each time it opens.
  useEffect(() => {
    if (isOpen) {
      setStep(1);
      setSelectedIds(new Set());
    }
  }, [isOpen]);

  const handleToggle = (id) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  };

  const handleNext = () => setStep(2);
  const handleBack = () => setStep(1);

  const handleSuccess = (newTripId) => {
    onClose();
    onSuccess(newTripId);
  };

  if (!isOpen) return null;

  return (
    <WizardShell onClose={onClose}>
      {step === 1 ? (
        <DestinationPicker
          selectedIds={selectedIds}
          onToggle={handleToggle}
          onNext={handleNext}
          onClose={onClose}
        />
      ) : (
        <TripDetailsForm
          selectedIds={selectedIds}
          onBack={handleBack}
          onClose={onClose}
          onSuccess={handleSuccess}
        />
      )}
    </WizardShell>
  );
}
