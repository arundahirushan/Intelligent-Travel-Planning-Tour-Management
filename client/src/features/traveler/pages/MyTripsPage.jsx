import React, { useState, useCallback, useEffect, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import DashboardLayout from '../../../components/DashboardLayout';
import Button from '../../../components/Button';
import SummaryMetricCard from '../../../components/SummaryMetricCard';
import SearchFilterBar from '../../../components/SearchFilterBar';
import StatusBadge from '../../../components/StatusBadge';
import EmptyState from '../../../components/EmptyState';
import ErrorBanner from '../../../components/ErrorBanner';
import LoadingSpinner from '../../../components/LoadingSpinner';
import ConfirmDialog from '../../../components/ConfirmDialog';
import AddEditTripModal from '../components/AddEditTripModal';
import { getMyTrips, cancelTrip } from '../../../services/travelerApi';

const NAV_ITEMS = [
  { icon: 'luggage', label: 'My Trips', path: '/traveler/trips' },
  { icon: 'book_online', label: 'My Bookings', path: '/traveler/bookings' },
];

// Statuses the traveler can filter by. These match TripStatus enum values in the backend.
const TRIP_STATUSES = ['Draft', 'Planned', 'PendingApproval', 'Confirmed', 'Completed', 'Cancelled'];

// Format a date range as "Jan 1 – Jan 10, 2025".
function formatDateRange(start, end) {
  const s = new Date(start);
  const e = new Date(end);
  const opts = { month: 'short', day: 'numeric' };
  return `${s.toLocaleDateString('en-US', opts)} – ${e.toLocaleDateString('en-US', { ...opts, year: 'numeric' })}`;
}

// Calculate trip duration in days (inclusive).
function tripDays(start, end) {
  const diff = Math.round((new Date(end) - new Date(start)) / (1000 * 60 * 60 * 24));
  return diff + 1;
}

// Format a number as LKR currency.
function formatLKR(amount) {
  return `LKR ${Number(amount).toLocaleString('en-LK')}`;
}

export default function MyTripsPage() {
  const navigate = useNavigate();

  // We fetch a large page so we can compute metrics accurately and do client-side filtering.
  // The backend supports server-side search & status filter; we use both here.
  const [allTrips, setAllTrips] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('All');
  const [page, setPage] = useState(1);
  const pageSize = 12;

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingTrip, setEditingTrip] = useState(null);
  const [cancelConfirmOpen, setCancelConfirmOpen] = useState(false);
  const [tripToCancel, setTripToCancel] = useState(null);
  const [actionError, setActionError] = useState(null);

  const fetchTrips = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      // Fetch all trips with a large pageSize so client-side metrics are complete.
      const data = await getMyTrips({ pageSize: 500 });
      setAllTrips(data.items || []);
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load your trips. Please try again.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchTrips();
  }, [fetchTrips]);

  // ── Metrics ───────────────────────────────────────────────────────────────

  const today = new Date();
  today.setHours(0, 0, 0, 0);

  const draftCount = allTrips.filter((t) => t.status === 'Draft').length;
  const completedCount = allTrips.filter((t) => t.status === 'Completed').length;
  const upcomingCount = allTrips.filter((t) => {
    const start = new Date(t.startDate);
    return (
      start >= today &&
      ['Draft', 'Planned', 'Confirmed'].includes(t.status)
    );
  }).length;

  // ── Client-side filtering & pagination ──────────────────────────────────

  const filteredTrips = useMemo(() => {
    return allTrips.filter((t) => {
      const matchSearch = !searchTerm || t.title.toLowerCase().includes(searchTerm.toLowerCase());
      const matchStatus = statusFilter === 'All' || t.status === statusFilter;
      return matchSearch && matchStatus;
    });
  }, [allTrips, searchTerm, statusFilter]);

  const totalCount = filteredTrips.length;
  const totalPages = Math.ceil(totalCount / pageSize) || 1;
  const paginatedTrips = filteredTrips.slice((page - 1) * pageSize, page * pageSize);
  const showingFrom = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const showingTo = Math.min(page * pageSize, totalCount);

  const handleResetFilters = () => {
    setSearchTerm('');
    setStatusFilter('All');
    setPage(1);
  };

  // ── Actions ──────────────────────────────────────────────────────────────

  const openCreateModal = () => {
    setEditingTrip(null);
    setIsModalOpen(true);
  };

  const openEditModal = (e, trip) => {
    e.stopPropagation();
    setEditingTrip(trip);
    setIsModalOpen(true);
  };

  const openCancelConfirm = (e, trip) => {
    e.stopPropagation();
    setTripToCancel(trip);
    setActionError(null);
    setCancelConfirmOpen(true);
  };

  const handleConfirmCancel = async () => {
    if (!tripToCancel) return;
    try {
      setActionError(null);
      await cancelTrip(tripToCancel.id);
      await fetchTrips();
    } catch (err) {
      setActionError(err.response?.data?.message || 'Failed to cancel trip.');
    }
  };

  const handleModalSuccess = (newTripId) => {
    // After creating, navigate to the new trip's details page.
    if (newTripId && !editingTrip) {
      navigate(`/traveler/trips/${newTripId}`);
    } else {
      fetchTrips();
    }
  };

  // Can this trip still be cancelled?
  const isCancellable = (trip) => ['Draft', 'Planned'].includes(trip.status);
  // Can this trip still be edited?
  const isEditable = (trip) => trip.status === 'Draft';

  return (
    <DashboardLayout navItems={NAV_ITEMS} roleBadge="Traveler" profileRoute="/traveler/profile">
      {/* Page Header */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-4 mb-8">
        <div>
          <span className="text-label-uppercase text-primary tracking-widest block mb-2">
            ● MY JOURNEYS
          </span>
          <h1 className="text-headline-lg font-heading font-bold text-text mb-1">My Trips</h1>
          <p className="text-body-md text-text-secondary">
            Plan, manage, and track all your Sri Lanka adventures.
          </p>
        </div>
        <Button onClick={openCreateModal}>
          <span className="flex items-center gap-2">
            <span className="material-symbols-outlined text-base">add</span>
            Create Trip
          </span>
        </Button>
      </div>

      {/* Metric Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-[var(--space-lg)] mb-8">
        <SummaryMetricCard icon="luggage" label="Total Trips" value={allTrips.length} />
        <SummaryMetricCard
          icon="edit_note"
          label="Draft"
          value={draftCount}
          badge={{ text: 'Draft', colorClass: 'bg-status-warning/15 text-status-warning' }}
        />
        <SummaryMetricCard
          icon="event_upcoming"
          label="Upcoming"
          value={upcomingCount}
          badge={{ text: 'Upcoming', colorClass: 'bg-primary/15 text-primary' }}
        />
        <SummaryMetricCard
          icon="check_circle"
          label="Completed"
          value={completedCount}
          badge={{ text: 'Completed', colorClass: 'bg-status-success/15 text-status-success' }}
        />
      </div>

      {/* API error */}
      {error && <ErrorBanner message={error} className="mb-6" />}
      {actionError && <ErrorBanner message={actionError} className="mb-4" />}

      {/* Search & Filter */}
      <SearchFilterBar
        searchPlaceholder="Search by trip title…"
        searchValue={searchTerm}
        onSearchChange={(val) => { setSearchTerm(val); setPage(1); }}
        onReset={handleResetFilters}
        showingFrom={showingFrom}
        showingTo={showingTo}
        totalCount={totalCount}
      >
        <select
          value={statusFilter}
          onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
          className="border border-border-neutral rounded-md px-3 py-2 bg-white text-body-md focus:outline-none focus:border-primary"
        >
          <option value="All">All Statuses</option>
          {TRIP_STATUSES.map((s) => (
            <option key={s} value={s}>
              {s.replace(/([A-Z])/g, ' $1').trim()}
            </option>
          ))}
        </select>
      </SearchFilterBar>

      {/* Grid / States */}
      {loading ? (
        <div className="flex justify-center items-center h-64">
          <LoadingSpinner size="lg" />
        </div>
      ) : allTrips.length === 0 ? (
        <EmptyState
          icon="luggage"
          title="No trips yet"
          description="Create your first trip to start planning your Sri Lanka adventure."
          action={<Button onClick={openCreateModal}>Create your first trip</Button>}
        />
      ) : filteredTrips.length === 0 ? (
        <EmptyState
          icon="search_off"
          title="No trips match your filters"
          description="Try adjusting your search term or status filter."
          action={<Button onClick={handleResetFilters} variant="secondary">Reset Filters</Button>}
        />
      ) : (
        <>
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-[var(--space-lg)] mb-8">
            {paginatedTrips.map((trip) => (
              <TripCard
                key={trip.id}
                trip={trip}
                onOpen={() => navigate(`/traveler/trips/${trip.id}`)}
                onEdit={(e) => openEditModal(e, trip)}
                onCancel={(e) => openCancelConfirm(e, trip)}
                isEditable={isEditable(trip)}
                isCancellable={isCancellable(trip)}
              />
            ))}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between p-[var(--space-md)] bg-white border border-border-neutral rounded-xl">
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page <= 1}
                className="px-4 py-2 text-label-button text-text border border-border-neutral rounded-md bg-white hover:bg-surface-neutral disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                Previous
              </button>
              <span className="text-body-sm text-text-secondary">
                Page {page} of {totalPages}
              </span>
              <button
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={page >= totalPages}
                className="px-4 py-2 text-label-button text-text border border-border-neutral rounded-md bg-white hover:bg-surface-neutral disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                Next
              </button>
            </div>
          )}
        </>
      )}

      {/* Modals */}
      <AddEditTripModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSuccess={handleModalSuccess}
        trip={editingTrip}
      />

      <ConfirmDialog
        isOpen={cancelConfirmOpen}
        onClose={() => setCancelConfirmOpen(false)}
        onConfirm={handleConfirmCancel}
        title="Cancel Trip"
        message={`Are you sure you want to cancel "${tripToCancel?.title}"? This action cannot be undone.`}
        confirmLabel="Cancel Trip"
        isDanger
      />
    </DashboardLayout>
  );
}

// ── Trip Card ─────────────────────────────────────────────────────────────────

function TripCard({ trip, onOpen, onEdit, onCancel, isEditable, isCancellable }) {
  const duration = tripDays(trip.startDate, trip.endDate);

  return (
    <div
      onClick={onOpen}
      className="group flex flex-col bg-white border border-border-neutral rounded-xl shadow-soft overflow-hidden hover:border-primary transition-colors cursor-pointer"
    >
      {/* Gradient header */}
      <div className="h-2 bg-gradient-to-r from-primary to-accent" />

      <div className="p-[var(--space-lg)] flex flex-col flex-1 relative">
        {/* Action buttons — stop event propagation so clicking them doesn't open the trip */}
        <div className="absolute top-[var(--space-lg)] right-[var(--space-lg)] flex gap-2">
          {isEditable && (
            <button
              onClick={onEdit}
              className="w-8 h-8 rounded-full bg-white shadow-soft flex items-center justify-center text-text-secondary hover:text-primary transition-colors"
              aria-label="Edit trip"
            >
              <span className="material-symbols-outlined text-sm">edit</span>
            </button>
          )}
          {isCancellable && (
            <button
              onClick={onCancel}
              className="w-8 h-8 rounded-full bg-white shadow-soft flex items-center justify-center text-text-secondary hover:text-status-danger transition-colors"
              aria-label="Cancel trip"
            >
              <span className="material-symbols-outlined text-sm">cancel</span>
            </button>
          )}
        </div>

        <h3 className="text-headline-sm font-heading font-bold text-text pr-20 mb-1 line-clamp-2">
          {trip.title}
        </h3>

        <div className="flex items-center gap-1.5 text-body-sm text-text-secondary mb-3">
          <span className="material-symbols-outlined text-sm">calendar_month</span>
          {formatDateRange(trip.startDate, trip.endDate)}
        </div>

        <div className="mb-4">
          <StatusBadge status={trip.status} />
        </div>

        <div className="mt-auto grid grid-cols-2 gap-3 pt-3 border-t border-border-neutral">
          <div>
            <p className="text-label-uppercase text-text-secondary tracking-widest mb-0.5">Duration</p>
            <p className="text-body-sm font-heading font-bold text-text">
              {duration} {duration === 1 ? 'day' : 'days'}
            </p>
          </div>
          <div>
            <p className="text-label-uppercase text-text-secondary tracking-widest mb-0.5">Budget</p>
            <p className="text-body-sm font-heading font-bold text-text">
              {formatLKR(trip.budget)}
            </p>
          </div>
        </div>

        <div className="mt-3 flex items-center gap-1.5 text-primary text-body-sm font-heading font-bold group-hover:gap-2.5 transition-all">
          <span className="material-symbols-outlined text-sm">arrow_forward</span>
          View Trip Details
        </div>
      </div>
    </div>
  );
}
