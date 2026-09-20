namespace TourManagement.Api.Models;

// Represents a supplier-initiated request for a new contract or renewal of an existing contract.
// Reviewed and approved/rejected by an Admin.
public class ContractRequest
{
    public int Id { get; set; }

    // The Supplier user submitting the request.
    public int SupplierId { get; set; }

    // Renewal or New.
    public ContractRequestType RequestType { get; set; }

    // Required when RequestType = Renewal; null when RequestType = New.
    public int? ExistingContractId { get; set; }

    // Optional/only meaningful for New requests.
    public DateTime? RequestedStartDate { get; set; }

    // The requested end date for the contract.
    public DateTime RequestedEndDate { get; set; }

    // Proposed terms or changes requested by the supplier.
    public string? RequestedTerms { get; set; }

    // Review status (Pending, Approved, Rejected).
    public ContractRequestStatus Status { get; set; } = ContractRequestStatus.Pending;

    // Optional explanation or comment recorded by the reviewing Admin.
    public string? AdminNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User Supplier { get; set; } = null!;
    public Contract? ExistingContract { get; set; }
}
