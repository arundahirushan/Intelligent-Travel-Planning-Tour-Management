import React, { useState, useEffect, useCallback } from 'react';
import Button from '../../../components/Button';
import StatusBadge from '../../../components/StatusBadge';
import EmptyState from '../../../components/EmptyState';
import ErrorBanner from '../../../components/ErrorBanner';
import LoadingSpinner from '../../../components/LoadingSpinner';
import ConfirmDialog from '../../../components/ConfirmDialog';
import PickupLocationModal from '../../../components/PickupLocationModal';
import BookVehicleModal from './BookVehicleModal';
import {
  getMyVehicleBookings,
  cancelVehicleBooking,
  deleteVehicleBooking,
} from '../../../services/travelerApi';

function formatLKR(amount) {
  return `LKR ${Number(amount).toLocaleString('en-LK')}`;
}

function formatDate(dateStr) {
  return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

// TransportTab — vehicle bookings for the traveler.
// NOTE: VehicleBookingSummaryDto does NOT include VehicleId, which UpdateVehicleBookingDto requires.
// In-place edit is therefore not supported. To change dates or location, cancel and rebook.
//
// Props:
//   trip — TripDetailDto (or null for global My Bookings page)
export default function TransportTab({ trip }) {
  const [allBookings, setAllBookings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [statusFilter, setStatusFilter] = useState('All');

  const [bookModalOpen, setBookModalOpen] = useState(false);

  const [viewMapOpen, setViewMapOpen] = useState(false);
  const [viewMapBooking, setViewMapBooking] = useState(null);

  const [cancelConfirmOpen, setCancelConfirmOpen] = useState(false);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [actionTarget, setActionTarget] = useState(null);
  const [actionError, setActionError] = useState(null);

  const fetchBookings = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      // Backend has no status filter — load all and filter client-side.
      const data = await getMyVehicleBookings({ pageSize: 200 });
      setAllBookings(data.items || []);
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load vehicle bookings.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchBookings(); }, [fetchBookings]);

  const bookings = statusFilter === 'All'
    ? allBookings
    : allBookings.filter((b) => b.status === statusFilter);

  const openCancel = (b) => { setActionTarget(b); setActionError(null); setCancelConfirmOpen(true); };
  const openDelete = (b) => { setActionTarget(b); setActionError(null); setDeleteConfirmOpen(true); };
  const openMap = (b) => { setViewMapBooking(b); setViewMapOpen(true); };

  const handleCancel = async () => {
    try {
      setActionError(null);
      await cancelVehicleBooking(actionTarget.id);
      fetchBookings();
    } catch (err) {
      setActionError(err.response?.data?.message || 'Cancel failed.');
    }
  };

  const handleDelete = async () => {
    try {
      setActionError(null);
      await deleteVehicleBooking(actionTarget.id);
      fetchBookings();
    } catch (err) {
      setActionError(err.response?.data?.message || 'Delete failed.');
    }
  };

  const isHeld = (b) => b.status === 'Held';
  const heldOrConfirmed = (b) => b.status === 'Held' || b.status === 'Confirmed';

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-headline-sm font-heading font-bold text-text mb-1">Transport</h2>
          <p className="text-body-sm text-text-secondary">Your vehicle bookings (all trips)</p>
        </div>
        <div className="flex gap-3 flex-wrap items-center">
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            className="border border-border-neutral rounded-md px-3 py-2 bg-white text-body-md focus:outline-none focus:border-primary text-sm"
          >
            <option value="All">All Statuses</option>
            <option value="Held">Held</option>
            <option value="Confirmed">Confirmed</option>
            <option value="Cancelled">Cancelled</option>
          </select>
          {trip && (
            <Button onClick={() => setBookModalOpen(true)}>
              <span className="flex items-center gap-2">
                <span className="material-symbols-outlined text-base">add</span>
                Book Vehicle
              </span>
            </Button>
          )}
        </div>
      </div>

      {error && <ErrorBanner message={error} />}
      {actionError && <ErrorBanner message={actionError} />}

      {loading ? (
        <div className="flex justify-center py-10"><LoadingSpinner size="lg" /></div>
      ) : bookings.length === 0 ? (
        <EmptyState
          icon="directions_car"
          title="No vehicle bookings"
          description={statusFilter !== 'All' ? 'No bookings match the current filter.' : (trip ? 'Book transport for your trip.' : 'You have no vehicle bookings yet. Navigate to a specific trip to book a vehicle.')}
          action={(trip && statusFilter === 'All') && <Button onClick={() => setBookModalOpen(true)}>Book a Vehicle</Button>}
        />
      ) : (
        <div className="space-y-4">
          {bookings.map((b) => {
            const days = Math.max(0, Math.round((new Date(b.endDate) - new Date(b.startDate)) / (1000 * 60 * 60 * 24)));
            return (
              <div key={b.id} className="bg-white border border-border-neutral rounded-xl p-5 shadow-soft">
                <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-4">
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-2">
                      <p className="font-heading font-bold text-text">{b.model}</p>
                      <StatusBadge status={b.status} />
                    </div>
                    <p className="text-body-sm text-text-secondary mb-3">
                      {b.vehicleType} · {b.registrationNumber}
                    </p>
                    <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
                      <div>
                        <p className="text-label-uppercase text-text-secondary tracking-widest">Start</p>
                        <p className="text-sm font-heading font-bold text-text">{formatDate(b.startDate)}</p>
                      </div>
                      <div>
                        <p className="text-label-uppercase text-text-secondary tracking-widest">End</p>
                        <p className="text-sm font-heading font-bold text-text">{formatDate(b.endDate)}</p>
                      </div>
                      <div>
                        <p className="text-label-uppercase text-text-secondary tracking-widest">Total</p>
                        <p className="text-sm font-heading font-bold text-primary">{formatLKR(b.totalPrice)}</p>
                      </div>
                    </div>
                    {isHeld(b) && (
                      <p className="mt-3 text-label-badge text-text-secondary italic">
                        To change dates or pickup location, cancel and place a new booking.
                      </p>
                    )}
                  </div>

                  <div className="flex gap-2 shrink-0">
                    {/* View pickup location */}
                    <button
                      onClick={() => openMap(b)}
                      className="p-2 rounded-lg hover:bg-surface-neutral text-text-secondary hover:text-primary transition-colors"
                      title="View pickup location"
                    >
                      <span className="material-symbols-outlined text-sm">location_on</span>
                    </button>
                    {isHeld(b) && (
                      <p className="hidden" /> // Omit edit, handle note below
                    )}
                    {heldOrConfirmed(b) && (
                      <button onClick={() => openCancel(b)} className="p-2 rounded-lg hover:bg-red-50 text-text-secondary hover:text-status-danger transition-colors" title="Cancel">
                        <span className="material-symbols-outlined text-sm">cancel</span>
                      </button>
                    )}
                    {isHeld(b) && (
                      <button onClick={() => openDelete(b)} className="p-2 rounded-lg hover:bg-red-50 text-text-secondary hover:text-status-danger transition-colors" title="Delete">
                        <span className="material-symbols-outlined text-sm">delete</span>
                      </button>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Modals */}
      <BookVehicleModal
        isOpen={bookModalOpen}
        onClose={() => setBookModalOpen(false)}
        onSuccess={fetchBookings}
        trip={trip}
      />
      <PickupLocationModal
        isOpen={viewMapOpen}
        onClose={() => setViewMapOpen(false)}
        latitude={viewMapBooking?.pickupLatitude}
        longitude={viewMapBooking?.pickupLongitude}
        note={viewMapBooking?.pickupNote}
      />
      <ConfirmDialog
        isOpen={cancelConfirmOpen}
        onClose={() => setCancelConfirmOpen(false)}
        onConfirm={handleCancel}
        title="Cancel Vehicle Booking"
        message={`Cancel your booking for "${actionTarget?.model}"?`}
        confirmLabel="Cancel Booking"
        isDanger
      />
      <ConfirmDialog
        isOpen={deleteConfirmOpen}
        onClose={() => setDeleteConfirmOpen(false)}
        onConfirm={handleDelete}
        title="Delete Vehicle Booking"
        message={`Permanently delete your booking for "${actionTarget?.model}"?`}
        confirmLabel="Delete"
        isDanger
      />
    </div>
  );
}
