import React, { useState, useEffect, useCallback } from 'react';
import Button from '../../../components/Button';
import StatusBadge from '../../../components/StatusBadge';
import EmptyState from '../../../components/EmptyState';
import ErrorBanner from '../../../components/ErrorBanner';
import LoadingSpinner from '../../../components/LoadingSpinner';
import ConfirmDialog from '../../../components/ConfirmDialog';
import OrderSupplyModal from './OrderSupplyModal';
import { getMySupplyOrders, cancelSupplyOrder, deleteSupplyOrder } from '../../../services/travelerApi';

function formatLKR(amount) {
  return `LKR ${Number(amount).toLocaleString('en-LK')}`;
}

// SuppliesTab — supply orders for the traveler.
// NOTE: SupplyOrderSummaryDto does NOT include SupplyId, which UpdateSupplyOrderDto requires.
// In-place edit is therefore not supported. To change quantity, cancel and reorder.
//
// Props:
//   trip — TripDetailDto (or null for global My Bookings page)
export default function SuppliesTab({ trip }) {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [statusFilter, setStatusFilter] = useState('All');

  const [orderModalOpen, setOrderModalOpen] = useState(false);

  const [cancelConfirmOpen, setCancelConfirmOpen] = useState(false);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [actionTarget, setActionTarget] = useState(null);
  const [actionError, setActionError] = useState(null);

  const fetchOrders = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getMySupplyOrders({ status: statusFilter, pageSize: 100 });
      setOrders(data.items || []);
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to load supply orders.');
    } finally {
      setLoading(false);
    }
  }, [statusFilter]);

  useEffect(() => { fetchOrders(); }, [fetchOrders]);

  const openCancel = (order) => { setActionTarget(order); setActionError(null); setCancelConfirmOpen(true); };
  const openDelete = (order) => { setActionTarget(order); setActionError(null); setDeleteConfirmOpen(true); };

  const handleCancel = async () => {
    try {
      setActionError(null);
      await cancelSupplyOrder(actionTarget.id);
      fetchOrders();
    } catch (err) {
      setActionError(err.response?.data?.message || 'Cancel failed.');
    }
  };

  const handleDelete = async () => {
    try {
      setActionError(null);
      await deleteSupplyOrder(actionTarget.id);
      fetchOrders();
    } catch (err) {
      setActionError(err.response?.data?.message || 'Delete failed.');
    }
  };

  const isHeld = (o) => o.status === 'Held';
  const heldOrConfirmed = (o) => o.status === 'Held' || o.status === 'Confirmed';

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-headline-sm font-heading font-bold text-text mb-1">Supplies</h2>
          <p className="text-body-sm text-text-secondary">Your supply orders (all trips)</p>
        </div>
        <div className="flex gap-3 flex-wrap items-center">
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            className="border border-border-neutral rounded-md px-3 py-2 bg-white text-body-md focus:outline-none focus:border-primary text-sm"
          >
            <option value="All">All Statuses</option>
            <option value="Held">Held</option>
            <option value="Confirmed">Confirmed</option>
            <option value="Cancelled">Cancelled</option>
          </select>
          {trip && (
            <Button onClick={() => setOrderModalOpen(true)}>
              <span className="flex items-center gap-2">
                <span className="material-symbols-outlined text-base">add</span>
                Order Supplies
              </span>
            </Button>
          )}
        </div>
      </div>

      {error && <ErrorBanner message={error} />}
      {actionError && <ErrorBanner message={actionError} />}

      {loading ? (
        <div className="flex justify-center py-10"><LoadingSpinner size="lg" /></div>
      ) : orders.length === 0 ? (
        <EmptyState
          icon="inventory_2"
          title="No supply orders"
          description={statusFilter !== 'All' ? 'No orders match the current filter.' : (trip ? 'Order supplies from approved suppliers for this trip.' : 'You have no supply orders yet. Navigate to a specific trip to order supplies.')}
          action={(trip && statusFilter === 'All') && <Button onClick={() => setOrderModalOpen(true)}>Order Supplies</Button>}
        />
      ) : (
        <div className="space-y-4">
          {orders.map((o) => (
            <div key={o.id} className="bg-white border border-border-neutral rounded-xl p-5 shadow-soft">
              <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-4">
                <div className="flex-1">
                  <div className="flex items-center gap-3 mb-2">
                    <p className="font-heading font-bold text-text">{o.supplyName}</p>
                    <StatusBadge status={o.status} />
                  </div>
                  <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
                    <div>
                      <p className="text-label-uppercase text-text-secondary tracking-widest">Quantity</p>
                      <p className="text-sm font-heading font-bold text-text">{o.quantity}</p>
                    </div>
                    <div>
                      <p className="text-label-uppercase text-text-secondary tracking-widest">Unit Price</p>
                      <p className="text-sm font-heading font-bold text-text">{formatLKR(o.priceAtOrderTime)}</p>
                    </div>
                    <div>
                      <p className="text-label-uppercase text-text-secondary tracking-widest">Total</p>
                      <p className="text-sm font-heading font-bold text-primary">{formatLKR(o.totalPrice)}</p>
                    </div>
                  </div>
                </div>

                {/* Actions — edit omitted because SupplyId is not in SupplyOrderSummaryDto */}
                {isHeld(o) && (
                  <p className="mt-3 text-label-badge text-text-secondary italic">
                    To change quantity, cancel and place a new order.
                  </p>
                )}
                <div className="flex gap-2 shrink-0">
                  {heldOrConfirmed(o) && (
                    <button onClick={() => openCancel(o)} className="p-2 rounded-lg hover:bg-red-50 text-text-secondary hover:text-status-danger transition-colors" title="Cancel">
                      <span className="material-symbols-outlined text-sm">cancel</span>
                    </button>
                  )}
                  {isHeld(o) && (
                    <button onClick={() => openDelete(o)} className="p-2 rounded-lg hover:bg-red-50 text-text-secondary hover:text-status-danger transition-colors" title="Delete">
                      <span className="material-symbols-outlined text-sm">delete</span>
                    </button>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Modals */}
      <OrderSupplyModal
        isOpen={orderModalOpen}
        onClose={() => setOrderModalOpen(false)}
        onSuccess={fetchOrders}
        trip={trip}
      />
      <ConfirmDialog
        isOpen={cancelConfirmOpen}
        onClose={() => setCancelConfirmOpen(false)}
        onConfirm={handleCancel}
        title="Cancel Supply Order"
        message={`Cancel your order for "${actionTarget?.supplyName}"? Stock will be restored.`}
        confirmLabel="Cancel Order"
        isDanger
      />
      <ConfirmDialog
        isOpen={deleteConfirmOpen}
        onClose={() => setDeleteConfirmOpen(false)}
        onConfirm={handleDelete}
        title="Delete Supply Order"
        message={`Permanently delete your order for "${actionTarget?.supplyName}"? Stock will be restored.`}
        confirmLabel="Delete"
        isDanger
      />
    </div>
  );
}
