namespace TourManagement.Api.Models;

// Whether a room type is currently available for booking.
// Stored as a string in the DB for readability.
public enum RoomStatus
{
    Active,    // available for new bookings
    Inactive   // soft-deleted; hides the room but preserves existing booking history
}
