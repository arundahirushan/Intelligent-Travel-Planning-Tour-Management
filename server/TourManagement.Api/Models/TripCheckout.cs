namespace TourManagement.Api.Models;

// A checkout represents one "hold session" — a traveler selects a hotel room,
// a vehicle, or both, and the system places a 12-hour hold on all selected items
// atomically.  Exactly one or both items must be present; the service enforces this.
//
// This is the parent record for the hold-to-payment flow.  When PayHere integration
// is added later, a payment confirmation will transition this to Paid and the linked
// bookings to Confirmed.
public class TripCheckout
{
    public int Id { get; set; }

    // The trip this checkout belongs to.
    public int TripId { get; set; }

    // Denormalised from the trip so the cleanup service and Admin queries
    // can filter by traveler without an extra join.
    public int TravelerId { get; set; }

    // Exactly one or both of these must be set (validated in CheckoutService).
    public int? HotelBookingId { get; set; }
    public int? VehicleBookingId { get; set; }
    
    // Unique identifier if this checkout was created from an AI proposal.
    public string? ProposalId { get; set; }

    // Prices captured at hold time.  Later price changes on the room/vehicle
    // do NOT affect these snapshots — this is the amount the traveler agreed to.
    public decimal? HotelPriceSnapshot { get; set; }
    public decimal? VehiclePriceSnapshot { get; set; }

    // Sum of the snapshots above.  Stored so it never needs to be recomputed.
    public decimal TotalPrice { get; set; }

    // Website booking-confirmation fee charged directly to the traveler via PayHere.
    // LKR 1000 flat fee per trip.
    public decimal WebsiteFee { get; set; } = 1000.00m;

    // Active = items are held.  Expired/Cancelled = items freed.  Paid = confirmed (future).
    // Stored as a string in the DB (see AppDbContext).
    public CheckoutStatus Status { get; set; } = CheckoutStatus.Active;

    // UTC timestamp at which this hold expires.  Set to CreatedAt + 12 hours.
    // After this time the hold no longer blocks availability, even if Status
    // is still Active (the cleanup job will eventually update Status to Expired).
    public DateTime HoldExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties.
    public Trip Trip { get; set; } = null!;
    public User Traveler { get; set; } = null!;
    // Kept for backward compatibility (maps to the first hotel booking if any).
    public HotelBooking? HotelBooking => HotelBookings.FirstOrDefault();
    public List<HotelBooking> HotelBookings { get; set; } = new();
    public VehicleBooking? VehicleBooking { get; set; }

    public List<SupplyOrder> SupplyOrders { get; set; } = new();
    
    // Payments made or attempted for this checkout
    public List<PaymentAttempt> PaymentAttempts { get; set; } = new();
}
