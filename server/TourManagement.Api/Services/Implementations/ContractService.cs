using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Common.Exceptions;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Supplier;
using TourManagement.Api.Mappings;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Services.Implementations;

public class ContractService : IContractService
{
    private readonly AppDbContext _db;

    public ContractService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ContractDetailDto> CreateFirstContractAsync(CreateContractDto dto)
    {
        // Confirm supplier exists
        var supplier = await _db.Users.FirstOrDefaultAsync(u => u.Id == dto.SupplierId);
        if (supplier == null)
            throw new NotFoundException($"Supplier with ID {dto.SupplierId} was not found.");

        if (supplier.Role != Roles.Supplier)
            throw new ValidationException($"User with ID {dto.SupplierId} does not hold the Supplier role.");

        if (dto.EndDate <= dto.StartDate)
            throw new ValidationException("EndDate must be after StartDate.");

        // Reject if supplier already has a currently-valid contract
        var today = DateTime.UtcNow.Date;
        var hasValidContract = await _db.Contracts.AnyAsync(c =>
            c.SupplierId == dto.SupplierId &&
            c.Status == ContractStatus.Active &&
            c.EndDate.Date >= today);

        if (hasValidContract)
            throw new ValidationException("This supplier already has an active, valid contract. A renewal request or termination is required before creating another contract.");

        var contract = dto.ToEntity();
        _db.Contracts.Add(contract);
        await _db.SaveChangesAsync();

        return await LoadContractDetailAsync(contract.Id);
    }

    public async Task<PagedResult<ContractSummaryDto>> GetAllContractsAsync(
        string? computedStatus, int? supplierId, string? sort, int page, int pageSize)
    {
        var today = DateTime.UtcNow.Date;
        var query = _db.Contracts
            .Include(c => c.Supplier)
            .AsQueryable();

        if (supplierId.HasValue)
            query = query.Where(c => c.SupplierId == supplierId.Value);

        // Filter by computed status: Active, Expired, or Terminated
        if (!string.IsNullOrWhiteSpace(computedStatus))
        {
            query = computedStatus.Trim().ToLower() switch
            {
                "active"     => query.Where(c => c.Status == ContractStatus.Active && c.EndDate.Date >= today),
                "expired"    => query.Where(c => c.Status == ContractStatus.Active && c.EndDate.Date < today),
                "terminated" => query.Where(c => c.Status == ContractStatus.Terminated),
                _            => query
            };
        }

        query = sort?.ToLower() switch
        {
            "enddate" or "enddate_asc" => query.OrderBy(c => c.EndDate),
            "enddate_desc"             => query.OrderByDescending(c => c.EndDate),
            "oldest"                   => query.OrderBy(c => c.CreatedAt),
            _                          => query.OrderByDescending(c => c.CreatedAt)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => c.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<ContractSummaryDto>
        {
            Items      = items,
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize
        };
    }

    public async Task<ContractDetailDto> GetByIdAsync(int id)
    {
        return await LoadContractDetailAsync(id);
    }

    public async Task<ContractDetailDto> TerminateAsync(int id)
    {
        var contract = await _db.Contracts
            .Include(c => c.Supplier)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract == null)
            throw new NotFoundException($"Contract with ID {id} was not found.");

        if (contract.Status != ContractStatus.Active)
            throw new ValidationException($"Only Active contracts can be terminated. Current status is {contract.Status}.");

        contract.Status    = ContractStatus.Terminated;
        contract.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return contract.ToDetailDto();
    }

    // ── Business-specific operation ───────────────────────────────────────────
    // Checks if the supplier currently holds any contract where Status == Active AND EndDate >= today.
    public async Task<bool> IsContractCurrentlyValidAsync(int supplierId)
    {
        var today = DateTime.UtcNow.Date;
        return await _db.Contracts.AnyAsync(c =>
            c.SupplierId == supplierId &&
            c.Status == ContractStatus.Active &&
            c.EndDate.Date >= today);
    }

    public async Task<SupplierContractStatusDto> GetContractStatusSummaryAsync(int supplierId)
    {
        var today = DateTime.UtcNow.Date;

        // Check for active, unexpired contract
        var activeContract = await _db.Contracts
            .Where(c => c.SupplierId == supplierId && c.Status == ContractStatus.Active && c.EndDate.Date >= today)
            .OrderByDescending(c => c.EndDate)
            .FirstOrDefaultAsync();

        if (activeContract != null)
        {
            return new SupplierContractStatusDto
            {
                SupplierId = supplierId,
                IsValid    = true,
                ContractId = activeContract.Id,
                EndDate    = activeContract.EndDate,
                Status     = "Active"
            };
        }

        // Check latest contract for non-active status diagnosis
        var latestContract = await _db.Contracts
            .Where(c => c.SupplierId == supplierId)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        string statusDesc;
        if (latestContract == null)
        {
            statusDesc = "None";
        }
        else if (latestContract.Status == ContractStatus.Terminated)
        {
            statusDesc = "Terminated";
        }
        else
        {
            statusDesc = "Expired";
        }

        return new SupplierContractStatusDto
        {
            SupplierId = supplierId,
            IsValid    = false,
            ContractId = latestContract?.Id,
            EndDate    = latestContract?.EndDate,
            Status     = statusDesc
        };
    }

    // ── Helper methods ────────────────────────────────────────────────────────

    private async Task<ContractDetailDto> LoadContractDetailAsync(int id)
    {
        var contract = await _db.Contracts
            .Include(c => c.Supplier)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract == null)
            throw new NotFoundException($"Contract with ID {id} was not found.");

        return contract.ToDetailDto();
    }
}
