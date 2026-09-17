namespace TourManagement.Api.Models;

// Lifecycle states for a Hotel listing.
// Stored as a string in the DB (see AppDbContext) for readability.
public enum HotelStatus
{
    PendingApproval,  // just submitted, waiting for Admin review
    Active,           // approved and visible to travelers
    Suspended,        // disabled by Admin (policy violation or used as rejection)
    Inactive          // soft-deleted by the owner; hidden but history is preserved
}
