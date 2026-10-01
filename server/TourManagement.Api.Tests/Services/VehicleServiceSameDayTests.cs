using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Transport;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Implementations;

namespace TourManagement.Api.Tests.Services;

public class VehicleServiceSameDayTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName).ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new AppDbContext(options);
        
        db.Users.Add(new User { Id = 1, FullName = "Provider", Email = "p@test.com", PasswordHash = "h", Role = "TransportProvider" });
        db.Vehicles.Add(new Vehicle
        {
            Id = 1, ProviderId = 1, VehicleType = "Van", Model = "Toyota", RegistrationNumber = "WP-1234",
            Capacity = 10, PricePerDay = 8000m, Status = VehicleStatus.Active
        });
        db.Users.Add(new User { Id = 2, FullName = "Traveler", Email = "t@test.com", PasswordHash = "h", Role = "Traveler" });
        db.Trips.Add(new Trip { Id = 1, TravelerId = 2, Title = "Test Trip", StartDate = new DateTime(2026, 9, 1), EndDate = new DateTime(2026, 10, 30), Budget = 50000, GroupSize = 4 });
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task SameDayBooking_BlocksAnotherSameDayBooking()
    {
        var db = CreateDb(nameof(SameDayBooking_BlocksAnotherSameDayBooking));
        var bookingDate = new DateTime(2026, 10, 1);
        
        // Existing same-day booking (converted to 1 day interval by our fix in the real service, so we do it here too)
        db.VehicleBookings.Add(new VehicleBooking
        {
            TripId = 1, VehicleId = 1, StartDate = bookingDate, EndDate = bookingDate.AddDays(1),
            Status = BookingStatus.Confirmed
        });
        db.SaveChanges();

        var service = new VehicleService(db);
        
        // Search for same day
        var results = await service.SearchAsync(new VehicleSearchRequestDto
        {
            StartDate = bookingDate, EndDate = bookingDate // Same day request
        });

        Assert.Empty(results);
    }

    [Fact]
    public async Task ActiveHold_BlocksAvailability()
    {
        var db = CreateDb(nameof(ActiveHold_BlocksAvailability));
        var bookingDate = new DateTime(2026, 10, 1);
        
        db.VehicleBookings.Add(new VehicleBooking
        {
            TripId = 1, VehicleId = 1, StartDate = bookingDate, EndDate = bookingDate.AddDays(1),
            Status = BookingStatus.Held, HoldExpiresAt = DateTime.UtcNow.AddHours(2)
        });
        db.SaveChanges();

        var service = new VehicleService(db);
        var results = await service.SearchAsync(new VehicleSearchRequestDto
        {
            StartDate = bookingDate, EndDate = bookingDate
        });

        Assert.Empty(results);
    }
    
    [Fact]
    public async Task ExpiredHold_DoesNotBlockAvailability()
    {
        var db = CreateDb(nameof(ExpiredHold_DoesNotBlockAvailability));
        var bookingDate = new DateTime(2026, 10, 1);
        
        db.VehicleBookings.Add(new VehicleBooking
        {
            TripId = 1, VehicleId = 1, StartDate = bookingDate, EndDate = bookingDate.AddDays(1),
            Status = BookingStatus.Held, HoldExpiresAt = DateTime.UtcNow.AddHours(-2) // Expired
        });
        db.SaveChanges();

        var service = new VehicleService(db);
        var results = await service.SearchAsync(new VehicleSearchRequestDto
        {
            StartDate = bookingDate, EndDate = bookingDate
        });

        Assert.Single(results); // Should not block
    }

    [Fact]
    public async Task MultiDayBooking_BlocksSameDayRequest()
    {
        var db = CreateDb(nameof(MultiDayBooking_BlocksSameDayRequest));
        
        db.VehicleBookings.Add(new VehicleBooking
        {
            TripId = 1, VehicleId = 1, StartDate = new DateTime(2026, 10, 1), EndDate = new DateTime(2026, 10, 5),
            Status = BookingStatus.Confirmed
        });
        db.SaveChanges();

        var service = new VehicleService(db);
        var results = await service.SearchAsync(new VehicleSearchRequestDto
        {
            StartDate = new DateTime(2026, 10, 3), EndDate = new DateTime(2026, 10, 3)
        });

        Assert.Empty(results); // Oct 3 is within Oct 1-5
    }

    [Fact]
    public async Task SameDayBooking_BlocksMultiDayRequest()
    {
        var db = CreateDb(nameof(SameDayBooking_BlocksMultiDayRequest));
        
        db.VehicleBookings.Add(new VehicleBooking
        {
            TripId = 1, VehicleId = 1, StartDate = new DateTime(2026, 10, 3), EndDate = new DateTime(2026, 10, 4),
            Status = BookingStatus.Confirmed
        });
        db.SaveChanges();

        var service = new VehicleService(db);
        var results = await service.SearchAsync(new VehicleSearchRequestDto
        {
            StartDate = new DateTime(2026, 10, 1), EndDate = new DateTime(2026, 10, 5)
        });

        Assert.Empty(results); // Oct 1-5 overlaps with Oct 3 same-day (which occupies Oct 3-4)
    }

    [Fact]
    public async Task CancelledBooking_DoesNotBlockAvailability()
    {
        var db = CreateDb(nameof(CancelledBooking_DoesNotBlockAvailability));
        
        db.VehicleBookings.Add(new VehicleBooking
        {
            TripId = 1, VehicleId = 1, StartDate = new DateTime(2026, 10, 3), EndDate = new DateTime(2026, 10, 4),
            Status = BookingStatus.Cancelled
        });
        db.SaveChanges();

        var service = new VehicleService(db);
        var results = await service.SearchAsync(new VehicleSearchRequestDto
        {
            StartDate = new DateTime(2026, 10, 3), EndDate = new DateTime(2026, 10, 3)
        });

        Assert.Single(results);
    }

    [Fact]
    public async Task NonOverlappingRentals_BoundaryBehavior_RemainsCorrect()
    {
        var db = CreateDb(nameof(NonOverlappingRentals_BoundaryBehavior_RemainsCorrect));
        
        db.VehicleBookings.Add(new VehicleBooking
        {
            TripId = 1, VehicleId = 1, StartDate = new DateTime(2026, 10, 1), EndDate = new DateTime(2026, 10, 3),
            Status = BookingStatus.Confirmed
        });
        db.SaveChanges();

        var service = new VehicleService(db);
        
        // Search starting exactly when the previous one ends (Oct 3)
        var results = await service.SearchAsync(new VehicleSearchRequestDto
        {
            StartDate = new DateTime(2026, 10, 3), EndDate = new DateTime(2026, 10, 5)
        });

        Assert.Single(results); // Exclusive boundary means they do not overlap
    }

    [Fact]
    public async Task EndDateBeforeStartDate_IsRejected()
    {
        var db = CreateDb(nameof(EndDateBeforeStartDate_IsRejected));
        var service = new VehicleService(db);
        
        await Assert.ThrowsAsync<TourManagement.Api.Common.Exceptions.ValidationException>(() => 
            service.SearchAsync(new VehicleSearchRequestDto
            {
                StartDate = new DateTime(2026, 10, 5), EndDate = new DateTime(2026, 10, 3)
            })
        );
    }
}

