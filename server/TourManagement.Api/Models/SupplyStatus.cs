namespace TourManagement.Api.Models;

// Represents the lifecycle status of a supply item.
// Stored as a string in the database.
public enum SupplyStatus
{
    Active,
    Inactive,
    Removed
}
