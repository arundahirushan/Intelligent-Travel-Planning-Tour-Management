namespace TourManagement.Api.Models;

// Represents a formal service contract between a Supplier and the tour management platform.
// Note: Only Active and Terminated are stored in the database. Expired is computed at query time.
public class Contract
{
    public int Id { get; set; }

    // The Supplier user party to this contract.
    public int SupplierId { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Terms and conditions or description of the contract agreement.
    public string Terms { get; set; } = string.Empty;

    // Stored as a string in the DB (Active, Terminated).
    public ContractStatus Status { get; set; } = ContractStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User Supplier { get; set; } = null!;
    public List<ContractRequest> ContractRequests { get; set; } = new();
}
