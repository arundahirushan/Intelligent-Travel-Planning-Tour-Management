namespace TourManagement.Api.Models;

// Lifecycle states for a hotel room Booking.
// Stored as a string in the DB for readability.
public enum BookingStatus
{
    Held,       // tentative reservation — counts against availability but isn't finalized
    Confirmed,  // finalized booking
    Cancelled   // no longer active; stays in the DB for history
}
