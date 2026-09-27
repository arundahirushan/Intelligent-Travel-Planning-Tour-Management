using TourManagement.Api.Dtos.Supplier;
using TourManagement.Api.Models;

namespace TourManagement.Api.Mappings;

// Extension methods for converting Supply entities to/from DTOs.
public static class SupplyMappings
{
    // Lightweight summary — assumes supply.Supplier may be loaded.
    public static SupplySummaryDto ToSummaryDto(this Supply supply)
    {
        return new SupplySummaryDto
        {
            Id           = supply.Id,
            Name         = supply.Name,
            Category     = supply.Category,
            PricePerUnit = supply.PricePerUnit,
            StockQuantity = supply.StockQuantity,
            Status       = supply.Status,
            SupplierName = supply.Supplier?.FullName ?? string.Empty
        };
    }

    // Full detail view.
    public static SupplyDetailDto ToDetailDto(this Supply supply)
    {
        return new SupplyDetailDto
        {
            Id            = supply.Id,
            SupplierId    = supply.SupplierId,
            SupplierName  = supply.Supplier?.FullName ?? string.Empty,
            Name          = supply.Name,
            Category      = supply.Category,
            Description   = supply.Description,
            PricePerUnit  = supply.PricePerUnit,
            StockQuantity = supply.StockQuantity,
            Status        = supply.Status,
            RemovalReason = supply.RemovalReason,
            RemovalNote   = supply.RemovalNote,
            CreatedAt     = supply.CreatedAt,
            UpdatedAt     = supply.UpdatedAt
        };
    }

    // Creates a new Supply entity from a CreateSupplyDto.
    // Status is set to Active immediately (no per-listing approval needed).
    public static Supply ToEntity(this CreateSupplyDto dto, int supplierId)
    {
        return new Supply
        {
            SupplierId    = supplierId,
            Name          = dto.Name,
            Category      = dto.Category,
            Description   = dto.Description,
            PricePerUnit  = dto.PricePerUnit,
            StockQuantity = dto.StockQuantity,
            Status        = SupplyStatus.Active,
            CreatedAt     = DateTime.UtcNow,
            UpdatedAt     = DateTime.UtcNow
        };
    }

    // Applies update values onto an existing Supply entity.
    public static void UpdateFromDto(this Supply supply, UpdateSupplyDto dto)
    {
        supply.Name          = dto.Name;
        supply.Category      = dto.Category;
        supply.Description   = dto.Description;
        supply.PricePerUnit  = dto.PricePerUnit;
        supply.StockQuantity = dto.StockQuantity;
        supply.UpdatedAt     = DateTime.UtcNow;
    }
}
