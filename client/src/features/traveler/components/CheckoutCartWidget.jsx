import React, { useState } from 'react';
import Button from '../../../components/Button';
import LoadingSpinner from '../../../components/LoadingSpinner';
import ErrorBanner from '../../../components/ErrorBanner';
import { placeHold, initiatePayment } from '../../../services/travelerApi';

function formatLKR(amount) {
  return `LKR ${Number(amount).toLocaleString('en-LK')}`;
}

export default function CheckoutCartWidget({ tripId, cartHotels, cartVehicle, cartSupplies, onClearCart }) {
  const [checkoutLoading, setCheckoutLoading] = useState(false);
  const [checkoutError, setCheckoutError] = useState(null);
  
  if (cartHotels.length === 0 && !cartVehicle && cartSupplies.length === 0) {
    return null;
  }

  const itemsTotal = 
    cartHotels.reduce((sum, h) => sum + h._price, 0) +
    (cartVehicle ? cartVehicle._price : 0) +
    cartSupplies.reduce((sum, s) => sum + s._price, 0);

  const websiteFee = 1000;
  const grandTotal = itemsTotal + websiteFee;

  const handleCheckout = async () => {
    setCheckoutLoading(true);
    setCheckoutError(null);
    try {
      // 1. Place Hold
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
      const checkoutId = checkoutResp.id;

      // 2. Initiate Payment
      const paymentInfo = await initiatePayment(checkoutId);
      
      // 3. Auto-submit form to PayHere Sandbox
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
      
      // We also need some required PayHere fields that we might not have in DTO, let's just supply defaults:
      appendInput('first_name', 'Traveler');
      appendInput('last_name', 'Name');
      appendInput('email', 'traveler@example.com');
      appendInput('phone', '0771234567');
      appendInput('address', 'No 1, Galle Road');
      appendInput('city', 'Colombo');
      appendInput('country', 'Sri Lanka');

      document.body.appendChild(form);
      form.submit();
      
      // Form submitted; clear cart (although page will redirect anyway)
      onClearCart();
    } catch (err) {
      console.error(err);
      setCheckoutError(err.response?.data?.message || 'Checkout failed. Please try again.');
      setCheckoutLoading(false);
    }
  };

  return (
    <div className="fixed bottom-0 left-0 right-0 bg-white border-t border-border-neutral shadow-[0_-4px_6px_-1px_rgba(0,0,0,0.1)] p-4 z-50">
      <div className="max-w-7xl mx-auto flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h3 className="font-heading font-bold text-text mb-1">Ready to Checkout</h3>
          <p className="text-body-sm text-text-secondary">
            {cartHotels.length} Hotel{cartHotels.length !== 1 ? 's' : ''}, 
            {cartVehicle ? ' 1 Vehicle' : ' 0 Vehicles'}, 
            {cartSupplies.length} Supply Order{cartSupplies.length !== 1 ? 's' : ''}
          </p>
        </div>
        
        <div className="flex items-center gap-6">
          <div className="text-right">
            <p className="text-body-sm text-text-secondary">Items: {formatLKR(itemsTotal)} + Fee: {formatLKR(websiteFee)}</p>
            <p className="font-heading font-bold text-primary text-headline-sm">{formatLKR(grandTotal)} Total</p>
          </div>
          
          <div className="flex items-center gap-3">
            <Button variant="secondary" onClick={onClearCart} disabled={checkoutLoading}>
              Clear
            </Button>
            <Button onClick={handleCheckout} disabled={checkoutLoading}>
              {checkoutLoading ? <LoadingSpinner size="sm" /> : 'Checkout & Pay'}
            </Button>
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
