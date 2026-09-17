using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Configurations;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Auth;
using TourManagement.Api.Dtos.Trips;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Implementations;

using ValidationException = TourManagement.Api.Common.Exceptions.ValidationException;

namespace TourManagement.Api.Tests.Services;

// Unit tests for the three key business rules from the specification.
// We use EF Core's in-memory database so we don't need a real PostgreSQL connection.
public class BusinessRuleTests
{
    // Creates a fresh in-memory database for each test so tests don't share state.
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    // AuthService needs JwtSettings to generate tokens — we use dummy values for tests.
    private static JwtSettings FakeJwtSettings() => new JwtSettings
    {
        Issuer       = "TestIssuer",
        Audience     = "TestAudience",
        SecretKey    = "this-is-a-test-secret-key-32-chars!!",
        ExpiresInHours = 2
    };

    // ── Test 1: Login with PendingApproval status ────────────────────────────

    [Fact]
    public async Task Login_WithPendingApprovalStatus_ThrowsValidationExceptionWithExpectedMessage()
    {
        // Arrange: seed a user whose account hasn't been approved yet.
        await using var db = CreateDb(nameof(Login_WithPendingApprovalStatus_ThrowsValidationExceptionWithExpectedMessage));

        db.Users.Add(new User
        {
            FullName     = "Test Provider",
            Email        = "provider@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role         = "HotelOwner",
            Status       = UserStatus.PendingApproval
        });
        await db.SaveChangesAsync();

        var authService = new AuthService(db, FakeJwtSettings());

        // Act & Assert: login should throw a ValidationException with the approval message.
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            authService.LoginAsync(new LoginRequestDto
            {
                Email    = "provider@test.com",
                Password = "password123"
            }));

        Assert.Contains("awaiting admin approval", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── Test 2: Update trip when no longer Draft ─────────────────────────────

    [Fact]
    public async Task UpdateTrip_WhenNotDraft_ThrowsValidationException()
    {
        // Arrange: seed a trip that is already Confirmed (not editable).
        await using var db = CreateDb(nameof(UpdateTrip_WhenNotDraft_ThrowsValidationException));

        var traveler = new User
        {
            FullName = "Test Traveler", Email = "traveler@test.com",
            PasswordHash = "hash", Role = "Traveler", Status = UserStatus.Active
        };
        db.Users.Add(traveler);
        await db.SaveChangesAsync();

        var trip = new Trip
        {
            TravelerId = traveler.Id,
            Title      = "My Trip",
            StartDate  = DateTime.UtcNow.AddDays(10),
            EndDate    = DateTime.UtcNow.AddDays(15),
            Budget     = 1000,
            GroupSize  = 2,
            // Confirmed trips cannot be edited — only Draft trips can.
            Status     = TripStatus.Confirmed
        };
        db.Trips.Add(trip);
        await db.SaveChangesAsync();

        var tripService = new TripService(db);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            tripService.UpdateAsync(trip.Id, new UpdateTripDto
            {
                Title     = "Updated Title",
                StartDate = trip.StartDate,
                EndDate   = trip.EndDate,
                Budget    = 1500,
                GroupSize = 3
            }, traveler.Id));

        Assert.Contains("Draft", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── Test 3: EndDate before StartDate is rejected ─────────────────────────

    [Fact]
    public async Task CreateTrip_WhenEndDateBeforeStartDate_ThrowsValidationException()
    {
        // Arrange
        await using var db = CreateDb(nameof(CreateTrip_WhenEndDateBeforeStartDate_ThrowsValidationException));

        var traveler = new User
        {
            FullName = "Test Traveler", Email = "traveler2@test.com",
            PasswordHash = "hash", Role = "Traveler", Status = UserStatus.Active
        };
        db.Users.Add(traveler);
        await db.SaveChangesAsync();

        var tripService = new TripService(db);

        // Act & Assert: EndDate is before StartDate — must be rejected.
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            tripService.CreateAsync(new CreateTripDto
            {
                Title     = "Bad Trip",
                StartDate = DateTime.UtcNow.AddDays(10),
                EndDate   = DateTime.UtcNow.AddDays(5),  // EndDate < StartDate
                Budget    = 500,
                GroupSize = 1
            }, traveler.Id));

        Assert.Contains("EndDate", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
