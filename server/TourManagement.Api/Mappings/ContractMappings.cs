using TourManagement.Api.Dtos.Supplier;
using TourManagement.Api.Models;

namespace TourManagement.Api.Mappings;

// Extension methods for converting Contract entities to/from DTOs.
public static class ContractMappings
{
    public static ContractSummaryDto ToSummaryDto(this Contract contract)
    {
        return new ContractSummaryDto
        {
            Id                       = contract.Id,
            SupplierId               = contract.SupplierId,
            SupplierName             = contract.Supplier?.FullName ?? string.Empty,
            StartDate                = contract.StartDate,
            EndDate                  = contract.EndDate,
            Terms                    = contract.Terms,
            Status                   = contract.Status,
            // ComputedIsCurrentlyValid is true only when Status is Active AND EndDate >= today.
            ComputedIsCurrentlyValid = contract.Status == ContractStatus.Active && contract.EndDate.Date >= DateTime.UtcNow.Date
        };
    }

    public static ContractDetailDto ToDetailDto(this Contract contract)
    {
        return new ContractDetailDto
        {
            Id                       = contract.Id,
            SupplierId               = contract.SupplierId,
            SupplierName             = contract.Supplier?.FullName ?? string.Empty,
            StartDate                = contract.StartDate,
            EndDate                  = contract.EndDate,
            Terms                    = contract.Terms,
            Status                   = contract.Status,
            ComputedIsCurrentlyValid = contract.Status == ContractStatus.Active && contract.EndDate.Date >= DateTime.UtcNow.Date,
            CreatedAt                = contract.CreatedAt,
            UpdatedAt                = contract.UpdatedAt
        };
    }

    public static Contract ToEntity(this CreateContractDto dto)
    {
        return new Contract
        {
            SupplierId = dto.SupplierId,
            StartDate  = dto.StartDate,
            EndDate    = dto.EndDate,
            Terms      = dto.Terms,
            Status     = ContractStatus.Active,
            CreatedAt  = DateTime.UtcNow,
            UpdatedAt  = DateTime.UtcNow
        };
    }
}
