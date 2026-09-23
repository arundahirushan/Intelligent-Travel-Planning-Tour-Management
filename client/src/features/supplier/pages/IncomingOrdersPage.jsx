import React, { useState, useEffect, useCallback } from 'react';
import DashboardLayout from '../../../components/DashboardLayout';
import SummaryMetricCard from '../../../components/SummaryMetricCard';
import EmptyState from '../../../components/EmptyState';
import ErrorBanner from '../../../components/ErrorBanner';
import StatusBadge from '../../../components/StatusBadge';
import { getReceivedOrders } from '../../../services/supplierApi';
import { SUPPLIER_NAV_ITEMS } from './SuppliesPage';

// BookingStatus values from backend (shared between hotel bookings and supply orders)
const ORDER_STATUSES = ['Held', 'Confirmed', 'Cancelled'];

export default function IncomingOrdersPage() {
  const [orders, setOrders] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Filters & pagination (server-side because orders can be numerous)
  const [statusFilter, setStatusFilter] = useState('All');
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const fetchOrders = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getReceivedOrders({
        status: statusFilter === 'All' ? null : statusFilter,
        page,
        pageSize,
      });
      setOrders(data.items || []);
      setTotalCount(data.totalCount || 0);
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load orders. Please try again.');
    } finally {
      setLoading(false);
    }
  }, [statusFilter, page, pageSize]);

  useEffect(() => {
    fetchOrders();
  }, [fetchOrders]);

  const handleFilterChange = (val) => {
    setStatusFilter(val);
    setPage(1);
  };

  const showingFrom = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const showingTo = Math.min(page * pageSize, totalCount);
  const totalPages = Math.ceil(totalCount / pageSize) || 1;

  // Metric summaries — only available from the current page of results unless we fetch all
  // We show total count from the server and status info from current page for UX context
  const heldCount = orders.filter((o) => o.status === 'Held').length;
  const confirmedCount = orders.filter((o) => o.status === 'Confirmed').length;
  const cancelledCount = orders.filter((o) => o.status === 'Cancelled').length;

  return (
    <DashboardLayout navItems={SUPPLIER_NAV_ITEMS} roleBadge="Supply Partner" profileRoute="/supplier/profile">

      {/* Page Header */}
      <div className="mb-8">
        <span className="text-label-uppercase text-primary tracking-widest block mb-2">
          ● ORDER MANAGEMENT
        </span>
        <h1 className="text-headline-lg font-heading font-bold text-text mb-1">
          Incoming Orders
        </h1>
        <p className="text-body-md text-text-secondary">
          Orders placed for your supply items. This is a read-only view — order management is handled by travelers and admins.
        </p>
      </div>

      {/* Read-only banner */}
      <div className="flex items-start gap-3 bg-surface-blue border border-border-blue rounded-xl p-4 mb-8">
        <span className="material-symbols-outlined text-primary text-lg mt-0.5">lock</span>
        <p className="text-body-sm text-text-secondary">
          <strong className="text-primary">Read-only view.</strong> Order status is managed by the traveler (hold/confirm/cancel) and the platform admin. Contact your admin if you have questions about a specific order.
        </p>
      </div>

      {/* Summary metric cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-[var(--space-lg)] mb-8">
        <SummaryMetricCard icon="shopping_bag" label="Total Orders" value={loading ? '—' : totalCount} />
        <SummaryMetricCard
          icon="pending"
          label="Held (page)"
          value={loading ? '—' : heldCount}
          badge={{ text: 'Held', colorClass: 'bg-status-warning/15 text-status-warning' }}
        />
        <SummaryMetricCard
          icon="check_circle"
          label="Confirmed (page)"
          value={loading ? '—' : confirmedCount}
          badge={{ text: 'Confirmed', colorClass: 'bg-status-success/15 text-status-success' }}
        />
        <SummaryMetricCard
          icon="cancel"
          label="Cancelled (page)"
          value={loading ? '—' : cancelledCount}
          badge={{ text: 'Cancelled', colorClass: 'bg-status-neutral/15 text-status-neutral' }}
        />
      </div>

      {/* Filter Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
        <div className="flex items-center gap-3">
          <select
            value={statusFilter}
            onChange={(e) => handleFilterChange(e.target.value)}
            className="border border-border-neutral rounded-md px-3 py-2 bg-white text-body-md focus:outline-none focus:border-primary"
            aria-label="Filter orders by status"
          >
            <option value="All">All Statuses</option>
            {ORDER_STATUSES.map((s) => (
              <option key={s} value={s}>{s}</option>
            ))}
          </select>
        </div>

        {!loading && totalCount > 0 && (
          <p className="text-body-sm text-text-secondary">
            Showing <span className="font-semibold text-text">{showingFrom}–{showingTo}</span> of{' '}
            <span className="font-semibold text-text">{totalCount}</span> orders
          </p>
        )}
      </div>

      {error && !loading && (
        <div className="mb-6">
          <ErrorBanner message={error} />
          <button onClick={fetchOrders} className="mt-2 text-primary text-body-sm font-bold hover:underline">
            ↺ Retry
          </button>
        </div>
      )}

      {/* Orders table */}
      {loading ? (
        <div className="space-y-3">
          {[1, 2, 3, 4, 5].map((i) => (
            <div key={i} className="h-14 bg-surface-neutral/50 animate-pulse rounded-xl border border-border-neutral" />
          ))}
        </div>
      ) : orders.length === 0 ? (
        <EmptyState
          icon="shopping_bag"
          title={statusFilter !== 'All' ? `No ${statusFilter} orders` : 'No orders yet'}
          description={
            statusFilter !== 'All'
              ? `There are no ${statusFilter.toLowerCase()} orders for your supplies.`
              : 'Orders will appear here once travelers place them for your supply items.'
          }
          action={statusFilter !== 'All' ? (
            <button
              onClick={() => handleFilterChange('All')}
              className="text-primary text-body-sm font-bold hover:underline"
            >
              Show all orders
            </button>
          ) : null}
        />
      ) : (
        <>
          <div className="bg-white border border-border-neutral rounded-xl overflow-x-auto">
            <table className="w-full text-left" aria-label="Incoming supply orders">
              <thead>
                <tr className="border-b border-border-neutral bg-surface-neutral/50">
                  <th className="px-6 py-3 text-label-uppercase text-text-secondary tracking-widest">Order ID</th>
                  <th className="px-6 py-3 text-label-uppercase text-text-secondary tracking-widest">Supply Item</th>
                  <th className="px-6 py-3 text-label-uppercase text-text-secondary tracking-widest">Qty</th>
                  <th className="px-6 py-3 text-label-uppercase text-text-secondary tracking-widest">Unit Price (LKR)</th>
                  <th className="px-6 py-3 text-label-uppercase text-text-secondary tracking-widest">Total (LKR)</th>
                  <th className="px-6 py-3 text-label-uppercase text-text-secondary tracking-widest">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border-neutral">
                {orders.map((order) => (
                  <OrderRow key={order.id} order={order} />
                ))}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between mt-4 p-[var(--space-md)] bg-white border border-border-neutral rounded-xl">
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
    </DashboardLayout>
  );
}

// ── Order Row ─────────────────────────────────────────────────────────────────

function OrderRow({ order }) {
  const statusColor = {
    Held: 'bg-status-warning/15 text-status-warning',
    Confirmed: 'bg-status-success/15 text-status-success',
    Cancelled: 'bg-status-neutral/15 text-status-neutral',
  }[order.status] || 'bg-surface-neutral text-text-secondary';

  return (
    <tr className="hover:bg-surface-neutral/30 transition-colors">
      <td className="px-6 py-4">
        <span className="text-body-sm font-mono text-text-secondary">#{order.id}</span>
      </td>
      <td className="px-6 py-4">
        <span className="text-body-sm font-semibold text-text">{order.supplyName}</span>
      </td>
      <td className="px-6 py-4">
        <span className="text-body-sm text-text">{order.quantity}</span>
      </td>
      <td className="px-6 py-4">
        <span className="text-body-sm text-text">
          {Number(order.priceAtOrderTime).toLocaleString('en-LK', { minimumFractionDigits: 2 })}
        </span>
      </td>
      <td className="px-6 py-4">
        <span className="text-body-sm font-bold text-text font-heading">
          {Number(order.totalPrice).toLocaleString('en-LK', { minimumFractionDigits: 2 })}
        </span>
      </td>
      <td className="px-6 py-4">
        <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-label-badge font-semibold ${statusColor}`}>
          {order.status}
        </span>
      </td>
    </tr>
  );
}
