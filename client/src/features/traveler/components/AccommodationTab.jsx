import React, { useState, useEffect, useCallback } from 'react';
import Button from '../../../components/Button';
import StatusBadge from '../../../components/StatusBadge';
import EmptyState from '../../../components/EmptyState';
import ErrorBanner from '../../../components/ErrorBanner';
import LoadingSpinner from '../../../components/LoadingSpinner';
import ConfirmDialog from '../../../components/ConfirmDialog';
import BookHotelModal from './BookHotelModal';
import { getDestinations, getMyHotelBookings, cancelHotelBooking, deleteHotelBooking } from '../../../services/travelerApi';

function formatLKR(amount) {
  return `LKR ${Number(amount).toLocaleString('en-LK')}`;
}

function formatDate(dateStr) {
  return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

// AccommodationTab — hotel bookings for this trip's traveler.
// NOTE: HotelBookingSummaryDto does NOT include RoomId, which UpdateHotelBookingDto requires.
// Therefore in-place edit is not supported; to modify, cancel and rebook.
//
// Props:
//   trip — TripDetailDto (or null for global My Bookings page)
export default function AccommodationTab({ trip }) {
  const [bookings, setBookings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [statusFilter, setStatusFilter] = useState('All');

  const [destinations, setDestinations] = useState([]);
  const [bookModalOpen, setBookModalOpen] = useState(false);

  const [cancelConfirmOpen, setCancelConfirmOpen] = useState(false);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [actionTarget, setActionTarget] = useState(null);
  const [actionError, setActionError] = useState(null);

  const fetchBookings = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getMyHotelBookings({ status: statusFilter, pageSize: 100 });
      setBookings(data.items || []);
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load hotel bookings.');
    } finally {
      setLoading(false);
    }
  }, [statusFilter]);

  useEffect(() => { fetchBookings(); }, [fetchBookings]);

  useEffect(() => {
    getDestinations({ pageSize: 200 }).then((d) => setDestinations(d.items || [])).catch(() => {});
  }, []);

  const openCancel = (booking) => { setActionTarget(booking); setActionError(null); setCancelConfirmOpen(true); };
  const openDelete = (booking) => { setActionTarget(booking); setActionError(null); setDeleteConfirmOpen(true); };

  const handleCancel = async () => {
    try {
      setActionError(null);
      await cancelHotelBooking(actionTarget.id);
      fetchBookings();
    } catch (err) {
      setActionError(err.response?.data?.message || 'Cancel failed.');
    }
  };

  const handleDelete = async () => {
    try {
      setActionError(null);
      await deleteHotelBooking(actionTarget.id);
      fetchBookings();
    } catch (err) {
      setActionError(err.response?.data?.message || 'Delete failed.');
    }
  };

  const isHeld = (b) => b.status === 'Held';
  const heldOrConfirmed = (b) => b.status === 'Held' || b.status === 'Confirmed';

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-headline-sm font-heading font-bold text-text mb-1">Accommodation</h2>
          <p className="text-body-sm text-text-secondary">Your hotel bookings (all trips)</p>
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
                Book Hotel
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
          icon="hotel"
          title="No hotel bookings"
          description={statusFilter !== 'All' ? 'No bookings match the current filter.' : (trip ? 'Book your first hotel for this trip.' : 'You have no hotel bookings yet. Navigate to a specific trip to book a hotel.')}
          action={(trip && statusFilter === 'All') && <Button onClick={() => setBookModalOpen(true)}>Book a Hotel</Button>}
        />
      ) : (
        <div className="space-y-4">
          {bookings.map((b) => (
            <div key={b.id} className="bg-white border border-border-neutral rounded-xl p-5 shadow-soft">
              <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-4">
                <div className="flex-1">
                  <div className="flex items-center gap-3 mb-2">
                    <p className="font-heading font-bold text-text">{b.hotelName}</p>
                    <StatusBadge status={b.status} />
                  </div>
                  <p className="text-body-sm text-text-secondary mb-3">
                    {b.roomType} · {b.numberOfRooms} room{b.numberOfRooms !== 1 ? 's' : ''}
                  </p>
                  <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
                    <div>
                      <p className="text-label-uppercase text-text-secondary tracking-widest">Check-in</p>
                      <p className="text-sm font-heading font-bold text-text">{formatDate(b.checkInDate)}</p>
                    </div>
                    <div>
                      <p className="text-label-uppercase text-text-secondary tracking-widest">Check-out</p>
                      <p className="text-sm font-heading font-bold text-text">{formatDate(b.checkOutDate)}</p>
                    </div>
                    <div>
                      <p className="text-label-uppercase text-text-secondary tracking-widest">Total</p>
                      <p className="text-sm font-heading font-bold text-primary">{formatLKR(b.totalPrice)}</p>
                    </div>
                  </div>
                  {isHeld(b) && (
                    <p className="mt-3 text-label-badge text-text-secondary italic">
                      To change dates or room type, cancel this booking and create a new one.
                    </p>
                  )}
                </div>

                {/* Actions — edit button omitted (RoomId not in SummaryDto) */}
                <div className="flex gap-2 shrink-0">
                  {heldOrConfirmed(b) && (
                    <button
                      onClick={() => openCancel(b)}
                      className="p-2 rounded-lg hover:bg-red-50 text-text-secondary hover:text-status-danger transition-colors"
                      title="Cancel booking"
                    >
                      <span className="material-symbols-outlined text-sm">cancel</span>
                    </button>
                  )}
                  {isHeld(b) && (
                    <button
                      onClick={() => openDelete(b)}
                      className="p-2 rounded-lg hover:bg-red-50 text-text-secondary hover:text-status-danger transition-colors"
                      title="Delete booking"
                    >
                      <span className="material-symbols-outlined text-sm">delete</span>
                    </button>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Modals */}
      <BookHotelModal
        isOpen={bookModalOpen}
        onClose={() => setBookModalOpen(false)}
        onSuccess={fetchBookings}
        trip={trip}
        destinations={destinations}
      />
      <ConfirmDialog
        isOpen={cancelConfirmOpen}
        onClose={() => setCancelConfirmOpen(false)}
        onConfirm={handleCancel}
        title="Cancel Hotel Booking"
        message={`Cancel your booking at "${actionTarget?.hotelName}"? The booking will be marked Cancelled.`}
        confirmLabel="Cancel Booking"
        isDanger
      />
      <ConfirmDialog
        isOpen={deleteConfirmOpen}
        onClose={() => setDeleteConfirmOpen(false)}
        onConfirm={handleDelete}
        title="Delete Hotel Booking"
        message={`Permanently delete your booking at "${actionTarget?.hotelName}"?`}
        confirmLabel="Delete"
        isDanger
      />
    </div>
  );
}
