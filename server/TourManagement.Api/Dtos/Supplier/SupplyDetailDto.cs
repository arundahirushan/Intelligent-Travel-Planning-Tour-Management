using TourManagement.Api.Models;

namespace TourManagement.Api.Dtos.Supplier;

// Full detail view of a supply item including removal reason and notes when present.
public class SupplyDetailDto
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PricePerUnit { get; set; }
    public int StockQuantity { get; set; }
    public SupplyStatus Status { get; set; }
    public RemovalReason? RemovalReason { get; set; }
    public string? RemovalNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
