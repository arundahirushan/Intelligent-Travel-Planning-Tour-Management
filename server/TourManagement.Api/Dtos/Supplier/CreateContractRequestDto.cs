using System.ComponentModel.DataAnnotations;
using TourManagement.Api.Models;

namespace TourManagement.Api.Dtos.Supplier;

// Request body for a supplier submitting a new contract or renewal request.
public class CreateContractRequestDto
{
    [Required(ErrorMessage = "RequestType is required.")]
    public ContractRequestType RequestType { get; set; }

    // Required when RequestType = Renewal; null when RequestType = New.
    public int? ExistingContractId { get; set; }

    // Meaningful for New requests; optional.
    public DateTime? RequestedStartDate { get; set; }

    [Required(ErrorMessage = "RequestedEndDate is required.")]
    public DateTime RequestedEndDate { get; set; }

    [StringLength(2000, ErrorMessage = "RequestedTerms cannot exceed 2000 characters.")]
    public string? RequestedTerms { get; set; }
}
