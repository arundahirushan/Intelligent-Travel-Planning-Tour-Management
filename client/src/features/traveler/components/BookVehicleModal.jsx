import React, { useState, useEffect, useCallback } from 'react';
import Modal from '../../../components/Modal';
import Button from '../../../components/Button';
import ErrorBanner from '../../../components/ErrorBanner';
import LoadingSpinner from '../../../components/LoadingSpinner';
import PickupLocationPickerModal from '../../../components/PickupLocationPickerModal';
import { searchVehicles, createVehicleBooking } from '../../../services/travelerApi';

function toDateInputValue(date) {
  if (!date) return '';
  return new Date(date).toISOString().split('T')[0];
}

function formatLKR(amount) {
  return `LKR ${Number(amount).toLocaleString('en-LK')}`;
}

// BookVehicleModal — 3-step flow:
//   1. Search: dates, min capacity, max price/day
//   2. Results: available vehicles list
//   3. Confirm: choose pickup location (map picker) and submit
//
// Props:
//   isOpen    — controls visibility
//   onClose   — close handler
//   onSuccess — called after successful booking
//   trip      — TripDetailDto (provides TripId and default date range)
export default function BookVehicleModal({ isOpen, onClose, onSuccess, trip }) {
  const [step, setStep] = useState('search');

  // Search form
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [minCapacity, setMinCapacity] = useState('');
  const [maxPrice, setMaxPrice] = useState('');

  // Results
  const [results, setResults] = useState([]);
  const [searchLoading, setSearchLoading] = useState(false);
  const [searchError, setSearchError] = useState(null);

  // Selected vehicle
  const [selectedVehicle, setSelectedVehicle] = useState(null);

  // Pickup — set via PickupLocationPickerModal
  const [pickupLat, setPickupLat] = useState(null);
  const [pickupLng, setPickupLng] = useState(null);
  const [pickupNote, setPickupNote] = useState('');
  const [pickerOpen, setPickerOpen] = useState(false);

  const [bookingLoading, setBookingLoading] = useState(false);
  const [bookingError, setBookingError] = useState(null);

  useEffect(() => {
    if (isOpen) {
      setStep('search');
      setStartDate(toDateInputValue(trip?.startDate));
      setEndDate(toDateInputValue(trip?.endDate));
      setMinCapacity('');
      setMaxPrice('');
      setResults([]);
      setSearchError(null);
      setSelectedVehicle(null);
      setPickupLat(null);
      setPickupLng(null);
      setPickupNote('');
      setBookingError(null);
    }
  }, [isOpen, trip]);

  const handleSearch = async (e) => {
    e.preventDefault();
    if (!startDate || !endDate) { setSearchError('Please select start and end dates.'); return; }
    if (endDate <= startDate) { setSearchError('End date must be after start date.'); return; }
    setSearchLoading(true);
    setSearchError(null);
    try {
      const data = await searchVehicles({
        startDate,
        endDate,
        minCapacity: minCapacity ? parseInt(minCapacity, 10) : undefined,
        maxPricePerDay: maxPrice ? parseFloat(maxPrice) : undefined,
      });
      setResults(data || []);
      setStep('results');
    } catch (err) {
      setSearchError(err.response?.data?.message || 'Search failed. Please try again.');
    } finally {
      setSearchLoading(false);
    }
  };

  const handleSelectVehicle = (v) => {
    setSelectedVehicle(v);
    setPickupLat(null);
    setPickupLng(null);
    setPickupNote('');
    setBookingError(null);
    setStep('confirm');
  };

  const handlePickerConfirm = ({ lat, lng, note }) => {
    setPickupLat(lat);
    setPickupLng(lng);
    setPickupNote(note);
  };

  const handleBook = async (e) => {
    e.preventDefault();
    if (pickupLat === null || pickupLng === null) {
      setBookingError('Please select a pickup location on the map.');
      return;
    }
    setBookingLoading(true);
    setBookingError(null);
    try {
      await createVehicleBooking({
        TripId: trip.id,
        VehicleId: selectedVehicle.vehicleId,
        StartDate: startDate,
        EndDate: endDate,
        PickupLatitude: pickupLat,
        PickupLongitude: pickupLng,
        PickupNote: pickupNote || null,
      });
      onSuccess();
      onClose();
    } catch (err) {
      setBookingError(err.response?.data?.message || 'Booking failed. Please try again.');
    } finally {
      setBookingLoading(false);
    }
  };

  const days = startDate && endDate
    ? Math.max(0, Math.round((new Date(endDate) - new Date(startDate)) / (1000 * 60 * 60 * 24)))
    : 0;

  const tripStartStr = toDateInputValue(trip?.startDate);
  const tripEndStr = toDateInputValue(trip?.endDate);

  const inputClass = 'bg-white border border-border-neutral rounded-md px-4 py-2.5 font-body text-text outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-all w-full';

  return (
    <>
      <Modal isOpen={isOpen} onClose={onClose} title="Book Transport" size="lg">
        {/* ── Step 1: Search ── */}
        {step === 'search' && (
          <form onSubmit={handleSearch} className="space-y-4">
            {searchError && <ErrorBanner message={searchError} />}
            <p className="text-body-sm text-text-secondary">
              Search available vehicles for your trip. Dates are pre-filled from your trip range.
            </p>

            <div className="grid grid-cols-2 gap-4">
              <div className="flex flex-col">
                <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary" htmlFor="veh-start">Start Date *</label>
                <input id="veh-start" type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} min={tripStartStr} max={tripEndStr} className={inputClass} required />
              </div>
              <div className="flex flex-col">
                <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary" htmlFor="veh-end">End Date *</label>
                <input id="veh-end" type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} min={startDate || tripStartStr} max={tripEndStr} className={inputClass} required />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="flex flex-col">
                <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary" htmlFor="veh-capacity">Min Capacity</label>
                <input id="veh-capacity" type="number" value={minCapacity} onChange={(e) => setMinCapacity(e.target.value)} min="1" step="1" className={inputClass} placeholder="Optional" />
              </div>
              <div className="flex flex-col">
                <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary" htmlFor="veh-price">Max Price/Day (LKR)</label>
                <input id="veh-price" type="number" value={maxPrice} onChange={(e) => setMaxPrice(e.target.value)} min="0" step="1" className={inputClass} placeholder="Optional" />
              </div>
            </div>

            <div className="flex justify-end gap-3 border-t border-border-neutral pt-4">
              <Button type="button" variant="secondary" onClick={onClose}>Cancel</Button>
              <Button type="submit" disabled={searchLoading}>
                {searchLoading ? <LoadingSpinner size="sm" /> : (
                  <span className="flex items-center gap-2"><span className="material-symbols-outlined text-base">search</span> Search Vehicles</span>
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
                <span className="material-symbols-outlined text-5xl text-text-secondary/40 block mb-3">directions_car</span>
                <p className="font-heading font-bold text-text mb-2">No vehicles available</p>
                <p className="text-body-sm text-text-secondary">Try adjusting your dates, capacity, or price filter.</p>
              </div>
            ) : (
              <div className="space-y-3 max-h-96 overflow-y-auto pr-1">
                {results.map((v) => (
                  <div key={v.vehicleId} className="border border-border-neutral rounded-lg p-4 bg-white hover:border-primary transition-colors">
                    <div className="flex justify-between items-start gap-3">
                      <div>
                        <p className="font-heading font-bold text-text">{v.model}</p>
                        <p className="text-body-sm text-text-secondary mb-1">{v.vehicleType} · {v.capacity} seats</p>
                      </div>
                      <div className="text-right shrink-0">
                        <p className="font-heading font-bold text-primary text-headline-sm">{formatLKR(v.pricePerDay)}</p>
                        <p className="text-label-badge text-text-secondary">/ day</p>
                      </div>
                    </div>
                    <Button onClick={() => handleSelectVehicle(v)} className="w-full mt-3">
                      Select — {formatLKR(v.pricePerDay * days)} total
                    </Button>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {step === 'confirm' && selectedVehicle && (
          <form onSubmit={handleBook} className="space-y-4">
            <button type="button" onClick={() => setStep('results')} className="text-primary hover:text-primary-dark font-heading font-bold text-sm flex items-center gap-1">
              <span className="material-symbols-outlined text-sm">arrow_back</span> Back to Results
            </button>

            {bookingError && <ErrorBanner message={bookingError} />}

            <div className="bg-surface-blue border border-border-blue rounded-lg px-4 py-3 grid grid-cols-2 gap-3">
              <div>
                <p className="text-label-uppercase text-text-secondary tracking-widest">Vehicle</p>
                <p className="font-heading font-bold text-text text-sm">{selectedVehicle.model}</p>
                <p className="text-label-badge text-text-secondary">{selectedVehicle.vehicleType} · {selectedVehicle.capacity} seats</p>
              </div>
              <div>
                <p className="text-label-uppercase text-text-secondary tracking-widest">Duration</p>
                <p className="font-heading font-bold text-text text-sm">{days} day{days !== 1 ? 's' : ''}</p>
                <p className="font-heading font-bold text-primary text-sm">{formatLKR(selectedVehicle.pricePerDay * days)} est.</p>
              </div>
            </div>

            {/* Pickup location picker */}
            <div>
              <p className="mb-2 font-heading text-sm font-semibold text-text-secondary">Pickup Location *</p>
              <button
                type="button"
                onClick={() => setPickerOpen(true)}
                className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg border-2 transition-all text-left
                  ${pickupLat !== null ? 'border-status-success bg-status-success/5' : 'border-dashed border-border-neutral hover:border-primary bg-white'}`}
              >
                <span className="material-symbols-outlined text-primary">location_on</span>
                <div>
                  {pickupLat !== null ? (
                    <>
                      <p className="font-heading font-bold text-sm text-text">Location selected</p>
                      <p className="text-label-badge text-text-secondary">{pickupLat.toFixed(5)}, {pickupLng.toFixed(5)}</p>
                      {pickupNote && <p className="text-label-badge text-text-secondary mt-0.5">{pickupNote}</p>}
                    </>
                  ) : (
                    <p className="font-heading font-bold text-sm text-text-secondary">Click to select pickup location on map</p>
                  )}
                </div>
              </button>
            </div>

            <div className="flex justify-end gap-3 border-t border-border-neutral pt-4">
              <Button type="button" variant="secondary" onClick={onClose} disabled={bookingLoading}>Cancel</Button>
              <Button type="submit" disabled={bookingLoading || pickupLat === null}>
                {bookingLoading ? <LoadingSpinner size="sm" /> : 'Confirm Booking'}
              </Button>
            </div>
          </form>
        )}
      </Modal>

      {/* Pickup location picker — outside the booking modal */}
      <PickupLocationPickerModal
        isOpen={pickerOpen}
        onClose={() => setPickerOpen(false)}
        initialLat={pickupLat}
        initialLng={pickupLng}
        initialNote={pickupNote}
        onConfirm={handlePickerConfirm}
      />
    </>
  );
}
