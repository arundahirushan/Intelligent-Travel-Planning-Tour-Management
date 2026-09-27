using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Supplier;

// Request body for updating an existing supply item.
public class UpdateSupplyDto
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required.")]
    [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters.")]
    public string Category { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Price per unit is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "PricePerUnit must be greater than 0.")]
    public decimal PricePerUnit { get; set; }

    [Required(ErrorMessage = "Stock quantity is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "StockQuantity cannot be negative.")]
    public int StockQuantity { get; set; }
}
