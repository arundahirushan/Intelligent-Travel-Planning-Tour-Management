using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Common.Exceptions;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Supplier;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Implementations;
using Xunit;

namespace TourManagement.Api.Tests.Services;

public class ContractServiceTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var db = new AppDbContext(options);

        // Seed a default Supplier user
        db.Users.Add(new User
        {
            Id           = 1,
            FullName     = "Test Supplier",
            Email        = "supplier@test.com",
            PasswordHash = "hash",
            Role         = Roles.Supplier,
            Status       = UserStatus.Active
        });

        db.SaveChanges();
        return db;
    }

    // ── Test 1: Creating a second Contract while a valid one already exists is rejected ──
    [Fact]
    public async Task CreateFirstContract_WhenValidContractAlreadyExists_ThrowsValidationException()
    {
        var db = CreateDb(Guid.NewGuid().ToString());
        var service = new ContractService(db);
        var today = DateTime.UtcNow.Date;

        // Seed an existing currently-valid contract (Status = Active, EndDate in the future)
        db.Contracts.Add(new Contract
        {
            Id         = 1,
            SupplierId = 1,
            StartDate  = today.AddDays(-10),
            EndDate    = today.AddDays(30),
            Terms      = "Existing active contract",
            Status     = ContractStatus.Active
        });
        await db.SaveChangesAsync();

        var dto = new CreateContractDto
        {
            SupplierId = 1,
            StartDate  = today,
            EndDate    = today.AddDays(90),
            Terms      = "Second contract attempt"
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateFirstContractAsync(dto));

        Assert.Contains("already has an active, valid contract", exception.Message);
    }

    // ── Test 2: Approving a Renewal ContractRequest correctly extends existing contract ──
    [Fact]
    public async Task ApproveRequest_Renewal_ExtendsExistingContractEndDate_DoesNotCreateNewRow()
    {
        var db = CreateDb(Guid.NewGuid().ToString());
        var requestService = new ContractRequestService(db);
        var today = DateTime.UtcNow.Date;

        // Seed existing contract
        var existingContract = new Contract
        {
            Id         = 10,
            SupplierId = 1,
            StartDate  = today.AddDays(-60),
            EndDate    = today.AddDays(5),
            Terms      = "Original Terms",
            Status     = ContractStatus.Active
        };
        db.Contracts.Add(existingContract);

        // Seed renewal contract request
        var renewalRequest = new ContractRequest
        {
            Id                 = 100,
            SupplierId         = 1,
            RequestType        = ContractRequestType.Renewal,
            ExistingContractId = 10,
            RequestedEndDate   = today.AddDays(180),
            RequestedTerms     = "Updated Terms for Renewal",
            Status             = ContractRequestStatus.Pending
        };
        db.ContractRequests.Add(renewalRequest);
        await db.SaveChangesAsync();

        // Act: Approve the renewal request
        var result = await requestService.ApproveAsync(100);

        // Assert: Request status is Approved
        Assert.Equal(ContractRequestStatus.Approved, result.Status);

        // Assert: Contracts table still has exactly 1 row (no new row was created)
        var totalContracts = await db.Contracts.CountAsync();
        Assert.Equal(1, totalContracts);

        // Assert: Existing contract's EndDate and Terms were updated
        var updatedContract = await db.Contracts.FindAsync(10);
        Assert.NotNull(updatedContract);
        Assert.Equal(today.AddDays(180), updatedContract.EndDate);
        Assert.Equal("Updated Terms for Renewal", updatedContract.Terms);
    }

    // ── Test 3: Contract past its EndDate but stored as Active is identified as invalid ──
    [Fact]
    public async Task IsContractCurrentlyValidAsync_WhenPastEndDateButStoredAsActive_ReturnsFalse()
    {
        var db = CreateDb(Guid.NewGuid().ToString());
        var service = new ContractService(db);
        var today = DateTime.UtcNow.Date;

        // Seed a contract where EndDate is in the past, but Status is still stored as Active
        db.Contracts.Add(new Contract
        {
            Id         = 5,
            SupplierId = 1,
            StartDate  = today.AddDays(-100),
            EndDate    = today.AddDays(-1), // expired yesterday
            Terms      = "Expired contract with stored Active status",
            Status     = ContractStatus.Active
        });
        await db.SaveChangesAsync();

        // Act & Assert: Live computed check identifies it as INVALID
        var isValid = await service.IsContractCurrentlyValidAsync(1);
        Assert.False(isValid);

        // Add a second contract that is actually valid
        db.Contracts.Add(new Contract
        {
            Id         = 6,
            SupplierId = 1,
            StartDate  = today,
            EndDate    = today.AddDays(30),
            Terms      = "Valid contract",
            Status     = ContractStatus.Active
        });
        await db.SaveChangesAsync();

        var isValidNow = await service.IsContractCurrentlyValidAsync(1);
        Assert.True(isValidNow);
    }
}
