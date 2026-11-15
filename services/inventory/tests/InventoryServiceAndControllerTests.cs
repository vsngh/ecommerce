using System.ComponentModel.DataAnnotations;
using Inventory.Api.Security;
using Inventory.Api.Controllers;
using Inventory.Api.Data;
using Inventory.Api.Dtos;
using Inventory.Api.Entities;
using Inventory.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
    public async Task ReserveInventoryAsync_MovesQuantityFromAvailableToReserved()
    {
        using var dbContext = CreateDbContext();
        var item = new InventoryItem { ProductId = Guid.NewGuid(), QuantityAvailable = 8, QuantityReserved = 2 };
        dbContext.InventoryItems.Add(item);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.ReserveInventoryAsync(item.Id, 3);

        Assert.True(result.IsSuccess);
        var savedItem = await dbContext.InventoryItems.FindAsync(item.Id);
        Assert.Equal(5, savedItem!.QuantityAvailable);
        Assert.Equal(5, savedItem.QuantityReserved);
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

    [Fact]
    public async Task GetInventoryByProductIdAsync_ReturnsMatchingItem()
    {
        using var dbContext = CreateDbContext();
        var productId = Guid.NewGuid();
        dbContext.InventoryItems.Add(new InventoryItem { ProductId = productId, QuantityAvailable = 7 });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var item = await service.GetInventoryByProductIdAsync(productId);

        Assert.NotNull(item);
        Assert.Equal(7, item!.QuantityAvailable);
    }

    [Fact]
    public async Task UpdateInventoryItemAsync_UpdatesExistingItem()
    {
        using var dbContext = CreateDbContext();
        var item = new InventoryItem { ProductId = Guid.NewGuid(), QuantityAvailable = 5, QuantityReserved = 1 };
        dbContext.InventoryItems.Add(item);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);
        var productId = Guid.NewGuid();

        var updated = await service.UpdateInventoryItemAsync(item.Id, new UpdateInventoryItemRequest
        {
            ProductId = productId,
            QuantityAvailable = 12,
            QuantityReserved = 2
        });

        Assert.True(updated);
        var savedItem = await dbContext.InventoryItems.FindAsync(item.Id);
        Assert.Equal(productId, savedItem!.ProductId);
        Assert.Equal(12, savedItem.QuantityAvailable);
        Assert.Equal(2, savedItem.QuantityReserved);
    }

    [Fact]
    public async Task UpdateInventoryQuantityAsync_UpdatesExistingItem()
    {
        using var dbContext = CreateDbContext();
        var item = new InventoryItem { ProductId = Guid.NewGuid(), QuantityAvailable = 5 };
        dbContext.InventoryItems.Add(item);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var updated = await service.UpdateInventoryQuantityAsync(item.Id, 9);

        Assert.True(updated);
        Assert.Equal(9, (await dbContext.InventoryItems.FindAsync(item.Id))!.QuantityAvailable);
    }

    [Fact]
    public async Task ReserveInventoryAsync_ReturnsMissingWhenItemDoesNotExist()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.ReserveInventoryAsync(Guid.NewGuid(), 1);

        Assert.True(result.NotFound);
    }

    [Fact]
    public async Task ReleaseInventoryAsync_FailsWhenQuantityExceedsReserved()
    {
        using var dbContext = CreateDbContext();
        var item = new InventoryItem { ProductId = Guid.NewGuid(), QuantityAvailable = 2, QuantityReserved = 1 };
        dbContext.InventoryItems.Add(item);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.ReleaseInventoryAsync(item.Id, 2);

        Assert.False(result.IsSuccess);
        Assert.Equal("Cannot release more inventory than is reserved.", result.Error);
        Assert.Equal(1, (await dbContext.InventoryItems.FindAsync(item.Id))!.QuantityReserved);
    }

    [Fact]
    public async Task DeleteInventoryItemAsync_RemovesExistingItem()
    {
        using var dbContext = CreateDbContext();
        var item = new InventoryItem { ProductId = Guid.NewGuid(), QuantityAvailable = 5 };
        dbContext.InventoryItems.Add(item);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var deleted = await service.DeleteInventoryItemAsync(item.Id);

        Assert.True(deleted);
        Assert.Null(await dbContext.InventoryItems.FindAsync(item.Id));
    }

    private static InventoryService CreateService(InventoryDbContext dbContext) => new(new InventoryRepository(dbContext), CreateCache());

    private static InventoryDbContext CreateDbContext() => new(new DbContextOptionsBuilder<InventoryDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}

public class InventoryControllerIntegrationTests
{
    [Fact]
    public void InventoryController_RequiresInventoryManagePolicy()
    {
        var authorization = Attribute.GetCustomAttribute(typeof(InventoryController), typeof(AuthorizeAttribute))
            as AuthorizeAttribute;

        Assert.Equal(PermissionPolicies.InventoryManage, authorization?.Policy);
    }

    [Fact]
    public void UpdateInventoryQuantityRequest_RejectsNegativeQuantity()
    {
        var request = new UpdateInventoryQuantityRequest { QuantityAvailable = -1 };
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
    }

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

    private static InventoryService CreateService(InventoryDbContext dbContext) => new(new InventoryRepository(dbContext), CreateCache());

    private static InventoryDbContext CreateDbContext() => new(new DbContextOptionsBuilder<InventoryDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}
