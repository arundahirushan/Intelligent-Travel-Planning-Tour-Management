using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Accommodation;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Implementations;

using ValidationException = TourManagement.Api.Common.Exceptions.ValidationException;

namespace TourManagement.Api.Tests.Services;

// Tests for the key business rules in the accommodation component:
//  1. Search correctly excludes a fully-booked room.
//  2. Creating a booking that would exceed TotalRooms capacity is rejected.
//  3. Partial availability — overlapping booking leaves 1 room free.
//  4. Booking dates outside the trip's date range are rejected.
public class HotelServiceTests
{
    // Creates an in-memory database seeded with a destination, hotel, and one room type.
    // Each test gets a fresh DB so they don't interfere with each other.
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var db = new AppDbContext(options);

        // Seed a destination.
        var destination = new Destination { Id = 1, Name = "Galle", Region = "Southern Province", Description = "Historic fort city." };
        db.Destinations.Add(destination);

        // Seed a HotelOwner user.
        var owner = new User { Id = 10, FullName = "Hotel Owner", Email = "owner@test.com", PasswordHash = "hash", Role = "HotelOwner" };
        db.Users.Add(owner);

        // Seed an active hotel.
        var hotel = new Hotel
        {
            Id            = 1,
            OwnerId       = 10,
            DestinationId = 1,
            Name          = "Sea View Hotel",
            Address       = "Galle Fort",
            Description   = "Nice view.",
            ContactPhone  = "0771234567",
            Status        = HotelStatus.Active
        };
        db.Hotels.Add(hotel);

        // Seed a room type with TotalRooms = 2.
        var room = new Room
        {
            Id            = 1,
            HotelId       = 1,
            RoomType      = "Standard Double",
            PricePerNight = 5000m,
            Capacity      = 2,
            TotalRooms    = 2,   // only 2 physical rooms of this type
            Status        = RoomStatus.Active
        };
        db.Rooms.Add(room);

        db.SaveChanges();
        return db;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Test 1: Search excludes a fully-booked room
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Search_ExcludesFullyBookedRoom()
    {
        // Arrange: both rooms are already booked for the search dates.
        var db = CreateDb(nameof(Search_ExcludesFullyBookedRoom));

        // Seed a traveler and a trip to attach bookings to.
        // Trip spans Sep 30 – Oct 10 so it encompasses the Oct 1–5 booking dates.
        db.Users.Add(new User { Id = 20, FullName = "Traveler", Email = "t@t.com", PasswordHash = "h", Role = "Traveler" });
        db.Trips.Add(new Trip { Id = 1, TravelerId = 20, Title = "Trip", StartDate = new DateTime(2026, 9, 30), EndDate = new DateTime(2026, 10, 10), Budget = 10000, GroupSize = 2 });
        db.SaveChanges();

        // Book all 2 rooms for 01 Oct – 05 Oct.
        db.HotelBookings.Add(new HotelBooking
        {
            TripId        = 1,
            RoomId        = 1,
            CheckInDate   = new DateTime(2026, 10, 1),
            CheckOutDate  = new DateTime(2026, 10, 5),
            NumberOfRooms = 2,   // all rooms taken
            Status        = BookingStatus.Confirmed
        });
        db.SaveChanges();

        var service = new HotelService(db);

        // Act: search for the same date range.
        var results = await service.SearchAsync(new HotelSearchRequestDto
        {
            DestinationId  = 1,
            CheckInDate    = new DateTime(2026, 10, 1),
            CheckOutDate   = new DateTime(2026, 10, 5),
            NumberOfGuests = 1
        });

        // Assert: the fully-booked room should not appear.
        Assert.Empty(results);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Test 2: Booking is rejected when it would exceed TotalRooms capacity
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBooking_RejectsWhenExceedsCapacity()
    {
        // Arrange: 1 of the 2 rooms is already booked for the target dates.
        var db = CreateDb(nameof(CreateBooking_RejectsWhenExceedsCapacity));

        // Trip spans Sep 30 – Oct 10 so it encompasses the Oct 1–5 booking dates.
        db.Users.Add(new User { Id = 20, FullName = "Traveler", Email = "t@t.com", PasswordHash = "h", Role = "Traveler" });
        db.Trips.Add(new Trip { Id = 1, TravelerId = 20, Title = "Trip", StartDate = new DateTime(2026, 9, 30), EndDate = new DateTime(2026, 10, 10), Budget = 10000, GroupSize = 2 });
        db.SaveChanges();

        // 1 room already booked → only 1 left.
        db.HotelBookings.Add(new HotelBooking
        {
            TripId        = 1,
            RoomId        = 1,
            CheckInDate   = new DateTime(2026, 10, 1),
            CheckOutDate  = new DateTime(2026, 10, 5),
            NumberOfRooms = 1,
            Status        = BookingStatus.Held
        });
        db.SaveChanges();

        var hotelService   = new HotelService(db);
        var bookingService = new HotelBookingService(db, hotelService);

        // Act: traveler tries to book 2 rooms — but only 1 is available.
        var dto = new CreateHotelBookingDto
        {
            TripId        = 1,
            RoomId        = 1,
            CheckInDate   = new DateTime(2026, 10, 1),
            CheckOutDate  = new DateTime(2026, 10, 5),
            NumberOfRooms = 2   // too many — only 1 available
        };

        // Assert: ValidationException is thrown with a clear message.
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => bookingService.CreateAsync(dto, travelerId: 20));

        Assert.Contains("Not enough rooms available", ex.Message);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Test 3: Partial availability — overlapping booking leaves 1 room free
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Search_ShowsPartiallyAvailableRoom()
    {
        // Arrange: 1 of 2 rooms already booked → search should show AvailableRoomCount = 1.
        var db = CreateDb(nameof(Search_ShowsPartiallyAvailableRoom));

        // Trip spans Sep 30 – Oct 10 so it encompasses the Oct 1–5 booking dates.
        db.Users.Add(new User { Id = 20, FullName = "Traveler", Email = "t@t.com", PasswordHash = "h", Role = "Traveler" });
        db.Trips.Add(new Trip { Id = 1, TravelerId = 20, Title = "Trip", StartDate = new DateTime(2026, 9, 30), EndDate = new DateTime(2026, 10, 10), Budget = 10000, GroupSize = 2 });
        db.SaveChanges();

        db.HotelBookings.Add(new HotelBooking
        {
            TripId        = 1,
            RoomId        = 1,
            CheckInDate   = new DateTime(2026, 10, 1),
            CheckOutDate  = new DateTime(2026, 10, 5),
            NumberOfRooms = 1,   // only 1 of the 2 rooms taken
            Status        = BookingStatus.Confirmed
        });
        db.SaveChanges();

        var service = new HotelService(db);

        // Act.
        var results = await service.SearchAsync(new HotelSearchRequestDto
        {
            DestinationId  = 1,
            CheckInDate    = new DateTime(2026, 10, 1),
            CheckOutDate   = new DateTime(2026, 10, 5),
            NumberOfGuests = 1
        });

        // Assert: room is still listed but with AvailableRoomCount = 1.
        Assert.Single(results);
        Assert.Equal(1, results[0].AvailableRoomCount);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Test 4: Booking dates outside the trip's date range are rejected
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateHotelBooking_RejectsDatesOutsideTrip()
    {
        // Arrange: trip runs Oct 1 – Oct 5; booking attempts to check out on Oct 7 (outside trip).
        var db = CreateDb(nameof(CreateHotelBooking_RejectsDatesOutsideTrip));

        db.Users.Add(new User { Id = 20, FullName = "Traveler", Email = "t@t.com", PasswordHash = "h", Role = "Traveler" });
        db.Trips.Add(new Trip { Id = 1, TravelerId = 20, Title = "Trip",
            StartDate = new DateTime(2026, 10, 1), EndDate = new DateTime(2026, 10, 5),
            Budget = 10000, GroupSize = 2 });
        db.SaveChanges();

        var hotelService   = new HotelService(db);
        var bookingService = new HotelBookingService(db, hotelService);

        // CheckOutDate (Oct 7) is after Trip.EndDate (Oct 5) — must be rejected.
        var dto = new CreateHotelBookingDto
        {
            TripId        = 1,
            RoomId        = 1,
            CheckInDate   = new DateTime(2026, 10, 2),
            CheckOutDate  = new DateTime(2026, 10, 7),   // outside trip range
            NumberOfRooms = 1
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => bookingService.CreateAsync(dto, travelerId: 20));

        Assert.Contains("trip's date range", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
