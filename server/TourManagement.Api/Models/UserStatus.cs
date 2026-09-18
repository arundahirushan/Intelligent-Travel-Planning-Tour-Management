namespace TourManagement.Api.Models;

// The possible statuses a user account can be in.
// Stored as a string in the database (configured in AppDbContext) so it's
// readable when you look at the table directly — e.g. "Active" instead of "0".
public enum UserStatus
{
    Active,           // Can log in and use the system.
    PendingApproval,  // Provider accounts wait here until an Admin approves them.
    Rejected,         // Admin rejected the registration.
    Suspended         // Admin suspended an existing account.
}
