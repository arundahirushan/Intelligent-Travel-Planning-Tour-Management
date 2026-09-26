import React, { useState, useMemo } from 'react';
import { Link } from 'react-router-dom';
import DashboardLayout from '../../../components/DashboardLayout';
import Button from '../../../components/Button';
import SummaryMetricCard from '../../../components/SummaryMetricCard';
import SearchFilterBar from '../../../components/SearchFilterBar';
import StatusBadge from '../../../components/StatusBadge';
import ProgressBar from '../../../components/ProgressBar';
import EmptyState from '../../../components/EmptyState';
import ErrorBanner from '../../../components/ErrorBanner';
import ConfirmDialog from '../../../components/ConfirmDialog';
import AddEditHotelModal from '../components/AddEditHotelModal';
import { useMyHotels } from '../hooks/useMyHotels';
import { deactivateHotel } from '../../../services/hotelOwnerApi';

const NAV_ITEMS = [
  { icon: 'hotel', label: 'Hotels', path: '/hotel-owner/hotels' },
  { icon: 'bed', label: 'Rooms', path: '/hotel-owner/rooms' },
  { icon: 'book_online', label: 'Bookings', path: '/hotel-owner/bookings' }
];

export default function MyHotelsPage() {
  const [page, setPage] = useState(1);
  const pageSize = 20;
  
  // We fetch all hotels for the owner (using a large pageSize) to calculate metrics accurately
  // and perform client-side filtering as specified in the plan.
  const { hotels, loading, error, refetch } = useMyHotels({ page: 1, pageSize: 1000 });

  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('All');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingHotel, setEditingHotel] = useState(null);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [hotelToDelete, setHotelToDelete] = useState(null);

  // Metrics (calculated over all fetched hotels)
  const activeCount = hotels.filter(h => h.status === 'Active').length;
  const pendingCount = hotels.filter(h => h.status === 'PendingApproval').length;
  const avgOccupancy = hotels.length > 0 
    ? Math.round(hotels.reduce((acc, h) => acc + h.occupancyPercentage, 0) / hotels.length)
    : 0;

  // Client-side filtering
  const filteredHotels = useMemo(() => {
    return hotels.filter(h => {
      const matchSearch = h.name.toLowerCase().includes(searchTerm.toLowerCase());
      const matchStatus = statusFilter === 'All' || h.status === statusFilter;
      return matchSearch && matchStatus;
    });
  }, [hotels, searchTerm, statusFilter]);

  // Client-side pagination (since we fetched all)
  const paginatedHotels = filteredHotels.slice((page - 1) * pageSize, page * pageSize);
  const totalFilteredCount = filteredHotels.length;
  const showingFrom = totalFilteredCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const showingTo = Math.min(page * pageSize, totalFilteredCount);
  const totalPages = Math.ceil(totalFilteredCount / pageSize) || 1;

  const handleResetFilters = () => {
    setSearchTerm('');
    setStatusFilter('All');
    setPage(1);
  };

  const handleEditClick = (e, hotel) => {
    e.preventDefault(); // Prevent Link navigation
    setEditingHotel(hotel);
    setIsModalOpen(true);
  };

  const handleDeleteClick = (e, hotel) => {
    e.preventDefault();
    setHotelToDelete(hotel);
    setDeleteConfirmOpen(true);
  };

  const handleConfirmDelete = async () => {
    if (hotelToDelete) {
      await deactivateHotel(hotelToDelete.id);
      await refetch();
    }
  };

  const openCreateModal = () => {
    setEditingHotel(null);
    setIsModalOpen(true);
  };

  return (
    <DashboardLayout navItems={NAV_ITEMS} roleBadge="Hotel Partner" profileRoute="/hotel-owner/profile">
      {/* Page Header */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-4 mb-8">
        <div>
          <span className="text-label-uppercase text-primary tracking-widest block mb-2">
            ● ACCOMMODATIONS
          </span>
          <h1 className="text-headline-lg font-heading font-bold text-text mb-1">
            My Hotels & Properties
          </h1>
          <p className="text-body-md text-text-secondary">
            Manage your hotel listings, room types, and availability.
          </p>
        </div>
        <Button onClick={openCreateModal}>
          + Add New Hotel
        </Button>
      </div>

      {/* Metric Cards Row */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-[var(--space-lg)] mb-8">
        <SummaryMetricCard 
          icon="hotel" 
          label="Total Hotels" 
          value={hotels.length} 
        />
        <SummaryMetricCard 
          icon="check_circle" 
          label="Active" 
          value={activeCount}
          badge={{ text: 'Active', colorClass: 'bg-status-success/15 text-status-success' }}
        />
        <SummaryMetricCard 
          icon="hourglass_top" 
          label="Pending Approval" 
          value={pendingCount}
          badge={{ text: 'Pending', colorClass: 'bg-status-warning/15 text-status-warning' }}
        />
        <SummaryMetricCard 
          icon="bar_chart" 
          label="Avg. Occupancy" 
          value={`${avgOccupancy}%`}
          progressPercentage={avgOccupancy}
        />
      </div>

      {error && <ErrorBanner message={error} className="mb-6" />}

      {/* Search & Filter */}
      <SearchFilterBar
        searchValue={searchTerm}
        onSearchChange={(val) => { setSearchTerm(val); setPage(1); }}
        onReset={handleResetFilters}
        showingFrom={showingFrom}
        showingTo={showingTo}
        totalCount={totalFilteredCount}
      >
        <select
          value={statusFilter}
          onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
          className="border border-border-neutral rounded-md px-3 py-2 bg-white text-body-md focus:outline-none focus:border-primary"
        >
          <option value="All">All Statuses</option>
          <option value="Active">Active</option>
          <option value="PendingApproval">Pending Approval</option>
          <option value="Suspended">Suspended</option>
          <option value="Rejected">Rejected</option>
          <option value="Inactive">Inactive</option>
        </select>
      </SearchFilterBar>

      {/* Grid Area */}
      {loading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-[var(--space-lg)]">
          {[1, 2, 3].map(i => (
            <div key={i} className="h-64 bg-surface-neutral/50 animate-pulse rounded-xl border border-border-neutral"></div>
          ))}
        </div>
      ) : hotels.length === 0 ? (
        <EmptyState 
          icon="hotel" 
          title="No hotels yet" 
          description="Create your first hotel listing to start managing bookings."
          action={<Button onClick={openCreateModal}>Add your first hotel</Button>}
        />
      ) : filteredHotels.length === 0 ? (
        <EmptyState 
          icon="search_off" 
          title="No results found" 
          description="Try adjusting your filters or search term."
          action={<Button onClick={handleResetFilters} variant="secondary">Reset Filters</Button>}
        />
      ) : (
        <>
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-[var(--space-lg)] mb-8">
            {paginatedHotels.map(hotel => (
              <Link 
                key={hotel.id} 
                to={`/hotel-owner/hotels/${hotel.id}`}
                className="group flex flex-col bg-white border border-border-neutral rounded-xl shadow-soft overflow-hidden hover:border-primary transition-colors"
              >
                {hotel.imageUrl ? (
                  <img src={hotel.imageUrl} alt={hotel.name} className="w-full aspect-video object-cover" />
                ) : (
                  <div className="w-full aspect-video bg-surface-blue flex items-center justify-center text-primary/50">
                    <span className="material-symbols-outlined text-[4rem]">hotel</span>
                  </div>
                )}
                
                <div className="p-[var(--space-lg)] flex flex-col flex-1 relative">
                  <div className="absolute top-[var(--space-lg)] right-[var(--space-lg)] flex gap-2">
                    <button 
                      onClick={(e) => handleEditClick(e, hotel)}
                      className="w-8 h-8 rounded-full bg-white shadow-soft flex items-center justify-center text-text-secondary hover:text-primary transition-colors"
                      aria-label="Edit hotel"
                    >
                      <span className="material-symbols-outlined text-sm">edit</span>
                    </button>
                    <button 
                      onClick={(e) => handleDeleteClick(e, hotel)}
                      className="w-8 h-8 rounded-full bg-white shadow-soft flex items-center justify-center text-text-secondary hover:text-status-danger transition-colors"
                      aria-label="Delete hotel"
                    >
                      <span className="material-symbols-outlined text-sm">delete</span>
                    </button>
                  </div>

                  <h3 className="text-headline-sm font-heading font-bold text-text pr-20 mb-1 line-clamp-1">
                    {hotel.name}
                  </h3>
                  <div className="text-body-sm text-text-secondary mb-3">
                    {hotel.destinationName}
                  </div>
                  
                  <div className="flex items-center gap-3 mb-4">
                    <StatusBadge status={hotel.status} />
                    <div className="text-status-warning text-sm tracking-widest">
                      {hotel.starRating ? '★'.repeat(hotel.starRating) : 'Unrated'}
                    </div>
                  </div>

                  <div className="mt-auto">
                    <ProgressBar 
                      percentage={hotel.occupancyPercentage} 
                      label={`${hotel.occupancyPercentage}% occupied`} 
                    />
                  </div>
                </div>
              </Link>
            ))}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between p-[var(--space-md)] bg-white border border-border-neutral rounded-xl">
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page <= 1}
                className="px-4 py-2 text-label-button text-text border border-border-neutral rounded-md bg-white hover:bg-surface-neutral disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                Previous
              </button>
              <span className="text-body-sm text-text-secondary">
                Page {page} of {totalPages}
              </span>
              <button
                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
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
      <AddEditHotelModal 
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSuccess={() => refetch()}
        hotel={editingHotel}
      />
      
      <ConfirmDialog
        isOpen={deleteConfirmOpen}
        onClose={() => setDeleteConfirmOpen(false)}
        onConfirm={handleConfirmDelete}
        title="Delete / Deactivate Hotel"
        message="Are you sure you want to delete this hotel? If it has no bookings, it will be permanently deleted. Otherwise, it will be deactivated and hidden from travelers."
        confirmLabel="Delete"
        isDanger
      />
    </DashboardLayout>
  );
}
