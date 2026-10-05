import React, { useState, useEffect } from 'react';
import Button from '../../../components/Button';
import LoadingSpinner from '../../../components/LoadingSpinner';
import ErrorBanner from '../../../components/ErrorBanner';
import { placeHold, initiatePayment, getMyHotelBookings, getMyVehicleBookings, getMySupplyOrders } from '../../../services/travelerApi';

function formatLKR(amount) {
  return `LKR ${Number(amount).toLocaleString('en-LK')}`;
}

export default function CheckoutCartWidget({ 
  tripId, 
  activeCheckout,
  onCheckoutCreated
}) {
  const [checkoutLoading, setCheckoutLoading] = useState(false);
  const [checkoutError, setCheckoutError] = useState(null);
  
  const [holds, setHolds] = useState({ hotels: [], vehicles: [], supplies: [] });
  const [loadingHolds, setLoadingHolds] = useState(true);

  useEffect(() => {
    async function loadHolds() {
      if (activeCheckout) {
        setLoadingHolds(false);
        return; // If there's an active checkout, we don't necessarily need to fetch separate holds for manual booking, because they are already covered. But wait, AI checkouts might also be active.
      }

      setLoadingHolds(true);
      try {
        const [hResp, vResp, sResp] = await Promise.all([
          getMyHotelBookings(tripId, 'Held'),
          getMyVehicleBookings(tripId, 'Held'),
          getMySupplyOrders(tripId, 'Held')
        ]);
        
        const now = new Date();
        const activeHotels = (hResp.items || []).filter(h => new Date(h.holdExpiresAt) > now);
        const activeVehicles = (vResp.items || []).filter(v => new Date(v.holdExpiresAt) > now);
        const activeSupplies = (sResp.items || []).filter(s => new Date(s.holdExpiresAt) > now);
        
        setHolds({ hotels: activeHotels, vehicles: activeVehicles, supplies: activeSupplies });
      } catch (err) {
        console.error("Failed to load active holds:", err);
      } finally {
        setLoadingHolds(false);
      }
    }
    loadHolds();
    
    // Refresh timers every minute
    const interval = setInterval(() => {
      setHolds(prev => {
        const now = new Date();
        return {
          hotels: prev.hotels.filter(h => new Date(h.holdExpiresAt) > now),
          vehicles: prev.vehicles.filter(v => new Date(v.holdExpiresAt) > now),
          supplies: prev.supplies.filter(s => new Date(s.holdExpiresAt) > now)
        };
      });
    }, 60000);
    
    return () => clearInterval(interval);
  }, [tripId, activeCheckout]);

  if (activeCheckout?._error) {
    return (
      <div className="bg-white border border-border-neutral rounded-xl p-6 shadow-soft">
        <ErrorBanner message={activeCheckout.message} />
      </div>
    );
  }

  const isSavedCheckout = !!activeCheckout;
  
  const hotelCount = isSavedCheckout ? (activeCheckout.hotels?.length || 0) : holds.hotels.length;
  const hasVehicle = isSavedCheckout ? !!activeCheckout.vehicleItem : holds.vehicles.length > 0;
  const supplyCount = isSavedCheckout ? (activeCheckout.supplies?.length || 0) : holds.supplies.length;
  
  const hasItems = hotelCount > 0 || hasVehicle || supplyCount > 0;

  if (!isSavedCheckout && !hasItems && !loadingHolds) {
    return (
      <div className="bg-white border border-border-neutral rounded-xl p-8 shadow-soft text-center">
        <span className="material-symbols-outlined text-4xl text-text-secondary mb-3">shopping_cart</span>
        <h3 className="font-heading font-bold text-text mb-2">No active bookings to pay for</h3>
        <p className="text-body-sm text-text-secondary max-w-md mx-auto">
          You haven't reserved any items, or your previous holds have expired. Add items from the Accommodation, Transport, or Supplies tabs first.
        </p>
      </div>
    );
  }

  let itemsTotal = 0;
  let websiteFee = 1000;
  let statusText = 'Ready to Pay';
  let statusBadge = null;
  let isPaymentAllowed = false;

  if (isSavedCheckout) {
    itemsTotal = activeCheckout.totalPrice;
    websiteFee = activeCheckout.websiteFee;
    isPaymentAllowed = activeCheckout.status === 'Active';
    
    if (activeCheckout.status === 'Active') {
      statusBadge = <span className="px-2 py-0.5 rounded text-xs font-bold bg-status-success/20 text-status-success">ACTIVE HOLD</span>;
      statusText = 'Payment Pending';
    } else if (activeCheckout.status === 'Paid') {
      statusBadge = <span className="px-2 py-0.5 rounded text-xs font-bold bg-primary/20 text-primary">PAID & CONFIRMED</span>;
      statusText = 'Payment Completed';
    } else if (activeCheckout.status === 'Expired') {
      statusBadge = <span className="px-2 py-0.5 rounded text-xs font-bold bg-status-danger/20 text-status-danger">EXPIRED</span>;
      statusText = 'Hold Expired';
    } else if (activeCheckout.status === 'Cancelled') {
      statusBadge = <span className="px-2 py-0.5 rounded text-xs font-bold bg-status-neutral/20 text-status-neutral">CANCELLED</span>;
      statusText = 'Checkout Cancelled';
    }
  } else {
    itemsTotal = holds.hotels.reduce((sum, h) => sum + h.priceSnapshot, 0) +
                 holds.vehicles.reduce((sum, v) => sum + v.priceSnapshot, 0) +
                 holds.supplies.reduce((sum, s) => sum + (s.priceAtOrderTime * s.quantity), 0);
    isPaymentAllowed = true;
    statusBadge = <span className="px-2 py-0.5 rounded text-xs font-bold bg-status-warning/20 text-status-warning">PENDING PAYMENT</span>;
  }

  const grandTotal = itemsTotal + websiteFee;

  const handlePay = async () => {
    setCheckoutLoading(true);
    setCheckoutError(null);
    try {
      let checkoutIdToPay = activeCheckout?.id;
      
      // If we don't have a saved checkout session yet, we create one out of the active holds
      if (!isSavedCheckout) {
        const checkoutResp = await placeHold({ TripId: tripId });
        checkoutIdToPay = checkoutResp.id;
      }
      
      const paymentInfo = await initiatePayment(checkoutIdToPay);
      
      const form = document.createElement('form');
      form.method = 'POST';
      form.action = 'https://sandbox.payhere.lk/pay/checkout';
      
      const appendInput = (name, value) => {
        const input = document.createElement('input');
        input.type = 'hidden';
        input.name = name;
        input.value = value;
        form.appendChild(input);
      };

      appendInput('merchant_id', paymentInfo.merchantId);
      appendInput('return_url', paymentInfo.returnUrl);
      appendInput('cancel_url', paymentInfo.cancelUrl);
      appendInput('notify_url', paymentInfo.notifyUrl);
      appendInput('order_id', paymentInfo.orderId);
      appendInput('items', paymentInfo.items);
      appendInput('currency', paymentInfo.currency);
      appendInput('amount', paymentInfo.amount);
      appendInput('hash', paymentInfo.hash);
      
      appendInput('first_name', 'Traveler');
      appendInput('last_name', 'Name');
      appendInput('email', 'traveler@example.com');
      appendInput('phone', '0771234567');
      appendInput('address', 'No 1, Galle Road');
      appendInput('city', 'Colombo');
      appendInput('country', 'Sri Lanka');

      document.body.appendChild(form);
      form.submit();
    } catch (err) {
      console.error(err);
      setCheckoutError(err.response?.data?.message || 'Payment initiation failed. Please try again.');
      setCheckoutLoading(false);
    }
  };

  return (
    <div className="bg-white border border-border-neutral rounded-xl p-6 shadow-soft">
      {loadingHolds ? (
        <div className="flex justify-center p-4"><LoadingSpinner size="md" /></div>
      ) : (
        <>
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div>
              <div className="flex items-center gap-3 mb-1">
                <h3 className="font-heading font-bold text-text">{statusText}</h3>
                {statusBadge}
              </div>
              <p className="text-body-sm text-text-secondary">
                {hotelCount} Hotel{hotelCount !== 1 ? 's' : ''}, 
                {hasVehicle ? ' 1 Vehicle' : ' 0 Vehicles'}, 
                {supplyCount} Supply Order{supplyCount !== 1 ? 's' : ''}
              </p>
              
              {!isSavedCheckout && (
                <div className="mt-3 text-xs text-text-secondary space-y-1">
                   {holds.hotels.map(h => <p key={`h-${h.id}`}>Hotel {h.room?.hotel?.name || ''} - Expires: {new Date(h.holdExpiresAt).toLocaleString()}</p>)}
                   {holds.vehicles.map(v => <p key={`v-${v.id}`}>Vehicle - Expires: {new Date(v.holdExpiresAt).toLocaleString()}</p>)}
                   {holds.supplies.map(s => <p key={`s-${s.id}`}>Supply {s.supply?.name || ''} - Expires: {new Date(s.holdExpiresAt).toLocaleString()}</p>)}
                </div>
              )}
              {isSavedCheckout && activeCheckout.holdExpiresAt && (
                <p className="text-xs text-text-secondary mt-1">Expires: {new Date(activeCheckout.holdExpiresAt).toLocaleString()}</p>
              )}
            </div>
            
            <div className="flex items-center gap-6">
              <div className="text-right">
                <p className="text-body-sm text-text-secondary">Provider: {formatLKR(itemsTotal)} + Fee: {formatLKR(websiteFee)}</p>
                <p className="font-heading font-bold text-primary text-headline-sm">{formatLKR(grandTotal)} Total</p>
              </div>
              
              <div className="flex items-center gap-3">
                <Button onClick={handlePay} disabled={!isPaymentAllowed || checkoutLoading} className={isPaymentAllowed ? '' : 'opacity-50 cursor-not-allowed'}>
                  {checkoutLoading ? <LoadingSpinner size="sm" /> : 'Pay LKR 1,000'}
                </Button>
              </div>
            </div>
          </div>
          {checkoutError && (
            <div className="mt-4">
              <ErrorBanner message={checkoutError} />
            </div>
          )}
        </>
      )}
    </div>
  );
}
