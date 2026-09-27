namespace TourManagement.Api.Models;

// Lifecycle states for a TripCheckout (hold group record).
// Stored as a string in the DB for readability.
public enum CheckoutStatus
{
    Active,    // hold is live; linked bookings block availability
    Expired,   // 12-hour window passed without payment; bookings set to Cancelled
    Cancelled, // traveler cancelled the hold before paying
    Paid       // reserved for the future PayHere integration — do NOT write this value yet
}
