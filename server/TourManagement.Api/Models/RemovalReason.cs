namespace TourManagement.Api.Models;

// Specific reasons an Admin can cite when removing a supply item from the catalog.
// Only set when Supply.Status is Removed.
public enum RemovalReason
{
    PriceIssue,
    NotSuitable,
    Other
}
