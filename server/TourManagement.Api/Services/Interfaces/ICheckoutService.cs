using TourManagement.Api.Common;
using TourManagement.Api.Dtos.Checkout;
using TourManagement.Api.Models;

namespace TourManagement.Api.Services.Interfaces;

public interface ICheckoutService
{
    // Place a 12-hour hold on a hotel room, a vehicle, or both.
    // Creates all booking rows and the TripCheckout record atomically.
    // Returns the existing checkout if an identical active hold already exists
    // (idempotency for network retries).
    Task<CheckoutResponseDto> PlaceHoldAsync(CreateCheckoutDto dto, int travelerId);

    // Called by the future Admin approval workflow to place holds for an AI proposal.
    // Validates that the proposed hotels match the trip's destinations, do not overlap, etc.
    Task<CheckoutResponseDto> PlaceApprovedProposalHoldAsync(CreateCheckoutDto dto, int travelerId);

    Task ValidateAgenticProposalAsync(CreateCheckoutDto dto, Trip trip);

    // Get a single checkout by ID. Throws ForbiddenException if not the owner.
    Task<CheckoutResponseDto> GetByIdAsync(int checkoutId, int travelerId);

    // Get all checkouts for the requesting traveler, newest first.
    Task<PagedResult<CheckoutResponseDto>> GetMyCheckoutsAsync(int travelerId, int? tripId, int page, int pageSize);

    // Cancel an active checkout. Sets checkout to Cancelled and linked bookings to Cancelled.
    // Allowed while Status == Active (whether or not the hold has expired).
    Task CancelAsync(int checkoutId, int travelerId);

    // ── PayHere integration point ──────────────────────────────────────────────
    // Generates the parameters for the PayHere form including the secure hash.
    Task<PayHereInitiateResponseDto> InitiatePaymentAsync(int checkoutId, int travelerId);

    // Called ONLY by the future PayHere callback handler after verifying payment.
    // Transitions:  Checkout → Paid,  HotelBooking → Confirmed,  VehicleBooking → Confirmed.
    // Throws ValidationException if the checkout is expired or not Active.
    // Do NOT wire this method to any UI button or public traveler endpoint in this task.
    // ──────────────────────────────────────────────────────────────────────────
    Task ConfirmAsync(int checkoutId);
}
