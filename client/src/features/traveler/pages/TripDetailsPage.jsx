import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import DashboardLayout from '../../../components/DashboardLayout';
import StatusBadge from '../../../components/StatusBadge';
import ErrorBanner from '../../../components/ErrorBanner';
import LoadingSpinner from '../../../components/LoadingSpinner';
import Button from '../../../components/Button';
import ConfirmDialog from '../../../components/ConfirmDialog';
import AddEditTripModal from '../components/AddEditTripModal';
import ItineraryTab from '../components/ItineraryTab';
import AccommodationTab from '../components/AccommodationTab';
import TransportTab from '../components/TransportTab';
import SuppliesTab from '../components/SuppliesTab';
import { getTripById, cancelTrip } from '../../../services/travelerApi';

const NAV_ITEMS = [
  { icon: 'luggage', label: 'My Trips', path: '/traveler/trips' },
  { icon: 'book_online', label: 'My Bookings', path: '/traveler/bookings' },
];

const TABS = [
  { key: 'overview', label: 'Overview', icon: 'info' },
  { key: 'itinerary', label: 'Itinerary', icon: 'map' },
  { key: 'accommodation', label: 'Accommodation', icon: 'hotel' },
  { key: 'transport', label: 'Transport', icon: 'directions_car' },
  { key: 'supplies', label: 'Supplies', icon: 'inventory_2' },
];

function formatDate(dateStr) {
  return new Date(dateStr).toLocaleDateString('en-US', {
    weekday: 'short', month: 'short', day: 'numeric', year: 'numeric',
  });
}

function formatLKR(amount) {
  return `LKR ${Number(amount).toLocaleString('en-LK')}`;
}

function tripDays(start, end) {
  return Math.round((new Date(end) - new Date(start)) / (1000 * 60 * 60 * 24)) + 1;
}

export default function TripDetailsPage() {
  const { tripId } = useParams();
  const navigate = useNavigate();

  const [trip, setTrip] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [activeTab, setActiveTab] = useState('overview');

  const [editModalOpen, setEditModalOpen] = useState(false);
  const [cancelConfirmOpen, setCancelConfirmOpen] = useState(false);
  const [cancelError, setCancelError] = useState(null);

  const fetchTrip = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getTripById(tripId);
      setTrip(data);
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load trip details.');
    } finally {
      setLoading(false);
    }
  }, [tripId]);

  useEffect(() => { fetchTrip(); }, [fetchTrip]);

  const handleCancelConfirm = async () => {
    try {
      setCancelError(null);
      await cancelTrip(tripId);
      await fetchTrip();
    } catch (err) {
      setCancelError(err.response?.data?.message || 'Failed to cancel trip.');
    }
  };

  const handleTripUpdate = (updatedTrip) => {
    setTrip(updatedTrip);
  };

  if (loading) {
    return (
      <DashboardLayout navItems={NAV_ITEMS} roleBadge="Traveler" profileRoute="/traveler/profile">
        <div className="flex justify-center items-center h-64"><LoadingSpinner size="lg" /></div>
      </DashboardLayout>
    );
  }

  if (error || !trip) {
    return (
      <DashboardLayout navItems={NAV_ITEMS} roleBadge="Traveler" profileRoute="/traveler/profile">
        <ErrorBanner message={error || 'Trip not found.'} />
        <div className="mt-4">
          <Link to="/traveler/trips" className="text-primary hover:underline font-heading font-bold">
            ← Back to My Trips
          </Link>
        </div>
      </DashboardLayout>
    );
  }

  const duration = tripDays(trip.startDate, trip.endDate);
  const isEditable = trip.status === 'Draft';
  const isCancellable = ['Draft', 'Planned'].includes(trip.status);

  return (
    <DashboardLayout navItems={NAV_ITEMS} roleBadge="Traveler" profileRoute="/traveler/profile">
      {/* Back link */}
      <Link
        to="/traveler/trips"
        className="inline-flex items-center gap-2 text-text-secondary hover:text-primary font-heading font-bold text-sm mb-6 transition-colors"
      >
        <span className="material-symbols-outlined text-sm">arrow_back</span>
        Back to My Trips
      </Link>

      {/* Trip Header */}
      <div className="bg-white border border-border-neutral rounded-2xl shadow-soft overflow-hidden mb-8">
        <div className="h-1.5 bg-gradient-to-r from-primary to-accent" />
        <div className="p-[var(--space-xl)]">
          <div className="flex flex-col lg:flex-row lg:items-start justify-between gap-6">
            <div className="flex-1">
              <div className="flex flex-wrap items-center gap-3 mb-3">
                <span className="text-label-uppercase text-primary tracking-widest font-heading font-bold">
                  ● TRIP DETAILS
                </span>
                <StatusBadge status={trip.status} />
              </div>
              <h1 className="text-headline-md font-heading font-bold text-text mb-4">{trip.title}</h1>

              <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
                <div>
                  <p className="text-label-uppercase text-text-secondary tracking-widest mb-1">Start</p>
                  <p className="text-body-sm font-heading font-bold text-text">{formatDate(trip.startDate)}</p>
                </div>
                <div>
                  <p className="text-label-uppercase text-text-secondary tracking-widest mb-1">End</p>
                  <p className="text-body-sm font-heading font-bold text-text">{formatDate(trip.endDate)}</p>
                </div>
                <div>
                  <p className="text-label-uppercase text-text-secondary tracking-widest mb-1">Duration</p>
                  <p className="text-body-sm font-heading font-bold text-text">{duration} day{duration !== 1 ? 's' : ''}</p>
                </div>
                <div>
                  <p className="text-label-uppercase text-text-secondary tracking-widest mb-1">Budget</p>
                  <p className="text-body-sm font-heading font-bold text-primary">{formatLKR(trip.budget)}</p>
                </div>
              </div>

              {(trip.groupSize || trip.interests) && (
                <div className="flex flex-wrap gap-4 mt-4">
                  {trip.groupSize && (
                    <div className="flex items-center gap-2 text-body-sm text-text-secondary">
                      <span className="material-symbols-outlined text-sm">group</span>
                      {trip.groupSize} traveller{trip.groupSize !== 1 ? 's' : ''}
                    </div>
                  )}
                  {trip.interests && (
                    <div className="flex items-center gap-2 text-body-sm text-text-secondary">
                      <span className="material-symbols-outlined text-sm">interests</span>
                      {trip.interests}
                    </div>
                  )}
                </div>
              )}
            </div>

            {/* Actions */}
            <div className="flex gap-3 shrink-0">
              {isEditable && (
                <Button onClick={() => setEditModalOpen(true)} variant="secondary">
                  <span className="flex items-center gap-2">
                    <span className="material-symbols-outlined text-sm">edit</span>
                    Edit
                  </span>
                </Button>
              )}
              {isCancellable && (
                <Button
                  onClick={() => setCancelConfirmOpen(true)}
                  className="bg-white border border-status-danger text-status-danger hover:bg-red-50 px-5 py-2.5 rounded-pill font-heading font-bold text-label-button transition-colors"
                >
                  <span className="flex items-center gap-2">
                    <span className="material-symbols-outlined text-sm">cancel</span>
                    Cancel Trip
                  </span>
                </Button>
              )}
            </div>
          </div>

          {cancelError && <ErrorBanner message={cancelError} className="mt-4" />}
        </div>
      </div>

      {/* Tabs */}
      <div className="mb-6">
        <div className="flex gap-1 overflow-x-auto border-b border-border-neutral pb-0">
          {TABS.map((tab) => (
            <button
              key={tab.key}
              onClick={() => setActiveTab(tab.key)}
              className={`flex items-center gap-2 px-4 py-3 font-heading font-bold text-sm whitespace-nowrap border-b-2 transition-colors
                ${activeTab === tab.key
                  ? 'border-primary text-primary'
                  : 'border-transparent text-text-secondary hover:text-text'
                }`}
            >
              <span className="material-symbols-outlined text-sm">{tab.icon}</span>
              {tab.label}
            </button>
          ))}
        </div>
      </div>

      {/* Tab Content */}
      <div>
        {activeTab === 'overview' && <OverviewTab trip={trip} />}
        {activeTab === 'itinerary' && (
          <ItineraryTab trip={trip} onTripUpdate={handleTripUpdate} />
        )}
        {activeTab === 'accommodation' && <AccommodationTab trip={trip} />}
        {activeTab === 'transport' && <TransportTab trip={trip} />}
        {activeTab === 'supplies' && <SuppliesTab trip={trip} />}
      </div>

      {/* Modals */}
      <AddEditTripModal
        isOpen={editModalOpen}
        onClose={() => setEditModalOpen(false)}
        onSuccess={() => fetchTrip()}
        trip={trip}
      />
      <ConfirmDialog
        isOpen={cancelConfirmOpen}
        onClose={() => setCancelConfirmOpen(false)}
        onConfirm={handleCancelConfirm}
        title="Cancel Trip"
        message={`Are you sure you want to cancel "${trip.title}"? This action cannot be undone.`}
        confirmLabel="Cancel Trip"
        isDanger
      />
    </DashboardLayout>
  );
}

// ── Overview Tab ──────────────────────────────────────────────────────────────

function OverviewTab({ trip }) {
  const items = trip.itineraryItems || [];
  const destinations = [...new Set(items.map((i) => i.destinationName))];

  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
      {/* Itinerary Summary */}
      <div className="bg-white border border-border-neutral rounded-xl p-6 shadow-soft">
        <div className="flex items-center gap-3 mb-4">
          <div className="w-10 h-10 rounded-lg bg-surface-blue flex items-center justify-center">
            <span className="material-symbols-outlined text-primary">map</span>
          </div>
          <div>
            <p className="font-heading font-bold text-text">Itinerary</p>
            <p className="text-body-sm text-text-secondary">{items.length} item{items.length !== 1 ? 's' : ''}</p>
          </div>
        </div>

        {destinations.length === 0 ? (
          <p className="text-body-sm text-text-secondary">No destinations planned yet.</p>
        ) : (
          <div className="flex flex-wrap gap-2">
            {destinations.map((d) => (
              <span key={d} className="px-3 py-1 bg-surface-blue rounded-pill text-body-sm font-heading font-bold text-primary">
                {d}
              </span>
            ))}
          </div>
        )}
      </div>

      {/* Trip Info Card */}
      <div className="bg-white border border-border-neutral rounded-xl p-6 shadow-soft">
        <div className="flex items-center gap-3 mb-4">
          <div className="w-10 h-10 rounded-lg bg-surface-blue flex items-center justify-center">
            <span className="material-symbols-outlined text-primary">info</span>
          </div>
          <p className="font-heading font-bold text-text">Trip Information</p>
        </div>
        <div className="space-y-3">
          <Row label="Status" value={<StatusBadge status={trip.status} />} />
          <Row label="Created" value={new Date(trip.createdAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })} />
          <Row label="Last Updated" value={new Date(trip.updatedAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })} />
          {trip.interests && <Row label="Interests" value={trip.interests} />}
          {trip.groupSize && <Row label="Group Size" value={`${trip.groupSize} people`} />}
        </div>
      </div>

      {/* Quick actions */}
      <div className="bg-gradient-to-br from-primary to-accent rounded-xl p-6 text-white lg:col-span-2">
        <p className="font-heading font-bold text-lg mb-1">Ready to plan your Sri Lanka adventure?</p>
        <p className="text-white/80 text-body-sm mb-5">
          Build your day-by-day itinerary, then book accommodation, transport, and supplies — all in one place.
        </p>
        <div className="flex flex-wrap gap-3">
          {[
            { icon: 'map', label: 'Build Itinerary', tab: 'itinerary' },
            { icon: 'hotel', label: 'Book Hotel', tab: 'accommodation' },
            { icon: 'directions_car', label: 'Book Vehicle', tab: 'transport' },
            { icon: 'inventory_2', label: 'Order Supplies', tab: 'supplies' },
          ].map((a) => (
            <button
              key={a.tab}
              onClick={() => {
                // Scroll to tabs area
                document.getElementById('trip-tabs-anchor')?.scrollIntoView({ behavior: 'smooth' });
                // Navigate to the tab via a small delay so the scroll can start
                setTimeout(() => {
                  const event = new CustomEvent('set-trip-tab', { detail: a.tab });
                  window.dispatchEvent(event);
                }, 50);
              }}
              className="inline-flex items-center gap-2 px-4 py-2 bg-white/20 hover:bg-white/30 rounded-pill text-white font-heading font-bold text-sm transition-colors"
            >
              <span className="material-symbols-outlined text-sm">{a.icon}</span>
              {a.label}
            </button>
          ))}
        </div>
      </div>
    </div>
  );
}

function Row({ label, value }) {
  return (
    <div className="flex items-center justify-between gap-4">
      <p className="text-body-sm text-text-secondary shrink-0">{label}</p>
      <p className="text-body-sm font-heading font-bold text-text text-right">{value}</p>
    </div>
  );
}
