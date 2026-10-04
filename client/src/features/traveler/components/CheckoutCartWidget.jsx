import React, { useState, useEffect } from 'react';
import Button from '../../../components/Button';
import LoadingSpinner from '../../../components/LoadingSpinner';
import ErrorBanner from '../../../components/ErrorBanner';
import { placeHold, initiatePayment } from '../../../services/travelerApi';

function formatLKR(amount) {
  return `LKR ${Number(amount).toLocaleString('en-LK')}`;
}

export default function CheckoutCartWidget({ 
  tripId, 
  activeCheckout,
  cartHotels, 
  cartVehicle, 
  cartSupplies, 
  onClearCart,
  onCheckoutCreated
}) {
  const [checkoutLoading, setCheckoutLoading] = useState(false);
  const [checkoutError, setCheckoutError] = useState(null);
  
  if (activeCheckout?._error) {
    return (
      <div className="fixed bottom-0 left-0 right-0 bg-white border-t border-border-neutral shadow-[0_-4px_6px_-1px_rgba(0,0,0,0.1)] p-4 z-50">
        <div className="max-w-7xl mx-auto">
          <ErrorBanner message={activeCheckout.message} />
        </div>
      </div>
    );
  }

  const isSavedCheckout = !!activeCheckout;
  const isLocalCart = !isSavedCheckout && (cartHotels.length > 0 || cartVehicle || cartSupplies.length > 0);

  if (!isSavedCheckout && !isLocalCart) {
    return null;
  }

  // Determine items and totals depending on mode
  let hotelCount = 0;
  let hasVehicle = false;
  let supplyCount = 0;
  let itemsTotal = 0;
  let websiteFee = 1000;
  
  let statusText = 'Ready to Reserve';
  let statusBadge = null;
  let expiryText = null;
  let isPaymentAllowed = false;
  let isPlaceHoldAllowed = false;

  if (isSavedCheckout) {
    hotelCount = activeCheckout.hotels?.length || 0;
    hasVehicle = !!activeCheckout.vehicleItem;
    supplyCount = activeCheckout.supplies?.length || 0;
    itemsTotal = activeCheckout.totalPrice;
    websiteFee = activeCheckout.websiteFee;
    
    isPaymentAllowed = activeCheckout.status === 'Active';
    
    // Status badges
    if (activeCheckout.status === 'Active') {
      statusBadge = <span className="px-2 py-0.5 rounded text-xs font-bold bg-status-success/20 text-status-success">ACTIVE HOLD</span>;
      const expiry = new Date(activeCheckout.holdExpiresAt);
      expiryText = `Expires: ${expiry.toLocaleString()}`;
      statusText = 'Hold Placed';
    } else if (activeCheckout.status === 'Paid') {
      statusBadge = <span className="px-2 py-0.5 rounded text-xs font-bold bg-primary/20 text-primary">PAID & CONFIRMED</span>;
      statusText = 'Payment Completed';
    } else if (activeCheckout.status === 'Expired') {
      statusBadge = <span className="px-2 py-0.5 rounded text-xs font-bold bg-status-danger/20 text-status-danger">EXPIRED</span>;
      statusText = 'Hold Expired';
    } else if (activeCheckout.status === 'Cancelled') {
      statusBadge = <span className="px-2 py-0.5 rounded text-xs font-bold bg-status-neutral/20 text-status-neutral">CANCELLED</span>;
      statusText = 'Checkout Cancelled';
    } else {
      statusBadge = <span className="px-2 py-0.5 rounded text-xs font-bold bg-status-neutral/20 text-status-neutral">{activeCheckout.status}</span>;
      statusText = 'Saved Checkout';
    }
  } else {
    hotelCount = cartHotels.length;
    hasVehicle = !!cartVehicle;
    supplyCount = cartSupplies.length;
    itemsTotal = cartHotels.reduce((sum, h) => sum + h._price, 0) +
                 (cartVehicle ? cartVehicle._price : 0) +
                 cartSupplies.reduce((sum, s) => sum + s._price, 0);
    isPlaceHoldAllowed = true;
  }

  const grandTotal = itemsTotal + websiteFee;

  const handlePlaceHold = async () => {
    setCheckoutLoading(true);
    setCheckoutError(null);
    try {
      const holdBody = {
        TripId: tripId,
        Hotels: cartHotels.map(h => ({
          RoomId: h.RoomId,
          CheckInDate: h.CheckInDate,
          CheckOutDate: h.CheckOutDate,
          NumberOfRooms: h.NumberOfRooms
        })),
        Vehicle: cartVehicle ? {
          VehicleId: cartVehicle.VehicleId,
          StartDate: cartVehicle.StartDate,
          EndDate: cartVehicle.EndDate,
          PickupLatitude: cartVehicle.PickupLatitude,
          PickupLongitude: cartVehicle.PickupLongitude,
          PickupNote: cartVehicle.PickupNote
        } : null,
        Supplies: cartSupplies.map(s => ({
          SupplyId: s.SupplyId,
          Quantity: s.Quantity
        }))
      };

      const checkoutResp = await placeHold(holdBody);
      onClearCart();
      if (onCheckoutCreated) {
        onCheckoutCreated();
      }
    } catch (err) {
      console.error(err);
      setCheckoutError(err.response?.data?.message || 'Failed to place hold. Please try again.');
    } finally {
      setCheckoutLoading(false);
    }
  };

  const handlePay = async () => {
    setCheckoutLoading(true);
    setCheckoutError(null);
    try {
      const paymentInfo = await initiatePayment(activeCheckout.id);
      
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
    <div className="fixed bottom-0 left-0 right-0 bg-white border-t border-border-neutral shadow-[0_-4px_6px_-1px_rgba(0,0,0,0.1)] p-4 z-50">
      <div className="max-w-7xl mx-auto flex flex-col sm:flex-row sm:items-center justify-between gap-4">
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
          {expiryText && <p className="text-xs text-text-secondary mt-1">{expiryText}</p>}
        </div>
        
        <div className="flex items-center gap-6">
          <div className="text-right">
            <p className="text-body-sm text-text-secondary">Provider: {formatLKR(itemsTotal)} + Fee: {formatLKR(websiteFee)}</p>
            <p className="font-heading font-bold text-primary text-headline-sm">{formatLKR(grandTotal)} Total</p>
          </div>
          
          <div className="flex items-center gap-3">
            {!isSavedCheckout && (
              <Button variant="secondary" onClick={onClearCart} disabled={checkoutLoading}>
                Clear
              </Button>
            )}
            
            {isPlaceHoldAllowed && (
              <Button onClick={handlePlaceHold} disabled={checkoutLoading}>
                {checkoutLoading ? <LoadingSpinner size="sm" /> : 'Reserve All Bookings'}
              </Button>
            )}

            {isSavedCheckout && (
              <Button onClick={handlePay} disabled={!isPaymentAllowed || checkoutLoading} className={isPaymentAllowed ? '' : 'opacity-50 cursor-not-allowed'}>
                {checkoutLoading ? <LoadingSpinner size="sm" /> : 'Pay LKR 1,000'}
              </Button>
            )}
          </div>
        </div>
      </div>
      {checkoutError && (
        <div className="max-w-7xl mx-auto mt-3">
          <ErrorBanner message={checkoutError} />
        </div>
      )}
    </div>
  );
}
