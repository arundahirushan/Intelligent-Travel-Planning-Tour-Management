import React, { useState, useMemo, useEffect, useCallback } from 'react';
import DashboardLayout from '../../../components/DashboardLayout';
import Button from '../../../components/Button';
import SummaryMetricCard from '../../../components/SummaryMetricCard';
import SearchFilterBar from '../../../components/SearchFilterBar';
import StatusBadge from '../../../components/StatusBadge';
import EmptyState from '../../../components/EmptyState';
import ErrorBanner from '../../../components/ErrorBanner';
import LoadingSpinner from '../../../components/LoadingSpinner';
import ConfirmDialog from '../../../components/ConfirmDialog';
import AddEditVehicleModal from '../components/AddEditVehicleModal';
import { getMyVehicles, deactivateVehicle } from '../../../services/transportProviderApi';

const NAV_ITEMS = [
  { icon: 'directions_car', label: 'Vehicles', path: '/transport-provider' },
  { icon: 'book_online', label: 'Bookings', path: '/transport-provider/bookings' },
];

export default function VehiclesPage() {
  const [vehicles, setVehicles] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Fetch all vehicles (large pageSize) for accurate metric calculation + client-side filter.
  // Same pattern as MyHotelsPage.
  const fetchVehicles = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getMyVehicles({ page: 1, pageSize: 1000 });
      setVehicles(data.items || []);
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load vehicles.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchVehicles(); }, [fetchVehicles]);

  // Filters
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('All');
  const [page, setPage] = useState(1);
  const pageSize = 20;

  // Modal state
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingVehicle, setEditingVehicle] = useState(null);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [vehicleToDelete, setVehicleToDelete] = useState(null);

  // Summary metrics
  const totalCount = vehicles.length;
  const activeCount = vehicles.filter(v => v.status === 'Active').length;
  const pendingCount = vehicles.filter(v => v.status === 'PendingApproval').length;
  // Available Today = Active vehicles that are NOT booked today
  const availableTodayCount = vehicles.filter(v => v.status === 'Active' && !v.isBookedToday).length;

  // Client-side filtering (fast, no extra network calls)
  const filteredVehicles = useMemo(() => {
    return vehicles.filter(v => {
      const matchSearch = !searchTerm ||
        v.vehicleType?.toLowerCase().includes(searchTerm.toLowerCase()) ||
        v.model?.toLowerCase().includes(searchTerm.toLowerCase()) ||
        v.registrationNumber?.toLowerCase().includes(searchTerm.toLowerCase());
      const matchStatus = statusFilter === 'All' || v.status === statusFilter;
      return matchSearch && matchStatus;
    });
  }, [vehicles, searchTerm, statusFilter]);

  const paginatedVehicles = filteredVehicles.slice((page - 1) * pageSize, page * pageSize);
  const totalFiltered = filteredVehicles.length;
  const showingFrom = totalFiltered === 0 ? 0 : (page - 1) * pageSize + 1;
  const showingTo = Math.min(page * pageSize, totalFiltered);

  const handleResetFilters = () => {
    setSearchTerm('');
    setStatusFilter('All');
    setPage(1);
  };

  const openCreateModal = () => {
    setEditingVehicle(null);
    setIsModalOpen(true);
  };

  const handleEditClick = (e, vehicle) => {
    e.stopPropagation();
    setEditingVehicle(vehicle);
    setIsModalOpen(true);
  };

  const handleDeleteClick = (e, vehicle) => {
    e.stopPropagation();
    setVehicleToDelete(vehicle);
    setDeleteConfirmOpen(true);
  };

  const handleConfirmDelete = async () => {
    if (vehicleToDelete) {
      await deactivateVehicle(vehicleToDelete.id);
      await fetchVehicles();
    }
  };

  return (
    <DashboardLayout navItems={NAV_ITEMS} roleBadge="Transport Partner" profileRoute="/transport-provider/profile">
      {/* Page Header */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-4 mb-8">
        <div>
          <span className="text-label-uppercase text-primary tracking-widest block mb-2">
            ● TRANSPORT
          </span>
          <h1 className="text-headline-lg font-heading font-bold text-text mb-1">
            My Vehicles &amp; Fleet
          </h1>
          <p className="text-body-md text-text-secondary">
            Manage your vehicle listings and track availability.
          </p>
        </div>
        <Button onClick={openCreateModal}>
          + Add New Vehicle
        </Button>
      </div>

      {error && <div className="mb-6"><ErrorBanner message={error} /></div>}

      {/* Summary Metric Cards */}
      {loading ? (
        <div className="flex justify-center py-12"><LoadingSpinner size="lg" /></div>
      ) : (
        <>
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
            <SummaryMetricCard
              icon="directions_car"
              label="Total Vehicles"
              value={totalCount}
              progressPercentage={100}
            />
            <SummaryMetricCard
              icon="check_circle"
              label="Active"
              value={activeCount}
              badge={{ text: 'Active', colorClass: 'bg-status-success/15 text-status-success' }}
              progressPercentage={totalCount > 0 ? Math.round((activeCount / totalCount) * 100) : 0}
            />
            <SummaryMetricCard
              icon="pending"
              label="Pending Approval"
              value={pendingCount}
              badge={{ text: 'Pending', colorClass: 'bg-status-warning/15 text-status-warning' }}
              progressPercentage={totalCount > 0 ? Math.round((pendingCount / totalCount) * 100) : 0}
            />
            <SummaryMetricCard
              icon="event_available"
              label="Available Today"
              value={availableTodayCount}
              badge={{ text: 'Free', colorClass: 'bg-status-success/15 text-status-success' }}
              progressPercentage={activeCount > 0 ? Math.round((availableTodayCount / activeCount) * 100) : 0}
            />
          </div>

          {/* Search + Filter Bar */}
          <div className="mb-6">
            <SearchFilterBar
              searchPlaceholder="Search by type, model, or registration..."
              searchValue={searchTerm}
              onSearchChange={(v) => { setSearchTerm(v); setPage(1); }}
              totalCount={totalFiltered}
              showingFrom={showingFrom}
              showingTo={showingTo}
              onReset={handleResetFilters}
            >
              <select
                value={statusFilter}
                onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
                className="bg-white border border-border-neutral rounded-md px-3 py-2 font-body text-body-sm text-text outline-none focus:border-primary transition-colors"
              >
                <option value="All">All Statuses</option>
                <option value="Active">Active</option>
                <option value="PendingApproval">Pending Approval</option>
                <option value="Suspended">Suspended</option>
                <option value="Rejected">Rejected</option>
                <option value="Inactive">Inactive</option>
              </select>
            </SearchFilterBar>
          </div>

          {/* Vehicle Cards Grid */}
          {paginatedVehicles.length === 0 ? (
            filteredVehicles.length === 0 && vehicles.length > 0 ? (
              // Filters returned no results
              <EmptyState
                icon="search_off"
                title="No vehicles match your filters"
                description="Try adjusting your search term or status filter."
                action={
                  <button onClick={handleResetFilters} className="text-primary hover:underline font-bold text-sm">
                    Reset Filters
                  </button>
                }
              />
            ) : (
              // No vehicles at all
              <EmptyState
                icon="directions_car"
                title="No vehicles yet"
                description="Add your first vehicle to start accepting bookings."
                action={<Button onClick={openCreateModal}>+ Add New Vehicle</Button>}
              />
            )
          ) : (
            <>
              <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4">
                {paginatedVehicles.map((vehicle) => (
                  <div
                    key={vehicle.id}
                    className="bg-white border border-border-neutral rounded-xl shadow-soft p-[var(--space-lg)] flex flex-col gap-3 hover:border-primary/30 transition-colors"
                  >
                    {/* Card Header: type + model + status badges */}
                    <div className="flex items-start justify-between gap-2">
                      <div className="min-w-0">
                        <p className="font-heading font-bold text-headline-sm text-text truncate">
                          {vehicle.vehicleType} — {vehicle.model}
                        </p>
                        <p className="text-body-sm text-text-secondary font-body mt-0.5">
                          {vehicle.registrationNumber}
                        </p>
                      </div>
                      <div className="flex flex-col items-end gap-1 shrink-0">
                        <StatusBadge status={vehicle.status} />
                        {/* Availability indicator — only meaningful for Active vehicles */}
                        {vehicle.status === 'Active' && (
                          <StatusBadge status={vehicle.isBookedToday ? 'Booked Today' : 'Available Today'} />
                        )}
                      </div>
                    </div>

                    {/* Key facts */}
                    <div className="flex items-center gap-4 text-body-sm text-text-secondary border-t border-border-neutral pt-3">
                      <span className="flex items-center gap-1">
                        <span className="material-symbols-outlined text-base">group</span>
                        {vehicle.capacity} persons
                      </span>
                      <span className="flex items-center gap-1">
                        <span className="material-symbols-outlined text-base">payments</span>
                        LKR {vehicle.pricePerDay?.toLocaleString()}/day
                      </span>
                    </div>

                    {/* Action buttons */}
                    <div className="flex items-center justify-end gap-2 mt-auto pt-1">
                      <button
                        onClick={(e) => handleEditClick(e, vehicle)}
                        className="w-8 h-8 flex items-center justify-center rounded-full text-text-secondary hover:text-primary hover:bg-surface-blue transition-colors"
                        title="Edit vehicle"
                      >
                        <span className="material-symbols-outlined text-lg">edit</span>
                      </button>
                      <button
                        onClick={(e) => handleDeleteClick(e, vehicle)}
                        className="w-8 h-8 flex items-center justify-center rounded-full text-text-secondary hover:text-status-danger hover:bg-status-danger/10 transition-colors"
                        title="Deactivate vehicle"
                      >
                        <span className="material-symbols-outlined text-lg">delete</span>
                      </button>
                    </div>
                  </div>
                ))}
              </div>

              {/* Simple client-side pagination */}
              {Math.ceil(totalFiltered / pageSize) > 1 && (
                <div className="flex items-center justify-between mt-6 p-4 bg-white border border-border-neutral rounded-xl">
                  <button
                    onClick={() => setPage(p => Math.max(1, p - 1))}
                    disabled={page <= 1}
                    className="px-4 py-2 text-label-button border border-border-neutral rounded-md bg-white hover:bg-surface-neutral disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                  >
                    Previous
                  </button>
                  <span className="text-body-sm text-text-secondary">
                    Page {page} of {Math.ceil(totalFiltered / pageSize)}
                  </span>
                  <button
                    onClick={() => setPage(p => p + 1)}
                    disabled={page >= Math.ceil(totalFiltered / pageSize)}
                    className="px-4 py-2 text-label-button border border-border-neutral rounded-md bg-white hover:bg-surface-neutral disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                  >
                    Next
                  </button>
                </div>
              )}
            </>
          )}
        </>
      )}

      <AddEditVehicleModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSuccess={fetchVehicles}
        vehicle={editingVehicle}
      />

      <ConfirmDialog
        isOpen={deleteConfirmOpen}
        onClose={() => setDeleteConfirmOpen(false)}
        onConfirm={handleConfirmDelete}
        title="Deactivate Vehicle"
        message="This will mark the vehicle as Inactive and remove it from search results. Existing bookings are not affected."
        confirmLabel="Deactivate"
        isDanger
      />
    </DashboardLayout>
  );
}

