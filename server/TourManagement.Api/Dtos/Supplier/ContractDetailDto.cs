using TourManagement.Api.Models;

namespace TourManagement.Api.Dtos.Supplier;

// Full detail view of a contract.
public class ContractDetailDto
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Terms { get; set; } = string.Empty;
    public ContractStatus Status { get; set; }
    public bool ComputedIsCurrentlyValid { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
