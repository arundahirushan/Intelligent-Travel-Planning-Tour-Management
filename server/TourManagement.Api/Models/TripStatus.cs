namespace TourManagement.Api.Models;

// The possible statuses a Trip can be in.
// Stored as a string in the database so it's easy to read during demos/debugging.
public enum TripStatus
{
    Draft,           // Just created — traveler is still editing it.
    Planned,         // Traveler marked it as planned, not confirmed yet.
    PendingApproval, // Submitted for some form of review (future use).
    Confirmed,       // Confirmed by the system / admin.
    Completed,       // The trip has been taken.
    Cancelled        // Cancelled by traveler or admin force-cancel.
}
