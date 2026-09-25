using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Transport;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Implementations;

namespace TourManagement.Api.Tests.Services;

// Tests for the two key business rules in the transport component:
//  1. Search correctly excludes an already-booked vehicle for overlapping dates.
//  2. A booking attempt on an already-booked vehicle is rejected.
//  3. Non-overlapping bookings do NOT block a vehicle (boundary check).
//  4. DTO-level lat/lng [Range] validation rejects out-of-range coordinates.
public class VehicleServiceTests
{
    // Creates an in-memory database seeded with a provider, an active vehicle, and a traveler+trip.
    // Each test gets its own DB name so tests don't share state.
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var db = new AppDbContext(options);

        // Seed a TransportProvider user.
        db.Users.Add(new User
        {
            Id           = 1,
            FullName     = "Provider",
            Email        = "provider@test.com",
            PasswordHash = "hash",
            Role         = "TransportProvider"
        });

        // Seed an active vehicle.
        db.Vehicles.Add(new Vehicle
        {
            Id                 = 1,
            ProviderId         = 1,
            VehicleType        = "Van",
            Model              = "Toyota KDH",
            RegistrationNumber = "WP-AB-1234",
            Capacity           = 10,
            PricePerDay        = 8000m,
            Status             = VehicleStatus.Active
        });

        // Seed a traveler and a trip to attach bookings to.
        db.Users.Add(new User
        {
            Id           = 2,
            FullName     = "Traveler",
            Email        = "traveler@test.com",
            PasswordHash = "hash",
            Role         = "Traveler"
        });
        db.Trips.Add(new Trip
        {
            Id         = 1,
            TravelerId = 2,
            Title      = "Test Trip",
            // Fixed date range covering all test booking windows (Sep 25 – Oct 10).
            StartDate  = new DateTime(2026, 9, 24),
            EndDate    = new DateTime(2026, 10, 10),
            Budget     = 50000,
            GroupSize  = 4
        });

        db.SaveChanges();
        return db;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Test 1: Search excludes a vehicle that is booked for the same dates
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Search_ExcludesAlreadyBookedVehicle()
    {
        // Arrange: book the vehicle for Oct 1–5.
        var db = CreateDb(nameof(Search_ExcludesAlreadyBookedVehicle));

        db.VehicleBookings.Add(new VehicleBooking
        {
            TripId          = 1,
            VehicleId       = 1,
            StartDate       = new DateTime(2026, 10, 1),
            EndDate         = new DateTime(2026, 10, 5),
            PickupLatitude  = 6.9m,
            PickupLongitude = 79.8m,
            Status          = BookingStatus.Confirmed
        });
        db.SaveChanges();

        var service = new VehicleService(db);

        // Act: search for the exact same date range.
        var results = await service.SearchAsync(new VehicleSearchRequestDto
        {
            StartDate = new DateTime(2026, 10, 1),
            EndDate   = new DateTime(2026, 10, 5)
        });

        // Assert: the booked vehicle should not appear.
        Assert.Empty(results);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Test 2: Search includes a vehicle when the existing booking doesn't overlap
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Search_IncludesVehicleWhenBookingEndsBeforeSearchStarts()
    {
        // Arrange: book the vehicle for Sep 25–30 (ends before our search window).
        var db = CreateDb(nameof(Search_IncludesVehicleWhenBookingEndsBeforeSearchStarts));

        db.VehicleBookings.Add(new VehicleBooking
        {
            TripId          = 1,
            VehicleId       = 1,
            StartDate       = new DateTime(2026, 9, 25),
            EndDate         = new DateTime(2026, 9, 30),
            PickupLatitude  = 6.9m,
            PickupLongitude = 79.8m,
            Status          = BookingStatus.Confirmed
        });
        db.SaveChanges();

        var service = new VehicleService(db);

        // Act: search for Oct 1–5 — no overlap with the Sep 25–30 booking.
        var results = await service.SearchAsync(new VehicleSearchRequestDto
        {
            StartDate = new DateTime(2026, 10, 1),
            EndDate   = new DateTime(2026, 10, 5)
        });

        // Assert: the vehicle IS available, so it should appear in results.
        Assert.Single(results);
        Assert.Equal(1, results[0].VehicleId);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Test 3: Booking is rejected when the vehicle is already booked (overlapping)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBooking_RejectsWhenVehicleAlreadyBooked()
    {
        // Arrange: vehicle is booked Held for Oct 1–5.
        var db = CreateDb(nameof(CreateBooking_RejectsWhenVehicleAlreadyBooked));

        db.VehicleBookings.Add(new VehicleBooking
        {
            TripId          = 1,
            VehicleId       = 1,
            StartDate       = new DateTime(2026, 10, 1),
            EndDate         = new DateTime(2026, 10, 5),
            PickupLatitude  = 6.9m,
            PickupLongitude = 79.8m,
            Status          = BookingStatus.Held
        });
        db.SaveChanges();

        var vehicleService  = new VehicleService(db);
        var bookingService  = new VehicleBookingService(db, vehicleService);

        // Act: traveler tries to book the same vehicle for an overlapping window (Oct 3–7).
        var dto = new CreateVehicleBookingDto
        {
            TripId          = 1,
            VehicleId       = 1,
            StartDate       = new DateTime(2026, 10, 3),
            EndDate         = new DateTime(2026, 10, 7),
            PickupLatitude  = 6.9m,
            PickupLongitude = 79.8m
        };

        // Assert: ValidationException is thrown with a clear message.
        var ex = await Assert.ThrowsAsync<TourManagement.Api.Common.Exceptions.ValidationException>(
            () => bookingService.CreateAsync(dto, travelerId: 2));

        Assert.Contains("already booked", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Test 4: DTO [Range] validation rejects invalid lat/lng values
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CreateVehicleBookingDto_RejectsInvalidLatLng()
    {
        // A latitude of 200 is clearly out of the valid -90 to 90 range.
        var dto = new CreateVehicleBookingDto
        {
            TripId          = 1,
            VehicleId       = 1,
            StartDate       = DateTime.Today,
            EndDate         = DateTime.Today.AddDays(3),
            PickupLatitude  = 200m,   // invalid — must be between -90 and 90
            PickupLongitude = 79.8m
        };

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(dto, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(dto.PickupLatitude)));
    }

    // ────────────────────────────────────────────────────────────────────────
    // Test 5: Booking dates outside the trip's date range are rejected
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateVehicleBooking_RejectsDatesOutsideTrip()
    {
        // Arrange: trip runs Oct 1–5; booking attempts to end on Oct 8 (outside trip).
        var db = CreateDb(nameof(CreateVehicleBooking_RejectsDatesOutsideTrip));

        // Override the trip's EndDate so it's narrower than the booking window.
        var trip = await db.Trips.FindAsync(1);
        trip!.StartDate = new DateTime(2026, 10, 1);
        trip!.EndDate   = new DateTime(2026, 10, 5);
        await db.SaveChangesAsync();

        var vehicleService = new VehicleService(db);
        var bookingService = new VehicleBookingService(db, vehicleService);

        // EndDate (Oct 8) is after Trip.EndDate (Oct 5) — must be rejected.
        var dto = new CreateVehicleBookingDto
        {
            TripId          = 1,
            VehicleId       = 1,
            StartDate       = new DateTime(2026, 10, 2),
            EndDate         = new DateTime(2026, 10, 8),   // outside trip range
            PickupLatitude  = 6.9m,
            PickupLongitude = 79.8m
        };

        var ex = await Assert.ThrowsAsync<TourManagement.Api.Common.Exceptions.ValidationException>(
            () => bookingService.CreateAsync(dto, travelerId: 2));

        Assert.Contains("trip's date range", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Test 6: My Vehicles Bookings shows bookings for all owned vehicles
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyVehiclesBookings_ShowsBookingsForProvidersVehiclesOnly()
    {
        var db = CreateDb(nameof(GetMyVehiclesBookings_ShowsBookingsForProvidersVehiclesOnly));
        
        // Add another vehicle for provider 1
        db.Vehicles.Add(new Vehicle
        {
            Id                 = 2,
            ProviderId         = 1,
            VehicleType        = "Car",
            Model              = "Prius",
            RegistrationNumber = "WP-CA-1234",
            Capacity           = 4,
            PricePerDay        = 4000m,
            Status             = VehicleStatus.Active
        });

        // Add a vehicle for another provider (provider 3)
        db.Users.Add(new User { Id = 3, FullName = "Other", Email = "other@test.com", PasswordHash = "hash", Role = "TransportProvider" });
        db.Vehicles.Add(new Vehicle
        {
            Id                 = 3,
            ProviderId         = 3,
            VehicleType        = "Bus",
            Model              = "Rosa",
            RegistrationNumber = "WP-BU-1234",
            Capacity           = 30,
            PricePerDay        = 15000m,
            Status             = VehicleStatus.Active
        });

        // Add bookings for all 3 vehicles (we must use different IDs and assign to trip)
        // Wait, VehicleBooking Summary Dto requires `Vehicle` to be populated, `trip` doesn't strictly matter.
        db.VehicleBookings.Add(new VehicleBooking { TripId = 1, VehicleId = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(2), Status = BookingStatus.Confirmed });
        db.VehicleBookings.Add(new VehicleBooking { TripId = 1, VehicleId = 2, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(2), Status = BookingStatus.Confirmed });
        db.VehicleBookings.Add(new VehicleBooking { TripId = 1, VehicleId = 3, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(2), Status = BookingStatus.Confirmed });
        await db.SaveChangesAsync();

        var vehicleService = new VehicleService(db);
        var bookingService = new VehicleBookingService(db, vehicleService);

        // Act: provider 1 asks for their vehicles' bookings
        var result = await bookingService.GetMyVehiclesBookingsAsync(1, null, null, 1, 10);

        // Assert: they should see 2 bookings (for vehicle 1 and 2), not the booking for vehicle 3
        Assert.Equal(2, result.TotalCount);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Test 7: IsBookedToday indicator computes correctly
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyVehicles_ComputesIsBookedTodayCorrectly()
    {
        var db = CreateDb(nameof(GetMyVehicles_ComputesIsBookedTodayCorrectly));
        
        // Add another vehicle for provider 1
        db.Vehicles.Add(new Vehicle
        {
            Id                 = 2,
            ProviderId         = 1,
            VehicleType        = "Car",
            Model              = "Prius",
            RegistrationNumber = "WP-CA-1234",
            Capacity           = 4,
            PricePerDay        = 4000m,
            Status             = VehicleStatus.Active
        });

        // Vehicle 1 has a booking covering today.
        db.VehicleBookings.Add(new VehicleBooking { TripId = 1, VehicleId = 1, StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1), Status = BookingStatus.Confirmed });
        
        // Vehicle 2 has a booking in the future.
        db.VehicleBookings.Add(new VehicleBooking { TripId = 1, VehicleId = 2, StartDate = DateTime.UtcNow.AddDays(10), EndDate = DateTime.UtcNow.AddDays(12), Status = BookingStatus.Confirmed });
        
        await db.SaveChangesAsync();

        var vehicleService = new VehicleService(db);

        // Act
        var result = await vehicleService.GetMyVehiclesAsync(1, null, null, null, 1, 10);

        // Assert
        var vehicle1 = result.Items.First(v => v.Id == 1);
        var vehicle2 = result.Items.First(v => v.Id == 2);

        Assert.True(vehicle1.IsBookedToday, "Vehicle 1 should be booked today.");
        Assert.False(vehicle2.IsBookedToday, "Vehicle 2 should NOT be booked today.");
    }
}

