import React, { useState, useEffect, useCallback, useMemo } from 'react';
import Button from '../../../components/Button';
import EmptyState from '../../../components/EmptyState';
import ErrorBanner from '../../../components/ErrorBanner';
import LoadingSpinner from '../../../components/LoadingSpinner';
import ConfirmDialog from '../../../components/ConfirmDialog';
import AddEditItineraryItemModal from './AddEditItineraryItemModal';
import GenerateDraftModal from './GenerateDraftModal';
import { getDestinations, removeItineraryItem } from '../../../services/travelerApi';

// ItineraryTab — displays itinerary items grouped by day and allows full CRUD.
//
// Props:
//   trip         — TripDetailDto (contains itineraryItems, startDate, endDate, status)
//   onTripUpdate — called with the updated TripDetailDto after any mutation
export default function ItineraryTab({ trip, onTripUpdate }) {
  const [destinations, setDestinations] = useState([]);
  const [loadingDestinations, setLoadingDestinations] = useState(false);

  const [itemModalOpen, setItemModalOpen] = useState(false);
  const [editingItem, setEditingItem] = useState(null);

  const [generateModalOpen, setGenerateModalOpen] = useState(false);

  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [itemToDelete, setItemToDelete] = useState(null);
  const [deleteLoading, setDeleteLoading] = useState(false);
  const [deleteError, setDeleteError] = useState(null);

  // Load destinations once for the add/edit form dropdowns.
  useEffect(() => {
    async function load() {
      try {
        setLoadingDestinations(true);
        const data = await getDestinations({ pageSize: 200 });
        setDestinations(data.items || []);
      } catch {
        setDestinations([]);
      } finally {
        setLoadingDestinations(false);
      }
    }
    load();
  }, []);

  const items = trip.itineraryItems || [];
  const totalDays = useMemo(() => {
    if (!trip.startDate || !trip.endDate) return 0;
    return Math.round((new Date(trip.endDate) - new Date(trip.startDate)) / (1000 * 60 * 60 * 24)) + 1;
  }, [trip.startDate, trip.endDate]);

  // Group items by DayNumber.
  const groupedByDay = useMemo(() => {
    const map = {};
    items.forEach((item) => {
      if (!map[item.dayNumber]) map[item.dayNumber] = [];
      map[item.dayNumber].push(item);
    });
    return map;
  }, [items]);

  const sortedDays = Object.keys(groupedByDay).map(Number).sort((a, b) => a - b);

  const openAddModal = () => {
    setEditingItem(null);
    setItemModalOpen(true);
  };

  const openEditModal = (item) => {
    setEditingItem(item);
    setItemModalOpen(true);
  };

  const openDeleteConfirm = (item) => {
    setItemToDelete(item);
    setDeleteError(null);
    setDeleteConfirmOpen(true);
  };

  const handleConfirmDelete = async () => {
    if (!itemToDelete) return;
    try {
      setDeleteLoading(true);
      setDeleteError(null);
      const updated = await removeItineraryItem(trip.id, itemToDelete.id);
      onTripUpdate(updated);
    } catch (err) {
      setDeleteError(err.response?.data?.message || 'Failed to remove item.');
    } finally {
      setDeleteLoading(false);
    }
  };

  // Format a date as "Day N — Mon DD" using the trip start date.
  const formatDayLabel = (dayNumber) => {
    if (!trip.startDate) return `Day ${dayNumber}`;
    const date = new Date(trip.startDate);
    date.setDate(date.getDate() + dayNumber - 1);
    return `Day ${dayNumber} — ${date.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' })}`;
  };

  return (
    <div className="space-y-6">
      {/* Header row */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-headline-sm font-heading font-bold text-text mb-1">Itinerary</h2>
          <p className="text-body-sm text-text-secondary">
            {items.length} item{items.length !== 1 ? 's' : ''} across {sortedDays.length} day{sortedDays.length !== 1 ? 's' : ''}
            {totalDays > 0 && ` (${totalDays}-day trip)`}
          </p>
        </div>
        <div className="flex gap-3 flex-wrap">
          {loadingDestinations ? (
            <LoadingSpinner size="sm" />
          ) : (
            <>
              <button
                onClick={() => setGenerateModalOpen(true)}
                className="inline-flex items-center gap-2 px-4 py-2 rounded-pill border border-primary text-primary font-heading font-bold text-label-button hover:bg-surface-blue transition-colors"
              >
                <span className="material-symbols-outlined text-sm">auto_awesome</span>
                Generate Draft Itinerary
              </button>
              <Button onClick={openAddModal}>
                <span className="flex items-center gap-2">
                  <span className="material-symbols-outlined text-base">add</span>
                  Add Item
                </span>
              </Button>
            </>
          )}
        </div>
      </div>

      {deleteError && <ErrorBanner message={deleteError} />}

      {/* Items grouped by day */}
      {items.length === 0 ? (
        <EmptyState
          icon="map"
          title="No itinerary items yet"
          description="Add destinations manually or generate a draft itinerary from your chosen destinations."
          action={<Button onClick={openAddModal}>Add first item</Button>}
        />
      ) : (
        <div className="space-y-6">
          {sortedDays.map((day) => (
            <DayGroup
              key={day}
              dayLabel={formatDayLabel(day)}
              items={groupedByDay[day].sort((a, b) => a.sequenceOrder - b.sequenceOrder)}
              onEdit={openEditModal}
              onDelete={openDeleteConfirm}
            />
          ))}
        </div>
      )}

      {/* Add/Edit item modal */}
      <AddEditItineraryItemModal
        isOpen={itemModalOpen}
        onClose={() => setItemModalOpen(false)}
        onSuccess={(updatedTrip) => { onTripUpdate(updatedTrip); setItemModalOpen(false); }}
        tripId={trip.id}
        tripStartDate={trip.startDate}
        totalDays={totalDays}
        destinations={destinations}
        item={editingItem}
      />

      {/* Generate draft modal */}
      <GenerateDraftModal
        isOpen={generateModalOpen}
        onClose={() => setGenerateModalOpen(false)}
        onSuccess={(updatedTrip) => { onTripUpdate(updatedTrip); setGenerateModalOpen(false); }}
        tripId={trip.id}
        destinations={destinations}
        hasExistingItems={items.length > 0}
      />

      {/* Delete confirmation */}
      <ConfirmDialog
        isOpen={deleteConfirmOpen}
        onClose={() => setDeleteConfirmOpen(false)}
        onConfirm={handleConfirmDelete}
        title="Remove Itinerary Item"
        message={`Remove "${itemToDelete?.destinationName}" from Day ${itemToDelete?.dayNumber}?`}
        confirmLabel="Remove"
        isDanger
      />
    </div>
  );
}

// ── Day Group ──────────────────────────────────────────────────────────────

function DayGroup({ dayLabel, items, onEdit, onDelete }) {
  return (
    <div className="bg-white border border-border-neutral rounded-xl overflow-hidden shadow-soft">
      <div className="px-5 py-3 bg-surface-blue border-b border-border-blue">
        <p className="font-heading font-bold text-sm text-primary">{dayLabel}</p>
      </div>
      <div className="divide-y divide-border-neutral">
        {items.map((item) => (
          <div key={item.id} className="flex items-start gap-4 px-5 py-4">
            <div className="w-7 h-7 rounded-full bg-primary/10 text-primary flex items-center justify-center font-heading font-bold text-xs shrink-0 mt-0.5">
              {item.sequenceOrder}
            </div>
            <div className="flex-1 min-w-0">
              <p className="font-heading font-bold text-text text-sm">{item.destinationName}</p>
              {item.notes && (
                <p className="text-body-sm text-text-secondary mt-0.5 line-clamp-2">{item.notes}</p>
              )}
            </div>
            <div className="flex gap-1 shrink-0">
              <button
                onClick={() => onEdit(item)}
                className="w-8 h-8 rounded-full flex items-center justify-center text-text-secondary hover:text-primary hover:bg-surface-neutral transition-colors"
                aria-label="Edit item"
              >
                <span className="material-symbols-outlined text-sm">edit</span>
              </button>
              <button
                onClick={() => onDelete(item)}
                className="w-8 h-8 rounded-full flex items-center justify-center text-text-secondary hover:text-status-danger hover:bg-red-50 transition-colors"
                aria-label="Remove item"
              >
                <span className="material-symbols-outlined text-sm">delete</span>
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
