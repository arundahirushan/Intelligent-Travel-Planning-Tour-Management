import React, { useState, useEffect } from 'react';
import Modal from '../../../components/Modal';
import Button from '../../../components/Button';
import ErrorBanner from '../../../components/ErrorBanner';
import LoadingSpinner from '../../../components/LoadingSpinner';
import { generateDraftItinerary } from '../../../services/travelerApi';

// GenerateDraftModal — multi-select destinations and call the generate-draft-itinerary endpoint.
//
// IMPORTANT: The backend REPLACES all existing itinerary items. The UI warns the traveler.
//
// Props:
//   isOpen             — controls visibility
//   onClose            — close handler
//   onSuccess          — called with updated TripDetailDto
//   tripId             — current trip ID
//   destinations       — all available destinations [{ id, name, region }]
//   hasExistingItems   — true if the trip already has itinerary items (triggers warning)
export default function GenerateDraftModal({
  isOpen,
  onClose,
  onSuccess,
  tripId,
  destinations = [],
  hasExistingItems = false,
}) {
  // Ordered list of selected destination IDs (order matters — first destinations get more days).
  const [selectedIds, setSelectedIds] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (isOpen) {
      setSelectedIds([]);
      setError(null);
    }
  }, [isOpen]);

  const toggleDestination = (id) => {
    setSelectedIds((prev) =>
      prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]
    );
  };

  const moveUp = (index) => {
    if (index <= 0) return;
    setSelectedIds((prev) => {
      const next = [...prev];
      [next[index - 1], next[index]] = [next[index], next[index - 1]];
      return next;
    });
  };

  const moveDown = (index) => {
    setSelectedIds((prev) => {
      if (index >= prev.length - 1) return prev;
      const next = [...prev];
      [next[index], next[index + 1]] = [next[index + 1], next[index]];
      return next;
    });
  };

  const handleGenerate = async () => {
    if (selectedIds.length === 0) {
      setError('Please select at least one destination.');
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const result = await generateDraftItinerary(tripId, selectedIds);
      onSuccess(result);
      onClose();
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to generate draft itinerary.');
    } finally {
      setLoading(false);
    }
  };

  const getDestName = (id) => destinations.find((d) => d.id === id)?.name || `Destination ${id}`;

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Generate Draft Itinerary" size="lg">
      <div className="space-y-5">
        {/* Warning about replacing existing items */}
        {hasExistingItems && (
          <div className="flex gap-3 p-4 bg-status-warning/10 border border-status-warning/30 rounded-lg">
            <span className="material-symbols-outlined text-status-warning shrink-0">warning</span>
            <div>
              <p className="font-heading font-bold text-sm text-status-warning mb-1">Existing items will be replaced</p>
              <p className="text-body-sm text-text-secondary">
                Generating a draft will remove all your current itinerary items and create a new set
                based on the destinations you select below.
              </p>
            </div>
          </div>
        )}

        <p className="text-body-md text-text-secondary">
          Select destinations in the order you want to visit them. The trip's days will be split
          evenly across your chosen destinations.
        </p>

        {error && <ErrorBanner message={error} />}

        {/* Destination checkboxes */}
        <div className="border border-border-neutral rounded-lg overflow-hidden max-h-56 overflow-y-auto">
          {destinations.length === 0 ? (
            <p className="p-4 text-body-sm text-text-secondary text-center">
              No destinations available.
            </p>
          ) : (
            destinations.map((d) => {
              const checked = selectedIds.includes(d.id);
              return (
                <label
                  key={d.id}
                  className={`flex items-center gap-3 px-4 py-3 cursor-pointer border-b border-border-neutral last:border-b-0 transition-colors
                    ${checked ? 'bg-surface-blue' : 'bg-white hover:bg-surface-neutral'}`}
                >
                  <input
                    type="checkbox"
                    checked={checked}
                    onChange={() => toggleDestination(d.id)}
                    className="w-4 h-4 accent-primary"
                  />
                  <div>
                    <p className="text-body-sm font-heading font-bold text-text">{d.name}</p>
                    {d.region && <p className="text-label-badge text-text-secondary">{d.region}</p>}
                  </div>
                </label>
              );
            })
          )}
        </div>

        {/* Order list — shows selected destinations in visit order with up/down controls */}
        {selectedIds.length > 0 && (
          <div>
            <p className="text-label-uppercase text-text-secondary tracking-widest mb-3 font-heading font-bold">
              Visit order (drag via buttons to reorder)
            </p>
            <div className="space-y-2">
              {selectedIds.map((id, idx) => (
                <div
                  key={id}
                  className="flex items-center gap-3 bg-surface-blue border border-border-blue rounded-lg px-4 py-2.5"
                >
                  <span className="w-6 h-6 rounded-full bg-primary text-white text-xs flex items-center justify-center font-heading font-bold shrink-0">
                    {idx + 1}
                  </span>
                  <span className="flex-1 text-body-sm font-heading font-bold text-text">
                    {getDestName(id)}
                  </span>
                  <div className="flex gap-1">
                    <button
                      type="button"
                      onClick={() => moveUp(idx)}
                      disabled={idx === 0}
                      className="w-7 h-7 rounded flex items-center justify-center text-text-secondary hover:text-primary hover:bg-white disabled:opacity-30 transition-colors"
                      aria-label="Move up"
                    >
                      <span className="material-symbols-outlined text-sm">arrow_upward</span>
                    </button>
                    <button
                      type="button"
                      onClick={() => moveDown(idx)}
                      disabled={idx === selectedIds.length - 1}
                      className="w-7 h-7 rounded flex items-center justify-center text-text-secondary hover:text-primary hover:bg-white disabled:opacity-30 transition-colors"
                      aria-label="Move down"
                    >
                      <span className="material-symbols-outlined text-sm">arrow_downward</span>
                    </button>
                    <button
                      type="button"
                      onClick={() => toggleDestination(id)}
                      className="w-7 h-7 rounded flex items-center justify-center text-text-secondary hover:text-status-danger hover:bg-white transition-colors"
                      aria-label="Remove"
                    >
                      <span className="material-symbols-outlined text-sm">close</span>
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        <div className="flex justify-end gap-3 border-t border-border-neutral pt-4">
          <Button type="button" variant="secondary" onClick={onClose} disabled={loading}>
            Cancel
          </Button>
          <Button
            type="button"
            onClick={handleGenerate}
            disabled={loading || selectedIds.length === 0}
          >
            {loading ? (
              <LoadingSpinner size="sm" />
            ) : (
              <span className="flex items-center gap-2">
                <span className="material-symbols-outlined text-base">auto_awesome</span>
                Generate Draft Itinerary
              </span>
            )}
          </Button>
        </div>
      </div>
    </Modal>
  );
}
