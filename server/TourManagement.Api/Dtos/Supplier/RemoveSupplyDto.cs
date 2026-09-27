using System.ComponentModel.DataAnnotations;
using TourManagement.Api.Models;

namespace TourManagement.Api.Dtos.Supplier;

// Request body for an Admin to remove a supply item.
public class RemoveSupplyDto
{
    [Required(ErrorMessage = "Removal reason is required.")]
    public RemovalReason RemovalReason { get; set; }

    [StringLength(500, ErrorMessage = "Removal note cannot exceed 500 characters.")]
    public string? RemovalNote { get; set; }
}
