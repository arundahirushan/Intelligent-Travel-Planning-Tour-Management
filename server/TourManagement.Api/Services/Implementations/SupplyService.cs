using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Exceptions;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Supplier;
using TourManagement.Api.Mappings;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Services.Implementations;

public class SupplyService : ISupplyService
{
    private readonly AppDbContext _db;

    public SupplyService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<SupplyDetailDto> CreateAsync(CreateSupplyDto dto, int supplierId)
    {
        ValidatePriceAndStock(dto.PricePerUnit, dto.StockQuantity);

        var supply = dto.ToEntity(supplierId);
        _db.Supplies.Add(supply);
        await _db.SaveChangesAsync();

        return await LoadSupplyDetailAsync(supply.Id);
    }

    public async Task<PagedResult<SupplySummaryDto>> GetMySuppliesAsync(
        int supplierId, string? search, string? category, string? status, string? sort, int page, int pageSize)
    {
        var query = _db.Supplies
            .Include(s => s.Supplier)
            .Where(s => s.SupplierId == supplierId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(term) || s.Category.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim().ToLower();
            query = query.Where(s => s.Category.ToLower() == cat);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SupplyStatus>(status, true, out var statusEnum))
        {
            query = query.Where(s => s.Status == statusEnum);
        }

        query = sort?.ToLower() switch
        {
            "price" or "price_asc"  => query.OrderBy(s => s.PricePerUnit),
            "price_desc"            => query.OrderByDescending(s => s.PricePerUnit),
            "name"                  => query.OrderBy(s => s.Name),
            "stock"                 => query.OrderBy(s => s.StockQuantity),
            "oldest"                => query.OrderBy(s => s.CreatedAt),
            _                       => query.OrderByDescending(s => s.CreatedAt)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => s.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<SupplySummaryDto>
        {
            Items      = items,
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize
        };
    }

    public async Task<SupplyDetailDto> GetMySupplyByIdAsync(int id, int supplierId)
    {
        var supply = await GetSupplyOrThrowAsync(id);
        CheckOwnership(supply, supplierId);
        return await LoadSupplyDetailAsync(id);
    }

    public async Task<SupplyDetailDto> UpdateAsync(int id, UpdateSupplyDto dto, int supplierId)
    {
        var supply = await GetSupplyOrThrowAsync(id);
        CheckOwnership(supply, supplierId);
        ValidatePriceAndStock(dto.PricePerUnit, dto.StockQuantity);

        supply.UpdateFromDto(dto);
        await _db.SaveChangesAsync();

        return await LoadSupplyDetailAsync(id);
    }

    public async Task DeactivateAsync(int id, int supplierId)
    {
        var supply = await GetSupplyOrThrowAsync(id);
        CheckOwnership(supply, supplierId);

        supply.Status    = SupplyStatus.Inactive;
        supply.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task<SupplyDetailDto> RepublishAsync(int id, int supplierId)
    {
        var supply = await GetSupplyOrThrowAsync(id);
        CheckOwnership(supply, supplierId);

        if (supply.Status == SupplyStatus.Active)
            throw new ValidationException("Supply is already active.");

        supply.Status        = SupplyStatus.Active;
        supply.RemovalReason = null;
        supply.RemovalNote   = null;
        supply.UpdatedAt     = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return await LoadSupplyDetailAsync(id);
    }

    public async Task<PagedResult<SupplySummaryDto>> GetAllAdminAsync(
        string? search, string? category, string? status, int? supplierId, string? sort, int page, int pageSize)
    {
        var query = _db.Supplies
            .Include(s => s.Supplier)
            .AsQueryable();

        if (supplierId.HasValue)
            query = query.Where(s => s.SupplierId == supplierId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(term) || s.Category.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim().ToLower();
            query = query.Where(s => s.Category.ToLower() == cat);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SupplyStatus>(status, true, out var statusEnum))
        {
            query = query.Where(s => s.Status == statusEnum);
        }

        query = sort?.ToLower() switch
        {
            "price" or "price_asc" => query.OrderBy(s => s.PricePerUnit),
            "price_desc"           => query.OrderByDescending(s => s.PricePerUnit),
            "name"                 => query.OrderBy(s => s.Name),
            "stock"                => query.OrderBy(s => s.StockQuantity),
            "oldest"               => query.OrderBy(s => s.CreatedAt),
            _                      => query.OrderByDescending(s => s.CreatedAt)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => s.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<SupplySummaryDto>
        {
            Items      = items,
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize
        };
    }

    public async Task<SupplyDetailDto> RemoveAsync(int id, RemoveSupplyDto dto)
    {
        var supply = await GetSupplyOrThrowAsync(id);

        supply.Status        = SupplyStatus.Removed;
        supply.RemovalReason = dto.RemovalReason;
        supply.RemovalNote   = dto.RemovalNote;
        supply.UpdatedAt     = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return await LoadSupplyDetailAsync(id);
    }

    public async Task<PagedResult<SupplySummaryDto>> BrowseActiveAsync(
        string? search, string? category, string? sort, int page, int pageSize)
    {
        var query = _db.Supplies
            .Include(s => s.Supplier)
            .Where(s => s.Status == SupplyStatus.Active);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(term) || s.Category.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim().ToLower();
            query = query.Where(s => s.Category.ToLower() == cat);
        }

        query = sort?.ToLower() switch
        {
            "price" or "price_asc" => query.OrderBy(s => s.PricePerUnit),
            "price_desc"           => query.OrderByDescending(s => s.PricePerUnit),
            "name"                 => query.OrderBy(s => s.Name),
            "oldest"               => query.OrderBy(s => s.CreatedAt),
            _                      => query.OrderByDescending(s => s.CreatedAt)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => s.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<SupplySummaryDto>
        {
            Items      = items,
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize
        };
    }

    // ── Helper methods ────────────────────────────────────────────────────────

    private async Task<Supply> GetSupplyOrThrowAsync(int id)
    {
        var supply = await _db.Supplies.FindAsync(id);
        if (supply == null)
            throw new NotFoundException($"Supply with ID {id} was not found.");
        return supply;
    }

    private async Task<SupplyDetailDto> LoadSupplyDetailAsync(int id)
    {
        var supply = await _db.Supplies
            .Include(s => s.Supplier)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (supply == null)
            throw new NotFoundException($"Supply with ID {id} was not found.");

        return supply.ToDetailDto();
    }

    private static void CheckOwnership(Supply supply, int requestingSupplierId)
    {
        if (supply.SupplierId != requestingSupplierId)
            throw new ForbiddenException("You do not have permission to access or modify this supply.");
    }

    private static void ValidatePriceAndStock(decimal price, int stock)
    {
        if (price <= 0)
            throw new ValidationException("PricePerUnit must be greater than 0.");
        if (stock < 0)
            throw new ValidationException("StockQuantity cannot be negative.");
    }
}
