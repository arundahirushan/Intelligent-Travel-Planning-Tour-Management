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

public class ContractRequestService : IContractRequestService
{
    private readonly AppDbContext _db;

    public ContractRequestService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ContractRequestSummaryDto> CreateRequestAsync(CreateContractRequestDto dto, int supplierId)
    {
        var today = DateTime.UtcNow.Date;

        // 1. Check if they have an active valid contract
        var hasValidContract = await _db.Contracts.AnyAsync(c =>
            c.SupplierId == supplierId &&
            c.Status == ContractStatus.Active &&
            c.EndDate.Date >= today);

        if (hasValidContract)
            throw new ValidationException("Cannot submit a contract request while you already hold an active, valid contract.");

        // 2. Reject if supplier already has a pending request
        var hasPending = await _db.ContractRequests.AnyAsync(r =>
            r.SupplierId == supplierId &&
            r.Status == ContractRequestStatus.Pending);

        if (hasPending)
            throw new ValidationException("You already have an outstanding contract request pending review.");

        // 3. Validate request-type specific rules
        if (dto.RequestType == ContractRequestType.Renewal)
        {
            if (!dto.ExistingContractId.HasValue)
                throw new ValidationException("ExistingContractId is required for renewal requests.");

            var existing = await _db.Contracts.FindAsync(dto.ExistingContractId.Value);
            if (existing == null)
                throw new NotFoundException($"Existing contract with ID {dto.ExistingContractId.Value} was not found.");

            if (existing.SupplierId != supplierId)
                throw new ForbiddenException("The specified contract does not belong to your supplier account.");

            if (dto.RequestedStartDate.HasValue && dto.RequestedStartDate.Value.AddYears(dto.DurationInYears) <= existing.EndDate)
                throw new ValidationException("Requested contract period must extend beyond the existing contract's EndDate.");
        }
        else
        {
            var startDate = dto.RequestedStartDate ?? today;
            // End date validation is now implicitly handled by DurationInYears
        }

        var request = dto.ToEntity(supplierId);
        _db.ContractRequests.Add(request);
        await _db.SaveChangesAsync();

        return await LoadRequestSummaryAsync(request.Id);
    }

    public async Task<PagedResult<ContractRequestSummaryDto>> GetMyRequestsAsync(int supplierId, int page, int pageSize)
    {
        var query = _db.ContractRequests
            .Include(r => r.Supplier)
            .Where(r => r.SupplierId == supplierId)
            .OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => r.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<ContractRequestSummaryDto>
        {
            Items      = items,
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize
        };
    }

    public async Task<ContractRequestSummaryDto> GetByIdAsync(int id, int? requestingUserId, string? requestingUserRole)
    {
        var request = await _db.ContractRequests
            .Include(r => r.Supplier)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
            throw new NotFoundException($"Contract request with ID {id} was not found.");

        if (requestingUserRole == Roles.Supplier && requestingUserId.HasValue && request.SupplierId != requestingUserId.Value)
            throw new ForbiddenException("You do not have permission to view this contract request.");

        return request.ToSummaryDto();
    }

    public async Task<PagedResult<ContractRequestSummaryDto>> GetAllAdminAsync(string? status, int page, int pageSize)
    {
        var query = _db.ContractRequests
            .Include(r => r.Supplier)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ContractRequestStatus>(status, true, out var statusEnum))
        {
            query = query.Where(r => r.Status == statusEnum);
        }

        query = query.OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => r.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<ContractRequestSummaryDto>
        {
            Items      = items,
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize
        };
    }

    public async Task<ContractRequestSummaryDto> ApproveAsync(int id)
    {
        var request = await _db.ContractRequests
            .Include(r => r.ExistingContract)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
            throw new NotFoundException($"Contract request with ID {id} was not found.");

        if (request.Status != ContractRequestStatus.Pending)
            throw new ValidationException($"Only Pending requests can be approved. Current status is {request.Status}.");

        // 1. Prevent overlapping valid contracts.
        //    NOTE: Do NOT use "c.Id != request.ExistingContractId" here — when ExistingContractId
        //    is null, SQL evaluates "c.Id != NULL" as unknown/false, silently disabling the guard.
        var today = DateTime.UtcNow.Date;
        bool hasValidContract;

        if (request.RequestType == ContractRequestType.Renewal && request.ExistingContractId.HasValue)
        {
            // For a Renewal: block if there is ANY active valid contract OTHER than the one being renewed
            var excludeId = request.ExistingContractId.Value;
            hasValidContract = await _db.Contracts.AnyAsync(c =>
                c.SupplierId == request.SupplierId &&
                c.Status == ContractStatus.Active &&
                c.EndDate.Date >= today &&
                c.Id != excludeId);
        }
        else
        {
            // For a New request: block if ANY active valid contract exists at all
            hasValidContract = await _db.Contracts.AnyAsync(c =>
                c.SupplierId == request.SupplierId &&
                c.Status == ContractStatus.Active &&
                c.EndDate.Date >= today);
        }

        if (hasValidContract)
            throw new ValidationException("Cannot approve this request because the supplier already has a valid active contract.");

        if (request.RequestType == ContractRequestType.Renewal)
        {
            if (!request.ExistingContractId.HasValue)
                throw new ValidationException("Renewal request is missing ExistingContractId.");

            var existing = request.ExistingContract ?? await _db.Contracts.FindAsync(request.ExistingContractId.Value);
            if (existing == null)
                throw new NotFoundException($"Existing contract with ID {request.ExistingContractId.Value} was not found.");

            // Extend existing contract's EndDate (and Terms if provided), leave it as one row.
            existing.EndDate   = request.RequestedEndDate;
            existing.Status    = ContractStatus.Active; // In case an expired active contract is renewed
            if (!string.IsNullOrWhiteSpace(request.RequestedTerms))
            {
                existing.Terms = request.RequestedTerms;
            }
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else // New contract request
        {
            var newContract = new Contract
            {
                SupplierId = request.SupplierId,
                StartDate  = request.RequestedStartDate ?? DateTime.UtcNow.Date,
                EndDate    = request.RequestedEndDate,
                Terms      = request.RequestedTerms ?? string.Empty,
                Status     = ContractStatus.Active,
                CreatedAt  = DateTime.UtcNow,
                UpdatedAt  = DateTime.UtcNow
            };
            _db.Contracts.Add(newContract);
        }

        request.Status    = ContractRequestStatus.Approved;
        request.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return await LoadRequestSummaryAsync(id);
    }

    public async Task<ContractRequestSummaryDto> RejectAsync(int id, RejectContractRequestDto dto)
    {
        var request = await _db.ContractRequests.FindAsync(id);
        if (request == null)
            throw new NotFoundException($"Contract request with ID {id} was not found.");

        if (request.Status != ContractRequestStatus.Pending)
            throw new ValidationException($"Only Pending requests can be rejected. Current status is {request.Status}.");

        request.Status    = ContractRequestStatus.Rejected;
        request.AdminNote = dto.AdminNote;
        request.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return await LoadRequestSummaryAsync(id);
    }

    // ── Helper methods ────────────────────────────────────────────────────────

    private async Task<ContractRequestSummaryDto> LoadRequestSummaryAsync(int id)
    {
        var request = await _db.ContractRequests
            .Include(r => r.Supplier)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
            throw new NotFoundException($"Contract request with ID {id} was not found.");

        return request.ToSummaryDto();
    }
}
