using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Common.Exceptions;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.SupplyOrders;
using TourManagement.Api.Mappings;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Services.Implementations;

public class SupplyOrderService : ISupplyOrderService
{
    private readonly AppDbContext _db;
    private readonly IContractService _contractService;

    public SupplyOrderService(AppDbContext db, IContractService contractService)
    {
        _db = db;
        _contractService = contractService;
    }

    public async Task<SupplyOrderSummaryDto> CreateAsync(CreateSupplyOrderDto dto, int travelerId)
    {
        // 1. Verify trip ownership
        var trip = await _db.Trips.FirstOrDefaultAsync(t => t.Id == dto.TripId);
        if (trip == null)
            throw new NotFoundException($"Trip with ID {dto.TripId} not found.");
            
        if (trip.TravelerId != travelerId)
            throw new ForbiddenException("You do not have permission to order supplies for this trip.");

        // 2. Load the Supply
        var supply = await _db.Supplies.FirstOrDefaultAsync(s => s.Id == dto.SupplyId);
        if (supply == null || supply.Status != SupplyStatus.Active)
            throw new ValidationException("Supply is not available or does not exist.");

        // 3. Contract Validity Check (using existing IContractService)
        bool hasValidContract = await _contractService.IsContractCurrentlyValidAsync(supply.SupplierId);
        if (!hasValidContract)
            throw new ValidationException("This supplier does not currently have a valid contract.");

        // 4. Stock Check
        if (supply.StockQuantity < dto.Quantity)
            throw new ValidationException("Insufficient stock available.");

        // 5. Decrement Stock and create order
        supply.StockQuantity -= dto.Quantity;
        supply.UpdatedAt = DateTime.UtcNow;

        var order = new SupplyOrder
        {
            TripId = dto.TripId,
            SupplyId = dto.SupplyId,
            Quantity = dto.Quantity,
            PriceAtOrderTime = supply.PricePerUnit,
            Status = BookingStatus.Held,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.SupplyOrders.Add(order);

        // Atomic save ensures stock doesn't go negative on race conditions
        await _db.SaveChangesAsync();

        order.Supply = supply; // populate for mapping
        return order.ToSummaryDto();
    }

    public async Task<SupplyOrderSummaryDto> UpdateAsync(int id, UpdateSupplyOrderDto dto, int travelerId)
    {
        var order = await _db.SupplyOrders
            .Include(o => o.Trip)
            .Include(o => o.Supply)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            throw new NotFoundException($"SupplyOrder with ID {id} not found.");

        if (order.Trip.TravelerId != travelerId)
            throw new ForbiddenException("You do not have permission to modify this order.");

        if (order.Status != BookingStatus.Held)
            throw new ValidationException($"Cannot modify a booking that is already {order.Status}.");

        // Restore old quantity
        order.Supply.StockQuantity += order.Quantity;

        var newSupply = await _db.Supplies.FirstOrDefaultAsync(s => s.Id == dto.SupplyId);
        if (newSupply == null || newSupply.Status != SupplyStatus.Active)
            throw new ValidationException("Supply is not available or does not exist.");

        bool hasValidContract = await _contractService.IsContractCurrentlyValidAsync(newSupply.SupplierId);
        if (!hasValidContract)
            throw new ValidationException("This supplier does not currently have a valid contract.");

        if (newSupply.StockQuantity < dto.Quantity)
            throw new ValidationException("Insufficient stock available.");

        newSupply.StockQuantity -= dto.Quantity;
        newSupply.UpdatedAt = DateTime.UtcNow;

        order.SupplyId = dto.SupplyId;
        order.Quantity = dto.Quantity;
        order.PriceAtOrderTime = newSupply.PricePerUnit;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        order.Supply = newSupply;
        return order.ToSummaryDto();
    }

    public async Task DeleteAsync(int id, int requestingUserId, string requestingUserRole)
    {
        var order = await _db.SupplyOrders
            .Include(o => o.Trip)
            .Include(o => o.Supply)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            throw new NotFoundException($"SupplyOrder with ID {id} not found.");

        bool isOwner = order.Trip.TravelerId == requestingUserId;
        bool isAdmin = requestingUserRole == Roles.Admin || requestingUserRole == Roles.SuperAdmin;

        if (!isOwner && !isAdmin)
            throw new ForbiddenException("You do not have permission to delete this order.");

        if (order.Status != BookingStatus.Held)
            throw new ValidationException($"Cannot modify a booking that is already {order.Status}.");

        order.Supply.StockQuantity += order.Quantity;
        order.Supply.UpdatedAt = DateTime.UtcNow;

        _db.SupplyOrders.Remove(order);
        await _db.SaveChangesAsync();
    }

    public async Task<PagedResult<SupplyOrderSummaryDto>> GetMyOrdersAsync(
        int travelerId, string? status, string? sort, int page, int pageSize)
    {
        var query = _db.SupplyOrders
            .Include(o => o.Supply)
            .Where(o => o.Trip.TravelerId == travelerId);

        return await ApplyFiltersAndPaginateAsync(query, status, sort, page, pageSize);
    }

    public async Task<SupplyOrderSummaryDto> CancelAsync(int id, int userId)
    {
        var order = await _db.SupplyOrders
            .Include(o => o.Trip)
            .Include(o => o.Supply)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            throw new NotFoundException($"SupplyOrder with ID {id} not found.");

        // Check if caller is Admin or SuperAdmin
        var user = await _db.Users.FindAsync(userId);
        bool isAdmin = user != null && (user.Role == Roles.Admin || user.Role == Roles.SuperAdmin);

        // Allow owner or admin to cancel
        if (!isAdmin && order.Trip.TravelerId != userId)
            throw new ForbiddenException("You do not have permission to cancel this order.");

        if (order.Status == BookingStatus.Cancelled)
            throw new ValidationException("Order is already cancelled.");

        // Restore stock
        order.Supply.StockQuantity += order.Quantity;
        order.Supply.UpdatedAt = DateTime.UtcNow;

        order.Status = BookingStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return order.ToSummaryDto();
    }

    public async Task<PagedResult<SupplyOrderSummaryDto>> GetReceivedOrdersAsync(
        int supplierId, string? status, string? sort, int page, int pageSize)
    {
        var query = _db.SupplyOrders
            .Include(o => o.Supply)
            .Where(o => o.Supply.SupplierId == supplierId);

        return await ApplyFiltersAndPaginateAsync(query, status, sort, page, pageSize);
    }

    public async Task<PagedResult<SupplyOrderSummaryDto>> GetAllAdminAsync(
        string? status, string? sort, int page, int pageSize)
    {
        var query = _db.SupplyOrders
            .Include(o => o.Supply)
            .AsQueryable();

        return await ApplyFiltersAndPaginateAsync(query, status, sort, page, pageSize);
    }

    private async Task<PagedResult<SupplyOrderSummaryDto>> ApplyFiltersAndPaginateAsync(
        IQueryable<SupplyOrder> query, string? status, string? sort, int page, int pageSize)
    {
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BookingStatus>(status, true, out var statusEnum))
        {
            query = query.Where(o => o.Status == statusEnum);
        }

        query = sort?.ToLower() switch
        {
            "oldest" => query.OrderBy(o => o.CreatedAt),
            "quantity" => query.OrderByDescending(o => o.Quantity),
            _ => query.OrderByDescending(o => o.CreatedAt)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<SupplyOrderSummaryDto>
        {
            Items = items.Select(o => o.ToSummaryDto()).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
