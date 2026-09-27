using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Supplier;

// Request body for an Admin creating the first contract for a supplier.
public class CreateContractDto
{
    [Required(ErrorMessage = "SupplierId is required.")]
    public int SupplierId { get; set; }

    [Required(ErrorMessage = "StartDate is required.")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "EndDate is required.")]
    public DateTime EndDate { get; set; }

    [Required(ErrorMessage = "Terms are required.")]
    [StringLength(2000, ErrorMessage = "Terms cannot exceed 2000 characters.")]
    public string Terms { get; set; } = string.Empty;
}
