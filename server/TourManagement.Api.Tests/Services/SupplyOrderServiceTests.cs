using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Common.Exceptions;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.SupplyOrders;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Implementations;
using Xunit;

namespace TourManagement.Api.Tests.Services;

public class SupplyOrderServiceTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var db = new AppDbContext(options);
        
        // Setup initial data
        db.Users.Add(new User { Id = 1, FullName = "Test Supplier", Email = "s@test.com", PasswordHash = "hash", Role = Roles.Supplier, Status = UserStatus.Active });
        db.Users.Add(new User { Id = 2, FullName = "Test Traveler", Email = "t@test.com", PasswordHash = "hash", Role = Roles.Traveler, Status = UserStatus.Active });
        
        db.Trips.Add(new Trip { Id = 1, TravelerId = 2, Title = "My Trip" });

        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task CreateAsync_WhenInsufficientStock_ThrowsValidationException()
    {
        var db = CreateDb(Guid.NewGuid().ToString());
        var contractService = new ContractService(db);
        var service = new SupplyOrderService(db, contractService);

        // Seed valid contract
        db.Contracts.Add(new Contract { SupplierId = 1, Status = ContractStatus.Active, StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1) });
        
        // Seed supply with limited stock
        db.Supplies.Add(new Supply { Id = 1, SupplierId = 1, Name = "Tent", PricePerUnit = 10, StockQuantity = 2, Status = SupplyStatus.Active });
        await db.SaveChangesAsync();

        var dto = new CreateSupplyOrderDto { TripId = 1, SupplyId = 1, Quantity = 5 };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(dto, 2));
        Assert.Contains("Insufficient stock", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenNoValidContract_ThrowsValidationException()
    {
        var db = CreateDb(Guid.NewGuid().ToString());
        var contractService = new ContractService(db);
        var service = new SupplyOrderService(db, contractService);

        // No contract seeded!

        db.Supplies.Add(new Supply { Id = 1, SupplierId = 1, Name = "Tent", PricePerUnit = 10, StockQuantity = 10, Status = SupplyStatus.Active });
        await db.SaveChangesAsync();

        var dto = new CreateSupplyOrderDto { TripId = 1, SupplyId = 1, Quantity = 1 };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(dto, 2));
        Assert.Contains("valid contract", ex.Message);
    }

    [Fact]
    public async Task CancelAsync_RestoresStockAndUpdatesStatus()
    {
        var db = CreateDb(Guid.NewGuid().ToString());
        var contractService = new ContractService(db);
        var service = new SupplyOrderService(db, contractService);

        db.Supplies.Add(new Supply { Id = 1, SupplierId = 1, Name = "Tent", PricePerUnit = 10, StockQuantity = 5, Status = SupplyStatus.Active });
        db.SupplyOrders.Add(new SupplyOrder { Id = 1, TripId = 1, SupplyId = 1, Quantity = 3, PriceAtOrderTime = 10, Status = BookingStatus.Held });
        await db.SaveChangesAsync();

        // Act
        var result = await service.CancelAsync(1, 2); // Traveler ID 2 cancels their order

        // Assert
        Assert.Equal(BookingStatus.Cancelled, result.Status);
        
        var supply = await db.Supplies.FindAsync(1);
        Assert.Equal(8, supply!.StockQuantity); // 5 original + 3 restored = 8
    }
}
