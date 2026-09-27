import React, { useState, useEffect, useCallback } from 'react';
import DashboardLayout from '../../../components/DashboardLayout';
import SearchFilterBar from '../../../components/SearchFilterBar';
import DataTable from '../../../components/DataTable';
import StatusBadge from '../../../components/StatusBadge';
import EmptyState from '../../../components/EmptyState';
import ErrorBanner from '../../../components/ErrorBanner';
import PickupLocationModal from '../../../components/PickupLocationModal';
import { getMyVehiclesBookings } from '../../../services/transportProviderApi';

const NAV_ITEMS = [
  { icon: 'directions_car', label: 'Vehicles', path: '/transport-provider' },
  { icon: 'book_online', label: 'Bookings', path: '/transport-provider/bookings' },
];

export default function BookingsPage() {
  const [bookings, setBookings] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Filters
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('All');
  const [page, setPage] = useState(1);
  const pageSize = 20;

  // Pickup location modal state
  const [pickupModalOpen, setPickupModalOpen] = useState(false);
  const [selectedBooking, setSelectedBooking] = useState(null);

  const fetchBookings = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getMyVehiclesBookings({ search, status, page, pageSize });
      setBookings(data.items || []);
      setTotalCount(data.totalCount || 0);
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load bookings.');
    } finally {
      setLoading(false);
    }
  }, [search, status, page]);

  useEffect(() => { fetchBookings(); }, [fetchBookings]);

  const handleViewPickup = (booking) => {
    setSelectedBooking(booking);
    setPickupModalOpen(true);
  };

  // DataTable column definitions — field names match VehicleBookingSummaryDto exactly
  const columns = [
    {
      key: 'vehicle',
      label: 'Vehicle',
      render: (row) => (
        <div>
          <p className="font-heading font-semibold text-text">{row.vehicleType} — {row.model}</p>
          <p className="text-body-sm text-text-secondary">{row.registrationNumber}</p>
        </div>
      ),
    },
    {
      key: 'startDate',
      label: 'Start Date',
      render: (row) => new Date(row.startDate).toLocaleDateString(),
    },
    {
      key: 'endDate',
      label: 'End Date',
      render: (row) => new Date(row.endDate).toLocaleDateString(),
    },
    {
      key: 'status',
      label: 'Status',
      render: (row) => <StatusBadge status={row.status} />,
    },
    {
      key: 'totalPrice',
      label: 'Total Price',
      render: (row) => `LKR ${row.totalPrice?.toLocaleString()}`,
    },
    {
      key: 'pickup',
      label: 'Pickup Location',
      render: (row) => (
        <button
          onClick={() => handleViewPickup(row)}
          className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-md border border-border-neutral text-body-sm font-heading font-bold text-text hover:bg-surface-blue hover:border-primary hover:text-primary transition-colors"
        >
          <span className="material-symbols-outlined text-base">location_on</span>
          View
        </button>
      ),
    },
  ];

  return (
    <DashboardLayout navItems={NAV_ITEMS} roleBadge="Transport Provider" profileRoute="/transport-provider/profile">
      {/* Page Header */}
      <div className="mb-6">
        <div className="text-label-uppercase text-primary flex items-center gap-2 mb-1">
          <span className="w-2 h-2 rounded-full bg-primary" />
          Reservations Overview
        </div>
        <h1 className="text-headline-lg font-heading font-bold text-text">Vehicle Bookings</h1>
        <p className="text-body-md text-text-secondary">All bookings across your fleet.</p>
      </div>

      {error && <div className="mb-4"><ErrorBanner message={error} /></div>}

      {/* Search + Filter */}
      <div className="mb-6">
        <SearchFilterBar
          searchPlaceholder="Search by type, model, or registration..."
          searchValue={search}
          onSearchChange={(v) => { setSearch(v); setPage(1); }}
          totalCount={totalCount}
          onReset={() => { setSearch(''); setStatus('All'); setPage(1); }}
        >
          <select
            value={status}
            onChange={(e) => { setStatus(e.target.value); setPage(1); }}
            className="bg-white border border-border-neutral rounded-md px-3 py-2 font-body text-body-sm text-text outline-none focus:border-primary transition-colors"
          >
            <option value="All">All Statuses</option>
            <option value="Held">Held</option>
            <option value="Confirmed">Confirmed</option>
            <option value="Cancelled">Cancelled</option>
          </select>
        </SearchFilterBar>
      </div>

      {/* Data Table */}
      <DataTable
        columns={columns}
        rows={bookings}
        isLoading={loading}
        page={page}
        pageSize={pageSize}
        totalCount={totalCount}
        onPageChange={setPage}
        emptyState={
          <EmptyState
            icon="event_busy"
            title="No bookings found"
            description="There are no bookings matching your current filters."
          />
        }
      />

      {/* Pickup Location Modal */}
      <PickupLocationModal
        isOpen={pickupModalOpen}
        onClose={() => { setPickupModalOpen(false); setSelectedBooking(null); }}
        latitude={selectedBooking?.pickupLatitude}
        longitude={selectedBooking?.pickupLongitude}
        note={selectedBooking?.pickupNote}
      />
    </DashboardLayout>
  );
}

