import sys

with open('server/TourManagement.Api/Services/Implementations/VehicleService.cs', 'r') as f:
    content = f.read()

# Add helper at the end of the file, right before private helpers
helper_code = """    public async Task<bool> IsVehicleBookedOnDateAsync(int vehicleId, DateTime date)
    {
        return await _db.VehicleBookings.AnyAsync(b =>
            b.VehicleId == vehicleId
            && (b.Status == BookingStatus.Held || b.Status == BookingStatus.Confirmed)
            && b.StartDate.Date <= date.Date
            && b.EndDate.Date >= date.Date
        );
    }

    // ── Private helpers ───────────────────────────────────────────────────────"""

content = content.replace("    // ── Private helpers ───────────────────────────────────────────────────────", helper_code)

# Replace the return blocks for VehicleSummaryDto
old_block = """        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => v.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<VehicleSummaryDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };"""

new_block = """        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => v.ToSummaryDto())
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        foreach (var item in items)
        {
            item.IsBookedToday = await IsVehicleBookedOnDateAsync(item.Id, today);
        }

        return new PagedResult<VehicleSummaryDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };"""

content = content.replace(old_block, new_block)

with open('server/TourManagement.Api/Services/Implementations/VehicleService.cs', 'w') as f:
    f.write(content)
