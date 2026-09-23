import React, { useState, useEffect, useCallback } from 'react';
import Modal from '../../../components/Modal';
import Button from '../../../components/Button';
import ErrorBanner from '../../../components/ErrorBanner';
import LoadingSpinner from '../../../components/LoadingSpinner';
import StatusBadge from '../../../components/StatusBadge';
import { searchHotels, createHotelBooking } from '../../../services/travelerApi';

// Format a number as LKR currency.
function formatLKR(amount) {
  return `LKR ${Number(amount).toLocaleString('en-LK')}`;
}

function toDateInputValue(date) {
  if (!date) return '';
  return new Date(date).toISOString().split('T')[0];
}

// BookHotelModal — 3-step flow:
//   1. Search: destination, dates, guests, budget
//   2. Results: scrollable list of available hotel rooms
//   3. Confirm: choose number of rooms and submit
//
// Props:
//   isOpen   — controls visibility
//   onClose  — close handler
//   onSuccess — called after successful booking
//   trip     — TripDetailDto (provides TripId and default date range)
//   destinations — array of { id, name } for the destination dropdown
export default function BookHotelModal({ isOpen, onClose, onSuccess, trip, destinations = [] }) {
  const [step, setStep] = useState('search');  // 'search' | 'results' | 'confirm'

  // Search form
  const [destinationId, setDestinationId] = useState('');
  const [checkIn, setCheckIn] = useState('');
  const [checkOut, setCheckOut] = useState('');
  const [guests, setGuests] = useState('1');
  const [maxBudget, setMaxBudget] = useState('');

  // Results
  const [results, setResults] = useState([]);
  const [searchLoading, setSearchLoading] = useState(false);
  const [searchError, setSearchError] = useState(null);

  // Selected room
  const [selectedRoom, setSelectedRoom] = useState(null);

  // Booking form
  const [numRooms, setNumRooms] = useState('1');
  const [bookingLoading, setBookingLoading] = useState(false);
  const [bookingError, setBookingError] = useState(null);

  useEffect(() => {
    if (isOpen) {
      setStep('search');
      setDestinationId('');
      setCheckIn(toDateInputValue(trip?.startDate));
      setCheckOut(toDateInputValue(trip?.endDate));
      setGuests('1');
      setMaxBudget('');
      setResults([]);
      setSearchError(null);
      setSelectedRoom(null);
      setNumRooms('1');
      setBookingError(null);
    }
  }, [isOpen, trip]);

  const handleSearch = async (e) => {
    e.preventDefault();
    if (!destinationId || !checkIn || !checkOut) {
      setSearchError('Please fill in all required fields.');
      return;
    }
    if (checkOut <= checkIn) {
      setSearchError('Check-out date must be after check-in date.');
      return;
    }
    setSearchLoading(true);
    setSearchError(null);
    try {
      const data = await searchHotels({
        destinationId: parseInt(destinationId, 10),
        checkInDate: checkIn,
        checkOutDate: checkOut,
        numberOfGuests: parseInt(guests, 10) || 1,
        maxBudgetPerNight: maxBudget ? parseFloat(maxBudget) : undefined,
      });
      setResults(data || []);
      setStep('results');
    } catch (err) {
      setSearchError(err.response?.data?.message || 'Search failed. Please try again.');
    } finally {
      setSearchLoading(false);
    }
  };

  const handleSelectRoom = (room) => {
    setSelectedRoom(room);
    setNumRooms('1');
    setBookingError(null);
    setStep('confirm');
  };

  const handleBook = async (e) => {
    e.preventDefault();
    const rooms = parseInt(numRooms, 10);
    if (!rooms || rooms < 1) {
      setBookingError('Number of rooms must be at least 1.');
      return;
    }
    if (rooms > selectedRoom.availableRoomCount) {
      setBookingError(`Only ${selectedRoom.availableRoomCount} room(s) available.`);
      return;
    }
    setBookingLoading(true);
    setBookingError(null);
    try {
      await createHotelBooking({
        TripId: trip.id,
        RoomId: selectedRoom.roomId,
        CheckInDate: checkIn,
        CheckOutDate: checkOut,
        NumberOfRooms: rooms,
      });
      onSuccess();
      onClose();
    } catch (err) {
      console.error('Booking error response:', err.response?.data);
      let errorMsg = err.response?.data?.message;
      if (!errorMsg && err.response?.data?.errors) {
        errorMsg = Object.values(err.response.data.errors).flat().join(' ');
      }
      if (!errorMsg && err.response?.data?.title) {
        errorMsg = err.response.data.title;
      }
      setBookingError(errorMsg || 'Booking failed. Please try again.');
    } finally {
      setBookingLoading(false);
    }
  };

  const nights = checkIn && checkOut
    ? Math.max(0, Math.round((new Date(checkOut) - new Date(checkIn)) / (1000 * 60 * 60 * 24)))
    : 0;

  const tripStartStr = toDateInputValue(trip?.startDate);
  const tripEndStr = toDateInputValue(trip?.endDate);

  const inputClass = 'bg-white border border-border-neutral rounded-md px-4 py-2.5 font-body text-text outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-all w-full';

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Book Accommodation" size="lg">
      {/* ── Step 1: Search ── */}
      {step === 'search' && (
        <form onSubmit={handleSearch} className="space-y-4">
          {searchError && <ErrorBanner message={searchError} />}
          <p className="text-body-sm text-text-secondary">
            Search for available hotels for your trip. Dates are pre-filled from your trip's date range.
          </p>

          <div className="flex flex-col">
            <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary">Destination *</label>
            <select value={destinationId} onChange={(e) => setDestinationId(e.target.value)} className={inputClass} required>
              <option value="">Select a destination…</option>
              {destinations.map((d) => (
                <option key={d.id} value={d.id}>{d.name}{d.region ? ` — ${d.region}` : ''}</option>
              ))}
            </select>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col">
              <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary" htmlFor="hotel-checkin">Check-in *</label>
              <input id="hotel-checkin" type="date" value={checkIn} onChange={(e) => setCheckIn(e.target.value)} min={tripStartStr} max={tripEndStr} className={inputClass} required />
            </div>
            <div className="flex flex-col">
              <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary" htmlFor="hotel-checkout">Check-out *</label>
              <input id="hotel-checkout" type="date" value={checkOut} onChange={(e) => setCheckOut(e.target.value)} min={checkIn || tripStartStr} max={tripEndStr} className={inputClass} required />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col">
              <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary" htmlFor="hotel-guests">Guests *</label>
              <input id="hotel-guests" type="number" value={guests} onChange={(e) => setGuests(e.target.value)} min="1" step="1" className={inputClass} required />
            </div>
            <div className="flex flex-col">
              <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary" htmlFor="hotel-budget">Max Budget/Night (LKR)</label>
              <input id="hotel-budget" type="number" value={maxBudget} onChange={(e) => setMaxBudget(e.target.value)} min="0" step="1" className={inputClass} placeholder="Optional" />
            </div>
          </div>

          <div className="flex justify-end gap-3 border-t border-border-neutral pt-4">
            <Button type="button" variant="secondary" onClick={onClose}>Cancel</Button>
            <Button type="submit" disabled={searchLoading}>
              {searchLoading ? <LoadingSpinner size="sm" /> : (
                <span className="flex items-center gap-2">
                  <span className="material-symbols-outlined text-base">search</span>
                  Search Hotels
                </span>
              )}
            </Button>
          </div>
        </form>
      )}

      {/* ── Step 2: Results ── */}
      {step === 'results' && (
        <div className="space-y-4">
          <div className="flex items-center gap-3">
            <button onClick={() => setStep('search')} className="text-primary hover:text-primary-dark font-heading font-bold text-sm flex items-center gap-1">
              <span className="material-symbols-outlined text-sm">arrow_back</span> Back to Search
            </button>
            <span className="text-body-sm text-text-secondary ml-auto">{results.length} result{results.length !== 1 ? 's' : ''}</span>
          </div>

          {results.length === 0 ? (
            <div className="text-center py-8">
              <span className="material-symbols-outlined text-5xl text-text-secondary/40 block mb-3">hotel</span>
              <p className="font-heading font-bold text-text mb-2">No hotels available</p>
              <p className="text-body-sm text-text-secondary">Try adjusting your dates or budget.</p>
            </div>
          ) : (
            <div className="space-y-3 max-h-96 overflow-y-auto pr-1">
              {results.map((r) => (
                <div key={r.roomId} className="border border-border-neutral rounded-lg p-4 bg-white hover:border-primary transition-colors">
                  <div className="flex justify-between items-start gap-3">
                    <div>
                      <p className="font-heading font-bold text-text">{r.hotelName}</p>
                      <p className="text-body-sm text-text-secondary mb-1">
                        {r.roomType}{r.starRating ? ` · ${'★'.repeat(r.starRating)}` : ''}
                      </p>
                      <p className="text-body-sm text-text-secondary">
                        {r.availableRoomCount} room{r.availableRoomCount !== 1 ? 's' : ''} available
                      </p>
                    </div>
                    <div className="text-right shrink-0">
                      <p className="font-heading font-bold text-primary text-headline-sm">{formatLKR(r.pricePerNight)}</p>
                      <p className="text-label-badge text-text-secondary">/ night</p>
                    </div>
                  </div>
                  <Button onClick={() => handleSelectRoom(r)} className="w-full mt-3">
                    Select — {formatLKR(r.pricePerNight * nights)} total
                  </Button>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {step === 'confirm' && selectedRoom && (
        <form onSubmit={handleBook} className="space-y-5">
          <button type="button" onClick={() => setStep('results')} className="text-primary hover:text-primary-dark font-heading font-bold text-sm flex items-center gap-1">
            <span className="material-symbols-outlined text-sm">arrow_back</span> Back to Results
          </button>

          {bookingError && <ErrorBanner message={bookingError} />}

          <div className="bg-surface-blue border border-border-blue rounded-lg p-4 space-y-2">
            <p className="font-heading font-bold text-text">{selectedRoom.hotelName}</p>
            <p className="text-body-sm text-text-secondary">{selectedRoom.roomType}</p>
            <div className="grid grid-cols-2 gap-3 mt-3">
              <div>
                <p className="text-label-uppercase text-text-secondary tracking-widest">Check-in</p>
                <p className="font-heading font-bold text-sm text-text">{new Date(checkIn).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}</p>
              </div>
              <div>
                <p className="text-label-uppercase text-text-secondary tracking-widest">Check-out</p>
                <p className="font-heading font-bold text-sm text-text">{new Date(checkOut).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}</p>
              </div>
              <div>
                <p className="text-label-uppercase text-text-secondary tracking-widest">Nights</p>
                <p className="font-heading font-bold text-sm text-text">{nights}</p>
              </div>
              <div>
                <p className="text-label-uppercase text-text-secondary tracking-widest">Price/Night</p>
                <p className="font-heading font-bold text-sm text-text">{formatLKR(selectedRoom.pricePerNight)}</p>
              </div>
            </div>
          </div>

          <div className="flex flex-col">
            <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary" htmlFor="num-rooms">
              Number of Rooms (max {selectedRoom.availableRoomCount})
            </label>
            <input
              id="num-rooms"
              type="number"
              value={numRooms}
              onChange={(e) => setNumRooms(e.target.value)}
              min="1"
              max={selectedRoom.availableRoomCount}
              step="1"
              className="bg-white border border-border-neutral rounded-md px-4 py-2.5 font-body text-text outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-all"
            />
          </div>

          {/* Total price preview */}
          {nights > 0 && numRooms > 0 && (
            <div className="flex justify-between items-center bg-surface-neutral rounded-lg px-4 py-3">
              <span className="font-heading font-bold text-text-secondary">Estimated Total</span>
              <span className="font-heading font-bold text-primary text-headline-sm">
                {formatLKR(selectedRoom.pricePerNight * nights * parseInt(numRooms, 10))}
              </span>
            </div>
          )}

          <div className="flex justify-end gap-3 border-t border-border-neutral pt-4">
            <Button type="button" variant="secondary" onClick={onClose} disabled={bookingLoading}>Cancel</Button>
            <Button type="submit" disabled={bookingLoading}>
              {bookingLoading ? <LoadingSpinner size="sm" /> : 'Confirm Booking'}
            </Button>
          </div>
        </form>
      )}
    </Modal>
  );
}
