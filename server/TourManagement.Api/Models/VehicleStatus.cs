namespace TourManagement.Api.Models;

// Lifecycle states for a Vehicle listing.
// Stored as a string in the DB (see AppDbContext) for readability.
public enum VehicleStatus
{
    PendingApproval,  // just submitted, waiting for Admin review
    Active,           // approved and visible to travelers
    Rejected,         // Admin reviewed and denied a PendingApproval listing
    Suspended,        // Admin disabled an already-Active listing (policy violation etc.)
    Inactive          // soft-deleted by the provider; hidden but history is preserved
}

