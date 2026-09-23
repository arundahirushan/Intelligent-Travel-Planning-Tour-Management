using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Data;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Implementations;

namespace TourManagement.Api.Tests.Services;

public class OccupancyTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task ComputeOccupancy_NoRooms_ReturnsZero()
    {
        var db = CreateDb(nameof(ComputeOccupancy_NoRooms_ReturnsZero));
        db.Hotels.Add(new Hotel { Id = 1 });
        await db.SaveChangesAsync();

        var service = new HotelService(db);
        var result = await service.ComputeOccupancyAsync(1, DateTime.UtcNow.Date);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ComputeOccupancy_OneRoomBooked_ReturnsPercentage()
    {
        var db = CreateDb(nameof(ComputeOccupancy_OneRoomBooked_ReturnsPercentage));
        db.Hotels.Add(new Hotel { Id = 1 });
        db.Rooms.Add(new Room { Id = 1, HotelId = 1, TotalRooms = 2 });
        
        var today = DateTime.UtcNow.Date;

        // Booking covering today, for 1 room out of 2 total -> 50%
        db.HotelBookings.Add(new HotelBooking 
        { 
            Id = 1, 
            RoomId = 1, 
            Status = BookingStatus.Confirmed,
            CheckInDate = today.AddDays(-1),
            CheckOutDate = today.AddDays(1),
            NumberOfRooms = 1
        });
        await db.SaveChangesAsync();

        var service = new HotelService(db);
        var result = await service.ComputeOccupancyAsync(1, today);

        Assert.Equal(50, result);
    }
}
