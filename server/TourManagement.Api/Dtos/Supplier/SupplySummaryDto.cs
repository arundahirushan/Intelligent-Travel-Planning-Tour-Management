using TourManagement.Api.Models;

namespace TourManagement.Api.Dtos.Supplier;

// Lightweight summary of a supply item for lists and browse results.
public class SupplySummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal PricePerUnit { get; set; }
    public int StockQuantity { get; set; }
    public SupplyStatus Status { get; set; }
    public string SupplierName { get; set; } = string.Empty;
}
