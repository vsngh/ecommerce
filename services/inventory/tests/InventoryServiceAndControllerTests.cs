using Inventory.Api.Controllers;
using Inventory.Api.Data;
using Inventory.Api.Dtos;
using Inventory.Api.Entities;
using Inventory.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace Inventory.Api.Tests;

public class InventoryServiceTests
{
    [Fact]
    public async Task CreateInventoryItemAsync_PersistsItem()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var item = await service.CreateInventoryItemAsync(new CreateInventoryItemRequest
        {
            ProductId = Guid.NewGuid(),
            QuantityAvailable = 10,
            QuantityReserved = 2
        });

        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal(10, item.QuantityAvailable);
        Assert.Single(await dbContext.InventoryItems.ToListAsync());
    }

    [Fact]
    public async Task ReserveInventoryAsync_FailsWhenQuantityUnavailable()
    {
        using var dbContext = CreateDbContext();
        var item = new InventoryItem { ProductId = Guid.NewGuid(), QuantityAvailable = 1, QuantityReserved = 0 };
        dbContext.InventoryItems.Add(item);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.ReserveInventoryAsync(item.Id, 3);

        Assert.False(result.IsSuccess);
        Assert.Equal("Not enough inventory available.", result.Error);
    }

    [Fact]
    public async Task ReleaseInventoryAsync_MovesReservedBackToAvailable()
    {
        using var dbContext = CreateDbContext();
        var item = new InventoryItem { ProductId = Guid.NewGuid(), QuantityAvailable = 2, QuantityReserved = 4 };
        dbContext.InventoryItems.Add(item);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.ReleaseInventoryAsync(item.Id, 3);

        Assert.True(result.IsSuccess);
        var saved = await dbContext.InventoryItems.FindAsync(item.Id);
        Assert.Equal(5, saved!.QuantityAvailable);
        Assert.Equal(1, saved.QuantityReserved);
    }

    private static InventoryService CreateService(InventoryDbContext dbContext) => new(dbContext, CreateCache());

    private static InventoryDbContext CreateDbContext() => new(new DbContextOptionsBuilder<InventoryDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}

public class InventoryControllerIntegrationTests
{
    [Fact]
    public async Task ReserveInventory_ReturnsBadRequestWhenUnavailable()
    {
        using var dbContext = CreateDbContext();
        var item = new InventoryItem { ProductId = Guid.NewGuid(), QuantityAvailable = 1, QuantityReserved = 0 };
        dbContext.InventoryItems.Add(item);
        await dbContext.SaveChangesAsync();
        var controller = new InventoryController(CreateService(dbContext));

        var result = await controller.ReserveInventory(item.Id, new ReserveInventoryRequest { Quantity = 2 });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetInventoryItemById_ReturnsNotFoundForMissingItem()
    {
        using var dbContext = CreateDbContext();
        var controller = new InventoryController(CreateService(dbContext));

        var result = await controller.GetInventoryItemById(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    private static InventoryService CreateService(InventoryDbContext dbContext) => new(dbContext, CreateCache());

    private static InventoryDbContext CreateDbContext() => new(new DbContextOptionsBuilder<InventoryDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}
