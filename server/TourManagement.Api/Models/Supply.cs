namespace TourManagement.Api.Models;

// Represents a supply / inventory item offered by a registered Supplier.
// Prices are in LKR (Sri Lanka only).
public class Supply
{
    public int Id { get; set; }

    // The Supplier user who owns this supply.
    public int SupplierId { get; set; }

    public string Name { get; set; } = string.Empty;

    // Free text category (e.g. "Camping Gear", "Safety Equipment").
    public string Category { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    // Price per unit in LKR. Stored with 2 decimal places.
    public decimal PricePerUnit { get; set; }

    public int StockQuantity { get; set; }

    // Stored as a string in the DB (Active, Inactive, Removed).
    public SupplyStatus Status { get; set; } = SupplyStatus.Active;

    // Only set when Status is Removed by an Admin.
    public RemovalReason? RemovalReason { get; set; }

    // Optional free-text note added by an Admin upon removal.
    public string? RemovalNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User Supplier { get; set; } = null!;
}
