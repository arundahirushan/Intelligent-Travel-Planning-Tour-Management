using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Profile;
using TourManagement.Api.Models;

namespace TourManagement.Api.Profile;

public class HotelOwnerDeletionGuard : IAccountDeletionGuard
{
    private readonly AppDbContext _db;

    public HotelOwnerDeletionGuard(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DeletionEligibilityDto> CheckAsync(int userId)
    {
        var today = DateTime.UtcNow.Date;

        var blockingCount = await _db.HotelBookings
            .Where(b => b.Room.Hotel.OwnerId == userId
                     && (b.Status == BookingStatus.Held || b.Status == BookingStatus.Confirmed)
                     && b.CheckOutDate >= today)
            .CountAsync();

        return new DeletionEligibilityDto
        {
            CanDelete = blockingCount == 0,
            BlockingCount = blockingCount,
            BlockingMessage = blockingCount > 0
                ? $"You have {blockingCount} active or upcoming hotel booking(s). These must be completed or cancelled before deleting your account."
                : null
        };
    }
}
