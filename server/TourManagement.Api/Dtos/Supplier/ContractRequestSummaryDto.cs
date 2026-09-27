using TourManagement.Api.Models;

namespace TourManagement.Api.Dtos.Supplier;

// Summary of a contract request for list and detail views.
public class ContractRequestSummaryDto
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public ContractRequestType RequestType { get; set; }
    public int? ExistingContractId { get; set; }
    public DateTime? RequestedStartDate { get; set; }
    public DateTime RequestedEndDate { get; set; }
    public string? RequestedTerms { get; set; }
    public ContractRequestStatus Status { get; set; }
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
