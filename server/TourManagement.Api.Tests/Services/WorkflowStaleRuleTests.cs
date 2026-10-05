using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using TourManagement.Api.AgentIntegration;
using TourManagement.Api.Common.Exceptions;
using TourManagement.Api.Data;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Implementations;
using ValidationException = TourManagement.Api.Common.Exceptions.ValidationException;

namespace TourManagement.Api.Tests.Services;

/// <summary>
/// A DbContext that rejects the first save containing new execution summaries, like Postgres does when a
/// jsonb column is given text that is not valid JSON.
/// </summary>
public class SaveFailsOnceDbContext : AppDbContext
{
    public bool FailNextSaveWithSummaries { get; set; }

    public SaveFailsOnceDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        if (FailNextSaveWithSummaries &&
            ChangeTracker.Entries<ExecutionSummary>().Any(e => e.State == EntityState.Added))
        {
            FailNextSaveWithSummaries = false;
            throw new DbUpdateException("Simulated: invalid input syntax for type json");
        }
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}

/// <summary>
/// Lifecycle rules for a proposal that is (or was) Generating: when it may be declared stale and
/// that a failed result save still ends in a terminal status.
/// </summary>
public class WorkflowStaleRuleTests : IDisposable
{
    private readonly SaveFailsOnceDbContext _db;
    private readonly FakeAgentClient _agentClient = new();
    private readonly WorkflowService _service;

    public WorkflowStaleRuleTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new SaveFailsOnceDbContext(options);

        var hotelService = new HotelService(_db);
        var vehicleService = new VehicleService(_db);
        var checkoutService = new CheckoutService(_db, hotelService, vehicleService, null!, new TourManagement.Api.Configurations.PayHereSettings());
        _service = new WorkflowService(_db, _agentClient, checkoutService, NullLogger<WorkflowService>.Instance);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private async Task<Trip> AddTripAsync()
    {
        var trip = new Trip
        {
            TravelerId = 1,
            Title = "Test Trip",
            StartDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc),
            Budget = 5000,
            GroupSize = 2,
            Status = TripStatus.Draft
        };
        _db.Trips.Add(trip);
        await _db.SaveChangesAsync();
        return trip;
    }

    private async Task<TripProposal> AddGeneratingProposalAsync(int tripId, TimeSpan age)
    {
        var createdAt = DateTime.UtcNow - age;
        var proposal = new TripProposal
        {
            ProposalId = Guid.NewGuid().ToString(),
            TripId = tripId,
            Version = 1,
            RequestId = Guid.NewGuid().ToString(),
            InputSnapshot = "{}",
            Payload = "{}",
            Status = ProposalStatus.Generating,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
        _db.TripProposals.Add(proposal);
        await _db.SaveChangesAsync();
        return proposal;
    }

    [Fact]
    public void StaleLimit_IsLongerThanTheAgentRequestTimeout()
    {
        // The stale rule must never fire while the API may still be waiting on the agents.
        Assert.True(AgentTimeouts.GeneratingStaleAfter > AgentTimeouts.AgentRequest);
    }

    [Theory]
    [InlineData(2.5)]  // just past the old 2-minute limit
    [InlineData(5.5)]  // past the agent request timeout, still inside the save grace period
    public async Task GetLatest_GeneratingWithinMaxRuntime_StaysGenerating(double ageMinutes)
    {
        var trip = await AddTripAsync();
        var proposal = await AddGeneratingProposalAsync(trip.Id, TimeSpan.FromMinutes(ageMinutes));
        var updatedBefore = proposal.UpdatedAt;

        var dto = await _service.GetLatestProposalAsync(trip.Id, travelerId: 1);

        Assert.Equal("Generating", dto.Status);
        var saved = await _db.TripProposals.AsNoTracking().SingleAsync(p => p.Id == proposal.Id);
        Assert.Equal(ProposalStatus.Generating, saved.Status);
        Assert.Equal(updatedBefore, saved.UpdatedAt);
        Assert.Null(saved.FailureReason);
    }

    [Fact]
    public async Task GetLatest_AbandonedGenerating_BecomesGenerationFailedWithReason()
    {
        var trip = await AddTripAsync();
        var proposal = await AddGeneratingProposalAsync(trip.Id, AgentTimeouts.GeneratingStaleAfter + TimeSpan.FromMinutes(1));

        var dto = await _service.GetLatestProposalAsync(trip.Id, travelerId: 1);

        Assert.Equal("GenerationFailed", dto.Status);
        var saved = await _db.TripProposals.AsNoTracking().SingleAsync(p => p.Id == proposal.Id);
        Assert.Equal(ProposalStatus.GenerationFailed, saved.Status);
        Assert.False(string.IsNullOrWhiteSpace(saved.FailureReason));
        Assert.True(saved.UpdatedAt > saved.CreatedAt);
    }

    [Fact]
    public async Task GetById_UsesTheSameStaleRuleAsGetLatest()
    {
        var trip = await AddTripAsync();
        var running = await AddGeneratingProposalAsync(trip.Id, TimeSpan.FromMinutes(3));

        var stillRunning = await _service.GetProposalByIdAsync(trip.Id, running.ProposalId, travelerId: 1);
        Assert.Equal("Generating", stillRunning.Status);

        running.CreatedAt = DateTime.UtcNow - AgentTimeouts.GeneratingStaleAfter - TimeSpan.FromMinutes(1);
        await _db.SaveChangesAsync();

        var abandoned = await _service.GetProposalByIdAsync(trip.Id, running.ProposalId, travelerId: 1);
        Assert.Equal("GenerationFailed", abandoned.Status);
    }

    [Fact]
    public async Task Generate_WhileAnotherIsStillWithinMaxRuntime_IsRejected()
    {
        var trip = await AddTripAsync();
        var running = await AddGeneratingProposalAsync(trip.Id, TimeSpan.FromMinutes(3));

        await Assert.ThrowsAsync<ValidationException>(() => _service.GenerateProposalAsync(trip.Id, travelerId: 1));

        var saved = await _db.TripProposals.AsNoTracking().SingleAsync(p => p.Id == running.Id);
        Assert.Equal(ProposalStatus.Generating, saved.Status);
        Assert.Equal(1, await _db.TripProposals.CountAsync());
    }

    [Fact]
    public async Task Generate_AfterAbandonedGenerating_FailsTheOldOneAndStartsANewOne()
    {
        var trip = await AddTripAsync();
        var old = await AddGeneratingProposalAsync(trip.Id, AgentTimeouts.GeneratingStaleAfter + TimeSpan.FromMinutes(1));
        _agentClient.NextResult = new AgentProposalResult { Status = "Generated", Payload = "{}" };

        var dto = await _service.GenerateProposalAsync(trip.Id, travelerId: 1);

        Assert.Equal("Generated", dto.Status);
        Assert.Equal(2, dto.Version);
        var oldSaved = await _db.TripProposals.AsNoTracking().SingleAsync(p => p.Id == old.Id);
        Assert.Equal(ProposalStatus.GenerationFailed, oldSaved.Status);
    }

    [Fact]
    public async Task Generate_WhenSavingTheResultFails_EndsInGenerationFailedNotGenerating()
    {
        var trip = await AddTripAsync();
        _agentClient.NextResult = new AgentProposalResult
        {
            Status = "Generated",
            Payload = "{}",
            ExecutionSummaries = new List<AgentExecutionSummary>
            {
                new() { AgentIdentity = "m3_transport_weather", Status = "Success", FinalOutcome = "Pass" }
            }
        };
        _db.FailNextSaveWithSummaries = true;

        var dto = await _service.GenerateProposalAsync(trip.Id, travelerId: 1);

        Assert.Equal("GenerationFailed", dto.Status);
        var saved = await _db.TripProposals.AsNoTracking().SingleAsync();
        Assert.Equal(ProposalStatus.GenerationFailed, saved.Status);
        Assert.False(string.IsNullOrWhiteSpace(saved.FailureReason));
        Assert.Equal(0, await _db.ExecutionSummaries.CountAsync());
    }
}
