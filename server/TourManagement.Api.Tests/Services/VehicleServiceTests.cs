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
            StartDate  = DateTime.Today,
            EndDate    = DateTime.Today.AddDays(10),
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
            Status          = VehicleBookingStatus.Confirmed
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
            Status          = VehicleBookingStatus.Confirmed
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
            Status          = VehicleBookingStatus.Held
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
}

