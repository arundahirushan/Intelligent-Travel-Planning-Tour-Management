namespace TourManagement.Api.Dtos.Supplier;

// Quick summary of a supplier's current contract status for dashboard display.
public class SupplierContractStatusDto
{
    public int SupplierId { get; set; }
    public bool IsValid { get; set; }
    public int? ContractId { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
}
