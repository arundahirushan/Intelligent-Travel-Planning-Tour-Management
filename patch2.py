import sys

with open('server/TourManagement.Api/Services/Implementations/VehicleBookingService.cs', 'r') as f:
    content = f.read()

new_method = """    public async Task<PagedResult<VehicleBookingSummaryDto>> GetMyVehiclesBookingsAsync(
        int providerId, string? search, string? status, int page, int pageSize)
    {
        var query = _db.VehicleBookings
            .Include(b => b.Vehicle)
            .Include(b => b.Trip)
            .Where(b => b.Vehicle.ProviderId == providerId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookingStatus>(status, out var statusEnum))
            query = query.Where(b => b.Status == statusEnum);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(b => 
                b.Vehicle.VehicleType.Contains(search) || 
                b.Vehicle.Model.Contains(search) || 
                b.Vehicle.RegistrationNumber.Contains(search));
        }

        query = query.OrderByDescending(b => b.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => b.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<VehicleBookingSummaryDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    // ── Shared operations ────────────────────────────────────────────────────"""

content = content.replace("    // ── Shared operations ────────────────────────────────────────────────────", new_method)

with open('server/TourManagement.Api/Services/Implementations/VehicleBookingService.cs', 'w') as f:
    f.write(content)
