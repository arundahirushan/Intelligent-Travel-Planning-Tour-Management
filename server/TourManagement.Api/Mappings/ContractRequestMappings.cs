using TourManagement.Api.Dtos.Supplier;
using TourManagement.Api.Models;

namespace TourManagement.Api.Mappings;

// Extension methods for converting ContractRequest entities to/from DTOs.
public static class ContractRequestMappings
{
    public static ContractRequestSummaryDto ToSummaryDto(this ContractRequest request)
    {
        return new ContractRequestSummaryDto
        {
            Id                 = request.Id,
            SupplierId         = request.SupplierId,
            SupplierName       = request.Supplier?.FullName ?? string.Empty,
            RequestType        = request.RequestType,
            ExistingContractId = request.ExistingContractId,
            RequestedStartDate = request.RequestedStartDate,
            RequestedEndDate   = request.RequestedEndDate,
            RequestedTerms     = request.RequestedTerms,
            Status             = request.Status,
            AdminNote          = request.AdminNote,
            CreatedAt          = request.CreatedAt,
            UpdatedAt          = request.UpdatedAt
        };
    }

    public static ContractRequest ToEntity(this CreateContractRequestDto dto, int supplierId)
    {
        return new ContractRequest
        {
            SupplierId         = supplierId,
            RequestType        = dto.RequestType,
            ExistingContractId = dto.ExistingContractId,
            RequestedStartDate = dto.RequestedStartDate,
            RequestedEndDate   = dto.RequestedEndDate,
            RequestedTerms     = dto.RequestedTerms,
            Status             = ContractRequestStatus.Pending,
            CreatedAt          = DateTime.UtcNow,
            UpdatedAt          = DateTime.UtcNow
        };
    }
}
