using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Supplier;

// Request body for an Admin rejecting a contract request.
public class RejectContractRequestDto
{
    [StringLength(1000, ErrorMessage = "AdminNote cannot exceed 1000 characters.")]
    public string? AdminNote { get; set; }
}
