import React, { useState, useEffect } from 'react';
import Modal from '../../../components/Modal';
import Button from '../../../components/Button';
import ErrorBanner from '../../../components/ErrorBanner';
import LoadingSpinner from '../../../components/LoadingSpinner';
import { browseSupplies, createSupplyOrder } from '../../../services/travelerApi';

function formatLKR(amount) {
  return `LKR ${Number(amount).toLocaleString('en-LK')}`;
}

// OrderSupplyModal — 2-step flow:
//   1. Browse active supplies with search/category filter
//   2. Select quantity and confirm the order
//
// Props:
//   isOpen    — controls visibility
//   onClose   — close handler
//   onSuccess — called after successful order
//   trip      — TripDetailDto (provides TripId)
export default function OrderSupplyModal({ isOpen, onClose, onSuccess, trip }) {
  const [step, setStep] = useState('browse');

  // Browse state
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState('');
  const [supplies, setSupplies] = useState([]);
  const [browseLoading, setBrowseLoading] = useState(false);
  const [browseError, setBrowseError] = useState(null);

  // Selected supply & order form
  const [selectedSupply, setSelectedSupply] = useState(null);
  const [quantity, setQuantity] = useState('1');
  const [orderLoading, setOrderLoading] = useState(false);
  const [orderError, setOrderError] = useState(null);

  useEffect(() => {
    if (isOpen) {
      setStep('browse');
      setSearch('');
      setCategory('');
      setSupplies([]);
      setBrowseError(null);
      setSelectedSupply(null);
      setQuantity('1');
      setOrderError(null);
      loadSupplies();
    }
  }, [isOpen]);

  const loadSupplies = async (searchVal = '', catVal = '') => {
    setBrowseLoading(true);
    setBrowseError(null);
    try {
      const data = await browseSupplies({ search: searchVal, category: catVal, pageSize: 50 });
      setSupplies(data.items || []);
    } catch (err) {
      setBrowseError(err.response?.data?.message || 'Failed to load supplies.');
    } finally {
      setBrowseLoading(false);
    }
  };

  const handleSearch = (e) => {
    e.preventDefault();
    loadSupplies(search, category);
  };

  const handleSelectSupply = (supply) => {
    setSelectedSupply(supply);
    setQuantity('1');
    setOrderError(null);
    setStep('order');
  };

  const handleOrder = async (e) => {
    e.preventDefault();
    const qty = parseInt(quantity, 10);
    if (!qty || qty < 1) { setOrderError('Quantity must be at least 1.'); return; }
    if (qty > selectedSupply.stockQuantity) {
      setOrderError(`Only ${selectedSupply.stockQuantity} units in stock.`);
      return;
    }
    setOrderLoading(true);
    setOrderError(null);
    try {
      await createSupplyOrder({
        TripId: trip.id,
        SupplyId: selectedSupply.id,
        Quantity: qty,
      });
      onSuccess();
      onClose();
    } catch (err) {
      setOrderError(err.response?.data?.message || 'Order failed. Please try again.');
    } finally {
      setOrderLoading(false);
    }
  };

  const inputClass = 'bg-white border border-border-neutral rounded-md px-4 py-2.5 font-body text-text outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-all w-full';

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Order Supplies" size="lg">
      {/* ── Step 1: Browse ── */}
      {step === 'browse' && (
        <div className="space-y-4">
          <form onSubmit={handleSearch} className="flex gap-3">
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search supplies…"
              className={`${inputClass} flex-1`}
            />
            <input
              type="text"
              value={category}
              onChange={(e) => setCategory(e.target.value)}
              placeholder="Category (optional)"
              className="bg-white border border-border-neutral rounded-md px-4 py-2.5 font-body text-text outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-all w-40"
            />
            <Button type="submit" disabled={browseLoading}>
              <span className="material-symbols-outlined text-sm">search</span>
            </Button>
          </form>

          {browseError && <ErrorBanner message={browseError} />}

          {browseLoading ? (
            <div className="flex justify-center py-8"><LoadingSpinner size="lg" /></div>
          ) : supplies.length === 0 ? (
            <div className="text-center py-8">
              <span className="material-symbols-outlined text-5xl text-text-secondary/40 block mb-3">inventory_2</span>
              <p className="font-heading font-bold text-text mb-2">No supplies found</p>
              <p className="text-body-sm text-text-secondary">Try adjusting your search.</p>
            </div>
          ) : (
            <div className="space-y-3 max-h-96 overflow-y-auto pr-1">
              {supplies.map((s) => (
                <div key={s.id} className="border border-border-neutral rounded-lg p-4 bg-white">
                  <div className="flex justify-between items-start gap-3">
                    <div>
                      <p className="font-heading font-bold text-text">{s.name}</p>
                      <p className="text-body-sm text-text-secondary">{s.category} · by {s.supplierName}</p>
                      <p className="text-label-badge text-text-secondary mt-1">
                        {s.stockQuantity} in stock
                      </p>
                    </div>
                    <div className="text-right shrink-0">
                      <p className="font-heading font-bold text-primary text-headline-sm">{formatLKR(s.pricePerUnit)}</p>
                      <p className="text-label-badge text-text-secondary">/ unit</p>
                    </div>
                  </div>
                  <Button
                    onClick={() => handleSelectSupply(s)}
                    disabled={s.stockQuantity === 0}
                    className="w-full mt-3"
                  >
                    {s.stockQuantity === 0 ? 'Out of Stock' : 'Order This'}
                  </Button>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* ── Step 2: Order Form ── */}
      {step === 'order' && selectedSupply && (
        <form onSubmit={handleOrder} className="space-y-4">
          <button onClick={() => setStep('browse')} className="text-primary hover:text-primary-dark font-heading font-bold text-sm flex items-center gap-1">
            <span className="material-symbols-outlined text-sm">arrow_back</span> Back to Browse
          </button>

          {orderError && <ErrorBanner message={orderError} />}

          <div className="bg-surface-blue border border-border-blue rounded-lg px-4 py-3">
            <p className="font-heading font-bold text-text">{selectedSupply.name}</p>
            <p className="text-body-sm text-text-secondary">{selectedSupply.category} · by {selectedSupply.supplierName}</p>
            <p className="font-heading font-bold text-primary mt-2">{formatLKR(selectedSupply.pricePerUnit)} / unit</p>
            <p className="text-label-badge text-text-secondary">{selectedSupply.stockQuantity} in stock</p>
          </div>

          <div className="flex flex-col">
            <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary" htmlFor="supply-qty">
              Quantity (max {selectedSupply.stockQuantity})
            </label>
            <input
              id="supply-qty"
              type="number"
              value={quantity}
              onChange={(e) => setQuantity(e.target.value)}
              min="1"
              max={selectedSupply.stockQuantity}
              step="1"
              className="bg-white border border-border-neutral rounded-md px-4 py-2.5 font-body text-text outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-all"
            />
          </div>

          {/* Total preview */}
          {parseInt(quantity, 10) > 0 && (
            <div className="flex justify-between items-center bg-surface-neutral rounded-lg px-4 py-3">
              <span className="font-heading font-bold text-text-secondary">Estimated Total</span>
              <span className="font-heading font-bold text-primary text-headline-sm">
                {formatLKR(selectedSupply.pricePerUnit * parseInt(quantity, 10))}
              </span>
            </div>
          )}

          <div className="flex justify-end gap-3 border-t border-border-neutral pt-4">
            <Button type="button" variant="secondary" onClick={onClose} disabled={orderLoading}>Cancel</Button>
            <Button type="submit" disabled={orderLoading}>
              {orderLoading ? <LoadingSpinner size="sm" /> : 'Place Order'}
            </Button>
          </div>
        </form>
      )}
    </Modal>
  );
}
