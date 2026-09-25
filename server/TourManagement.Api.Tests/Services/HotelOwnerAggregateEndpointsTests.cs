using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Data;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Implementations;

namespace TourManagement.Api.Tests.Services;

public class HotelOwnerAggregateEndpointsTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var db = new AppDbContext(options);

        // Seed destination
        db.Destinations.Add(new Destination { Id = 1, Name = "Galle", Region = "South", Description = "Test" });

        // Seed owners
        db.Users.Add(new User { Id = 10, FullName = "Owner 1", Email = "o1@t.com", PasswordHash = "h", Role = "HotelOwner" });
        db.Users.Add(new User { Id = 11, FullName = "Owner 2", Email = "o2@t.com", PasswordHash = "h", Role = "HotelOwner" });

        // Seed Traveler
        db.Users.Add(new User { Id = 20, FullName = "Traveler", Email = "t@t.com", PasswordHash = "h", Role = "Traveler" });
        db.Trips.Add(new Trip { Id = 1, TravelerId = 20, Title = "Trip", StartDate = new DateTime(2026, 9, 30), EndDate = new DateTime(2026, 10, 10), Budget = 10000, GroupSize = 2 });

        // Owner 1 has two hotels
        db.Hotels.Add(new Hotel { Id = 1, OwnerId = 10, DestinationId = 1, Name = "Hotel A", Address = "Test", ContactPhone = "123", Description = "Test", Status = HotelStatus.Active });
        db.Hotels.Add(new Hotel { Id = 2, OwnerId = 10, DestinationId = 1, Name = "Hotel B", Address = "Test", ContactPhone = "123", Description = "Test", Status = HotelStatus.Active });
        
        // Owner 2 has one hotel
        db.Hotels.Add(new Hotel { Id = 3, OwnerId = 11, DestinationId = 1, Name = "Hotel C", Address = "Test", ContactPhone = "123", Description = "Test", Status = HotelStatus.Active });

        // Owner 1 rooms
        db.Rooms.Add(new Room { Id = 1, HotelId = 1, RoomType = "Standard", TotalRooms = 2, Status = RoomStatus.Active });
        db.Rooms.Add(new Room { Id = 2, HotelId = 2, RoomType = "Deluxe", TotalRooms = 2, Status = RoomStatus.Active });

        // Owner 2 room
        db.Rooms.Add(new Room { Id = 3, HotelId = 3, RoomType = "Suite", TotalRooms = 2, Status = RoomStatus.Active });

        // Owner 1 bookings (one for each hotel)
        db.HotelBookings.Add(new HotelBooking { Id = 1, TripId = 1, RoomId = 1, CheckInDate = new DateTime(2026, 10, 1), CheckOutDate = new DateTime(2026, 10, 5), NumberOfRooms = 1, Status = BookingStatus.Confirmed });
        db.HotelBookings.Add(new HotelBooking { Id = 2, TripId = 1, RoomId = 2, CheckInDate = new DateTime(2026, 10, 5), CheckOutDate = new DateTime(2026, 10, 7), NumberOfRooms = 1, Status = BookingStatus.Confirmed });

        // Owner 2 bookings
        db.HotelBookings.Add(new HotelBooking { Id = 3, TripId = 1, RoomId = 3, CheckInDate = new DateTime(2026, 10, 7), CheckOutDate = new DateTime(2026, 10, 9), NumberOfRooms = 1, Status = BookingStatus.Confirmed });

        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task GetMyRoomsAsync_ReturnsRoomsFromAllOwnedHotels()
    {
        var db = CreateDb(nameof(GetMyRoomsAsync_ReturnsRoomsFromAllOwnedHotels));
        var service = new HotelService(db);

        // Owner 1 has rooms in Hotel A and Hotel B (2 rooms total)
        var result = await service.GetMyRoomsAsync(10, null, null, 1, 10);
        
        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, r => r.RoomType == "Standard" && r.HotelName == "Hotel A");
        Assert.Contains(result.Items, r => r.RoomType == "Deluxe" && r.HotelName == "Hotel B");
        Assert.DoesNotContain(result.Items, r => r.RoomType == "Suite"); // Owner 2's room
    }

    [Fact]
    public async Task GetMyHotelsBookingsAsync_ReturnsBookingsFromAllOwnedHotels()
    {
        var db = CreateDb(nameof(GetMyHotelsBookingsAsync_ReturnsBookingsFromAllOwnedHotels));
        var hotelService = new HotelService(db);
        var bookingService = new HotelBookingService(db, hotelService);

        // Owner 1 has 2 bookings across their 2 hotels
        var result = await bookingService.GetMyHotelsBookingsAsync(10, null, null, 1, 10);

        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, b => b.RoomType == "Standard" && b.HotelName == "Hotel A");
        Assert.Contains(result.Items, b => b.RoomType == "Deluxe" && b.HotelName == "Hotel B");
        Assert.DoesNotContain(result.Items, b => b.RoomType == "Suite"); // Owner 2's booking
    }
}
