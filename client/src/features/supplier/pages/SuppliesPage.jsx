import React, { useState, useEffect, useCallback, useMemo } from 'react';
import DashboardLayout from '../../../components/DashboardLayout';
import Button from '../../../components/Button';
import SummaryMetricCard from '../../../components/SummaryMetricCard';
import SearchFilterBar from '../../../components/SearchFilterBar';
import StatusBadge from '../../../components/StatusBadge';
import EmptyState from '../../../components/EmptyState';
import ErrorBanner from '../../../components/ErrorBanner';
import ConfirmDialog from '../../../components/ConfirmDialog';
import LoadingSpinner from '../../../components/LoadingSpinner';
import AddEditSupplyModal from '../components/AddEditSupplyModal';
import { getMySupplies, deactivateSupply, republishSupply } from '../../../services/supplierApi';

export const SUPPLIER_NAV_ITEMS = [
  { icon: 'inventory_2', label: 'My Supplies', path: '/supplier/supplies' },
  { icon: 'description', label: 'Contracts', path: '/supplier/contracts' },
  { icon: 'shopping_bag', label: 'Incoming Orders', path: '/supplier/orders' },
];

// Supply statuses from SupplyStatus enum (stored as strings)
const SUPPLY_STATUSES = ['Active', 'Inactive', 'Removed'];

// Format a number as LKR currency
function formatLKR(amount) {
  return `LKR ${Number(amount).toLocaleString('en-LK', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

export default function SuppliesPage() {
  // We fetch all supplies for accurate metrics and do client-side filtering,
  // following the same pattern as MyHotelsPage.
  const [supplies, setSupplies] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Filters
  const [searchTerm, setSearchTerm] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('All');
  const [statusFilter, setStatusFilter] = useState('All');
  const [page, setPage] = useState(1);
  const pageSize = 20;

  // Modals
  const [isSupplyModalOpen, setIsSupplyModalOpen] = useState(false);
  const [editingSupply, setEditingSupply] = useState(null);
  const [deactivateConfirmOpen, setDeactivateConfirmOpen] = useState(false);
  const [supplyToDeactivate, setSupplyToDeactivate] = useState(null);
  const [republishConfirmOpen, setRepublishConfirmOpen] = useState(false);
  const [supplyToRepublish, setSupplyToRepublish] = useState(null);
  const [actionLoading, setActionLoading] = useState(false);
  const [actionError, setActionError] = useState(null);

  const fetchSupplies = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      // Fetch a large page to calculate metrics accurately; backend supports this.
      const data = await getMySupplies({ pageSize: 1000 });
      setSupplies(data.items || []);
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load supplies. Please try again.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchSupplies();
  }, [fetchSupplies]);

  // Derive unique categories from loaded data for the filter dropdown
  const categories = useMemo(() => {
    const cats = [...new Set(supplies.map((s) => s.category).filter(Boolean))].sort();
    return cats;
  }, [supplies]);

  // Summary metrics over all fetched supplies
  const metrics = useMemo(() => ({
    total: supplies.length,
    active: supplies.filter((s) => s.status === 'Active').length,
    inactive: supplies.filter((s) => s.status === 'Inactive').length,
    removed: supplies.filter((s) => s.status === 'Removed').length,
  }), [supplies]);

  // Client-side filtering
  const filteredSupplies = useMemo(() => {
    return supplies.filter((s) => {
      const matchSearch = !searchTerm
        || s.name.toLowerCase().includes(searchTerm.toLowerCase())
        || s.category.toLowerCase().includes(searchTerm.toLowerCase());
      const matchCategory = categoryFilter === 'All' || s.category === categoryFilter;
      const matchStatus = statusFilter === 'All' || s.status === statusFilter;
      return matchSearch && matchCategory && matchStatus;
    });
  }, [supplies, searchTerm, categoryFilter, statusFilter]);

  // Client-side pagination
  const paginatedSupplies = filteredSupplies.slice((page - 1) * pageSize, page * pageSize);
  const totalFilteredCount = filteredSupplies.length;
  const showingFrom = totalFilteredCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const showingTo = Math.min(page * pageSize, totalFilteredCount);
  const totalPages = Math.ceil(totalFilteredCount / pageSize) || 1;

  const handleResetFilters = () => {
    setSearchTerm('');
    setCategoryFilter('All');
    setStatusFilter('All');
    setPage(1);
  };

  const openCreateModal = () => {
    setEditingSupply(null);
    setIsSupplyModalOpen(true);
  };

  const openEditModal = (supply) => {
    setEditingSupply(supply);
    setIsSupplyModalOpen(true);
  };

  const handleDeactivateClick = (supply) => {
    setSupplyToDeactivate(supply);
    setDeactivateConfirmOpen(true);
    setActionError(null);
  };

  const handleConfirmDeactivate = async () => {
    if (!supplyToDeactivate) return;
    try {
      setActionLoading(true);
      await deactivateSupply(supplyToDeactivate.id);
      await fetchSupplies();
    } catch (err) {
      setActionError(err.response?.data?.message || 'Failed to deactivate supply.');
    } finally {
      setActionLoading(false);
    }
  };

  const handleRepublishClick = (supply) => {
    setSupplyToRepublish(supply);
    setRepublishConfirmOpen(true);
    setActionError(null);
  };

  const handleConfirmRepublish = async () => {
    if (!supplyToRepublish) return;
    try {
      setActionLoading(true);
      await republishSupply(supplyToRepublish.id);
      await fetchSupplies();
    } catch (err) {
      setActionError(err.response?.data?.message || 'Failed to republish supply.');
    } finally {
      setActionLoading(false);
    }
  };

  return (
    <DashboardLayout navItems={SUPPLIER_NAV_ITEMS} roleBadge="Supply Partner" profileRoute="/supplier/profile">

      {/* Page Header */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-4 mb-8">
        <div>
          <span className="text-label-uppercase text-primary tracking-widest block mb-2">
            ● SUPPLY MANAGEMENT
          </span>
          <h1 className="text-headline-lg font-heading font-bold text-text mb-1">
            My Supplies
          </h1>
          <p className="text-body-md text-text-secondary">
            Manage your supply listings. An active supplier contract is required to add new items.
          </p>
        </div>
        <Button onClick={openCreateModal}>
          + Add Supply
        </Button>
      </div>

      {/* Action error (deactivate/republish) */}
      {actionError && <ErrorBanner message={actionError} className="mb-6" />}

      {/* Fetch error */}
      {error && !loading && (
        <div className="mb-6">
          <ErrorBanner message={error} />
          <button
            onClick={fetchSupplies}
            className="mt-2 text-primary text-body-sm font-bold hover:underline"
          >
            ↺ Retry
          </button>
        </div>
      )}

      {/* Summary Metric Cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-[var(--space-lg)] mb-8">
        <SummaryMetricCard icon="inventory_2" label="Total Supplies" value={loading ? '—' : metrics.total} />
        <SummaryMetricCard
          icon="check_circle"
          label="Active"
          value={loading ? '—' : metrics.active}
          badge={{ text: 'Active', colorClass: 'bg-status-success/15 text-status-success' }}
        />
        <SummaryMetricCard
          icon="pause_circle"
          label="Inactive"
          value={loading ? '—' : metrics.inactive}
          badge={{ text: 'Inactive', colorClass: 'bg-status-neutral/15 text-status-neutral' }}
        />
        <SummaryMetricCard
          icon="remove_circle"
          label="Removed by Admin"
          value={loading ? '—' : metrics.removed}
          badge={{ text: 'Removed', colorClass: 'bg-status-danger/15 text-status-danger' }}
        />
      </div>

      {/* Search & Filters */}
      <SearchFilterBar
        searchValue={searchTerm}
        onSearchChange={(val) => { setSearchTerm(val); setPage(1); }}
        onReset={handleResetFilters}
        showingFrom={showingFrom}
        showingTo={showingTo}
        totalCount={totalFilteredCount}
      >
        {/* Category filter — built from actual supply data */}
        {categories.length > 0 && (
          <select
            value={categoryFilter}
            onChange={(e) => { setCategoryFilter(e.target.value); setPage(1); }}
            className="border border-border-neutral rounded-md px-3 py-2 bg-white text-body-md focus:outline-none focus:border-primary"
            aria-label="Filter by category"
          >
            <option value="All">All Categories</option>
            {categories.map((cat) => (
              <option key={cat} value={cat}>{cat}</option>
            ))}
          </select>
        )}

        {/* Status filter */}
        <select
          value={statusFilter}
          onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
          className="border border-border-neutral rounded-md px-3 py-2 bg-white text-body-md focus:outline-none focus:border-primary"
          aria-label="Filter by status"
        >
          <option value="All">All Statuses</option>
          {SUPPLY_STATUSES.map((s) => (
            <option key={s} value={s}>{s}</option>
          ))}
        </select>
      </SearchFilterBar>

      {/* Grid area */}
      {loading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-[var(--space-lg)]">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-52 bg-surface-neutral/50 animate-pulse rounded-xl border border-border-neutral" />
          ))}
        </div>
      ) : supplies.length === 0 ? (
        <EmptyState
          icon="inventory_2"
          title="No supplies yet"
          description="Add your first supply item. You'll need an active supplier contract before creating one."
          action={<Button onClick={openCreateModal}>Add your first supply</Button>}
        />
      ) : filteredSupplies.length === 0 ? (
        <EmptyState
          icon="search_off"
          title="No results found"
          description="Try adjusting your search term or filters."
          action={<Button onClick={handleResetFilters} variant="secondary">Reset Filters</Button>}
        />
      ) : (
        <>
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-[var(--space-lg)] mb-8">
            {paginatedSupplies.map((supply) => (
              <SupplyCard
                key={supply.id}
                supply={supply}
                onEdit={() => openEditModal(supply)}
                onDeactivate={() => handleDeactivateClick(supply)}
                onRepublish={() => handleRepublishClick(supply)}
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

      {/* Add/Edit Supply Modal */}
      <AddEditSupplyModal
        isOpen={isSupplyModalOpen}
        onClose={() => setIsSupplyModalOpen(false)}
        onSuccess={fetchSupplies}
        supply={editingSupply}
      />

      {/* Deactivate Confirm Dialog */}
      <ConfirmDialog
        isOpen={deactivateConfirmOpen}
        onClose={() => setDeactivateConfirmOpen(false)}
        onConfirm={handleConfirmDeactivate}
        title="Deactivate Supply"
        message={`Are you sure you want to deactivate "${supplyToDeactivate?.name}"? It will be hidden from travelers. You can republish it later.`}
        confirmLabel={actionLoading ? 'Deactivating…' : 'Deactivate'}
        isDanger
      />

      {/* Republish Confirm Dialog */}
      <ConfirmDialog
        isOpen={republishConfirmOpen}
        onClose={() => setRepublishConfirmOpen(false)}
        onConfirm={handleConfirmRepublish}
        title="Republish Supply"
        message={`Republish "${supplyToRepublish?.name}"? It will become Active and visible to travelers again.`}
        confirmLabel={actionLoading ? 'Republishing…' : 'Republish'}
        isDanger={false}
      />
    </DashboardLayout>
  );
}

// ── Supply Card ───────────────────────────────────────────────────────────────

function SupplyCard({ supply, onEdit, onDeactivate, onRepublish }) {
  const canDeactivate = supply.status === 'Active';
  // Supplier can republish both Inactive (self-deactivated) and Removed (admin-removed) supplies
  const canRepublish = supply.status === 'Inactive' || supply.status === 'Removed';

  return (
    <div className="flex flex-col bg-white border border-border-neutral rounded-xl shadow-soft overflow-hidden hover:border-primary transition-colors">
      {/* Card Header colored by status */}
      <div className={`h-2 w-full ${
        supply.status === 'Active'
          ? 'bg-status-success'
          : supply.status === 'Removed'
            ? 'bg-status-danger'
            : 'bg-status-neutral'
      }`} />

      <div className="p-[var(--space-lg)] flex flex-col flex-1">
        {/* Title row */}
        <div className="flex items-start justify-between gap-2 mb-3">
          <div className="flex-1 min-w-0">
            <h3 className="text-headline-sm font-heading font-bold text-text line-clamp-2 mb-0.5">
              {supply.name}
            </h3>
            <span className="text-body-sm text-text-secondary">{supply.category}</span>
          </div>
          <StatusBadge status={supply.status} />
        </div>

        {/* Removed by admin note */}
        {supply.status === 'Removed' && (
          <div className="bg-status-danger/5 border border-status-danger/20 rounded-md px-3 py-2 mb-3">
            <p className="text-body-sm text-status-danger font-semibold">
              <span className="material-symbols-outlined text-sm align-middle mr-1">warning</span>
              Removed by admin
            </p>
            <p className="text-body-sm text-text-secondary mt-0.5">
              You can still republish this item.
            </p>
          </div>
        )}

        {/* Price & Stock */}
        <div className="grid grid-cols-2 gap-3 mt-auto pt-3 border-t border-border-neutral">
          <div>
            <p className="text-label-uppercase text-text-secondary tracking-wider mb-0.5">Price / Unit</p>
            <p className="text-body-md font-bold text-primary font-heading">
              LKR {Number(supply.pricePerUnit).toLocaleString()}
            </p>
          </div>
          <div>
            <p className="text-label-uppercase text-text-secondary tracking-wider mb-0.5">Stock</p>
            <p className="text-body-md font-bold text-text font-heading">
              {supply.stockQuantity} units
            </p>
          </div>
        </div>

        {/* Actions */}
        <div className="flex items-center gap-2 mt-4 pt-3 border-t border-border-neutral">
          {/* Edit — always available */}
          <button
            onClick={onEdit}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-md text-label-button text-text-secondary hover:bg-surface-neutral hover:text-primary transition-colors"
            aria-label={`Edit ${supply.name}`}
          >
            <span className="material-symbols-outlined text-base">edit</span>
            Edit
          </button>

          {canDeactivate && (
            <button
              onClick={onDeactivate}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-md text-label-button text-text-secondary hover:bg-status-danger/10 hover:text-status-danger transition-colors ml-auto"
              aria-label={`Deactivate ${supply.name}`}
            >
              <span className="material-symbols-outlined text-base">pause_circle</span>
              Deactivate
            </button>
          )}

          {canRepublish && (
            <button
              onClick={onRepublish}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-md text-label-button text-text-secondary hover:bg-status-success/10 hover:text-status-success transition-colors ml-auto"
              aria-label={`Republish ${supply.name}`}
            >
              <span className="material-symbols-outlined text-base">publish</span>
              Republish
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
