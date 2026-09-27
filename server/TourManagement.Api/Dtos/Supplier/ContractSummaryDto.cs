using TourManagement.Api.Models;

namespace TourManagement.Api.Dtos.Supplier;

// Summary of a contract for list views.
// ComputedIsCurrentlyValid is computed at runtime in the mapping method.
public class ContractSummaryDto
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Terms { get; set; } = string.Empty;
    public ContractStatus Status { get; set; }

    // Live computed condition: Status == Active && EndDate >= today
    public bool ComputedIsCurrentlyValid { get; set; }
}
