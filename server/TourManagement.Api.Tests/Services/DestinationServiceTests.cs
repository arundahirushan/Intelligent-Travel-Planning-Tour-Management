using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Destination;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Implementations;

using ValidationException = TourManagement.Api.Common.Exceptions.ValidationException;

namespace TourManagement.Api.Tests.Services;

// Tests for the destination deletion guard in DestinationService.
// Verifies that the service blocks deletion when hotels or itinerary items reference
// the destination, and allows deletion when no references exist.
public class DestinationServiceTests
{
    // Creates an in-memory database. Each test receives a unique name to avoid interference.
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 1: Deletion succeeds when the destination has no references
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_Succeeds_WhenNoReferences()
    {
        // Arrange
        var db = CreateDb(nameof(DeleteAsync_Succeeds_WhenNoReferences));
        var dest = new Destination { Id = 1, Name = "Unida", Region = "East", Description = "No bookings." };
        db.Destinations.Add(dest);
        await db.SaveChangesAsync();

        var service = new DestinationService(db);

        // Act — should not throw
        await service.DeleteAsync(1);

        // Assert — destination is gone
        var remaining = await db.Destinations.FindAsync(1);
        Assert.Null(remaining);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 2: Deletion is blocked when a hotel references the destination
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_Blocked_WhenHotelReferences()
    {
        // Arrange
        var db = CreateDb(nameof(DeleteAsync_Blocked_WhenHotelReferences));

        var dest = new Destination { Id = 1, Name = "Galle", Region = "Kandy", Description = "Fort city." };
        db.Destinations.Add(dest);

        var owner = new User
        {
            Id = 10, FullName = "Hotel Owner", Email = "owner@test.com",
            PasswordHash = "hash", Role = "HotelOwner"
        };
        db.Users.Add(owner);

        var hotel = new Hotel
        {
            Id = 1, OwnerId = 10, DestinationId = 1,
            Name = "Sea View Hotel", Address = "Fort Road",
            Description = "A nice hotel.", ContactPhone = "0771234567",
            Status = HotelStatus.Active
        };
        db.Hotels.Add(hotel);
        await db.SaveChangesAsync();

        var service = new DestinationService(db);

        // Act + Assert — expects a ValidationException with the hotel message
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.DeleteAsync(1));

        Assert.Contains("hotels are registered at it", ex.Message);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 3: Deletion is blocked when an itinerary item references the destination
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_Blocked_WhenItineraryItemReferences()
    {
        // Arrange
        var db = CreateDb(nameof(DeleteAsync_Blocked_WhenItineraryItemReferences));

        var dest = new Destination { Id = 1, Name = "Sigiriya", Region = "Central", Description = "Rock fortress." };
        db.Destinations.Add(dest);

        var traveler = new User
        {
            Id = 20, FullName = "Alice", Email = "alice@test.com",
            PasswordHash = "hash", Role = "Traveler"
        };
        db.Users.Add(traveler);

        var trip = new Trip
        {
            Id = 1, TravelerId = 20, Title = "Sri Lanka Tour",
            StartDate = new DateTime(2026, 10, 1),
            EndDate = new DateTime(2026, 10, 10),
            Budget = 50000, GroupSize = 2
        };
        db.Trips.Add(trip);

        var item = new ItineraryItem
        {
            Id = 1, TripId = 1, DestinationId = 1,
            DayNumber = 1, SequenceOrder = 1
        };
        db.ItineraryItems.Add(item);
        await db.SaveChangesAsync();

        var service = new DestinationService(db);

        // Act + Assert — expects a ValidationException with the itinerary message
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.DeleteAsync(1));

        Assert.Contains("itinerary items", ex.Message);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 4: Hotel check takes priority over itinerary check
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ReportsHotelFirst_WhenBothHotelAndItineraryExist()
    {
        // Arrange: destination is referenced by both a hotel and an itinerary item.
        var db = CreateDb(nameof(DeleteAsync_ReportsHotelFirst_WhenBothHotelAndItineraryExist));

        var dest = new Destination { Id = 1, Name = "Kandy", Region = "Central", Description = "Cultural capital." };
        db.Destinations.Add(dest);

        var owner = new User
        {
            Id = 10, FullName = "Owner", Email = "o@test.com",
            PasswordHash = "hash", Role = "HotelOwner"
        };
        db.Users.Add(owner);

        var hotel = new Hotel
        {
            Id = 1, OwnerId = 10, DestinationId = 1,
            Name = "Temple Hotel", Address = "Kandy Lake Road",
            Description = "Nice.", ContactPhone = "0771234567",
            Status = HotelStatus.Active
        };
        db.Hotels.Add(hotel);

        var traveler = new User
        {
            Id = 20, FullName = "Bob", Email = "bob@test.com",
            PasswordHash = "hash", Role = "Traveler"
        };
        db.Users.Add(traveler);

        var trip = new Trip
        {
            Id = 1, TravelerId = 20, Title = "Trip",
            StartDate = new DateTime(2026, 10, 1),
            EndDate = new DateTime(2026, 10, 10),
            Budget = 20000, GroupSize = 1
        };
        db.Trips.Add(trip);

        var item = new ItineraryItem { Id = 1, TripId = 1, DestinationId = 1, DayNumber = 1, SequenceOrder = 1 };
        db.ItineraryItems.Add(item);
        await db.SaveChangesAsync();

        var service = new DestinationService(db);

        // Act + Assert — hotel guard fires first
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.DeleteAsync(1));

        Assert.Contains("hotels are registered at it", ex.Message);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 5: GetAllAsync returns paginated destinations filtered by name/region
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsFilteredAndPaginatedResults()
    {
        var db = CreateDb(nameof(GetAllAsync_ReturnsFilteredAndPaginatedResults));

        db.Destinations.AddRange(
            new Destination { Id = 1, Name = "Galle", Region = "Kandy", Description = "Fort." },
            new Destination { Id = 2, Name = "Ella", Region = "Uva Province", Description = "Mountain." },
            new Destination { Id = 3, Name = "Colombo", Region = "Western Province", Description = "City." }
        );
        await db.SaveChangesAsync();

        var service = new DestinationService(db);

        // Search for "Galle" by name — should return only id=1
        var result = await service.GetAllAsync(search: "Galle", sort: null, page: 1, pageSize: 20);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Galle", result.Items[0].Name);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 6: CreateAsync persists and returns the new destination
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PersistsAndReturnsDestination()
    {
        var db = CreateDb(nameof(CreateAsync_PersistsAndReturnsDestination));
        var service = new DestinationService(db);

        var dto = new CreateDestinationDto
        {
            Name = "Mirissa",
            Region = "Kandy",
            Description = "Beach town.",
            ImageUrl = "https://example.com/mirissa.jpg"
        };

        var result = await service.CreateAsync(dto);

        Assert.Equal("Mirissa", result.Name);
        Assert.Equal("Kandy", result.Region);
        Assert.Equal("https://example.com/mirissa.jpg", result.ImageUrl);
        Assert.True(result.Id > 0);
    }
}

