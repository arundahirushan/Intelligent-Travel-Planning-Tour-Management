namespace TourManagement.Api.Models;

// Lifecycle states for a VehicleBooking.
// Stored as a string in the DB for readability.
public enum VehicleBookingStatus
{
    Held,       // tentative reservation — vehicle is blocked for these dates
    Confirmed,  // finalized booking
    Cancelled   // no longer active; stays in the DB for history
}

