import React, { useState, useEffect, useCallback } from 'react';
import DashboardLayout from '../../../components/DashboardLayout';
import SearchFilterBar from '../../../components/SearchFilterBar';
import DataTable from '../../../components/DataTable';
import StatusBadge from '../../../components/StatusBadge';
import EmptyState from '../../../components/EmptyState';
import ErrorBanner from '../../../components/ErrorBanner';
import { getMyHotelsBookings } from '../../../services/hotelOwnerApi';

const NAV_ITEMS = [
  { icon: 'hotel', label: 'Hotels', path: '/hotel-owner/hotels' },
  { icon: 'bed', label: 'Rooms', path: '/hotel-owner/rooms' },
  { icon: 'book_online', label: 'Bookings', path: '/hotel-owner/bookings' }
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

  const fetchBookings = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getMyHotelsBookings({ search, status, page, pageSize });
      setBookings(data.items || []);
      setTotalCount(data.totalCount || 0);
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load bookings.');
    } finally {
      setLoading(false);
    }
  }, [search, status, page]);

  useEffect(() => {
    fetchBookings();
  }, [fetchBookings]);

  const columns = [
    { key: 'hotelName', label: 'Hotel' },
    { key: 'roomType', label: 'Room Type' },
    { key: 'checkInDate', label: 'Check In', render: (row) => new Date(row.checkInDate).toLocaleDateString() },
    { key: 'checkOutDate', label: 'Check Out', render: (row) => new Date(row.checkOutDate).toLocaleDateString() },
    { key: 'numberOfRooms', label: 'Rooms' },
    { key: 'status', label: 'Status', render: (row) => <StatusBadge status={row.status} /> },
    { key: 'totalPrice', label: 'Total Price', render: (row) => `LKR ${row.totalPrice.toLocaleString()}` }
  ];

  return (
    <DashboardLayout navItems={NAV_ITEMS} roleBadge="Hotel Partner">
      <div className="mb-6">
        <div className="text-label-uppercase text-primary flex items-center gap-2 mb-1">
          <span className="w-2 h-2 rounded-full bg-primary"></span>
          Reservations Overview
        </div>
        <h1 className="text-headline-lg font-heading font-bold text-text">All Bookings</h1>
        <p className="text-body-md text-text-secondary">View all bookings across all your properties.</p>
      </div>

      {error && <div className="mb-4"><ErrorBanner message={error} /></div>}

      <div className="mb-6">
        <SearchFilterBar
          searchPlaceholder="Search by hotel or room type..."
          searchValue={search}
          onSearchChange={setSearch}
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
            <option value="Completed">Completed</option>
          </select>
        </SearchFilterBar>
      </div>

      <div className="bg-white border border-border-neutral rounded-xl overflow-hidden shadow-soft">
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
              description="There are currently no bookings matching your filters." 
            />
          }
        />
      </div>
    </DashboardLayout>
  );
}
