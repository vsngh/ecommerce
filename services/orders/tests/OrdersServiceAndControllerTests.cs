using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Orders.Api.Controllers;
using Orders.Api.Data;
using Orders.Api.Dtos;
using Orders.Api.Entities;
using Orders.Api.Services;
using Xunit;

namespace Orders.Api.Tests;

public class OrdersServiceTests
{
    [Fact]
    public async Task CreateOrderAsync_ComputesTotalAndPersistsItems()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var order = await service.CreateOrderAsync(new CreateOrderRequest
        {
            UserId = Guid.NewGuid(),
            OrderItems =
            [
                new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 2, UnitPrice = 15 },
                new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 10 }
            ]
        });

        Assert.Equal(40, order.TotalAmount);
        Assert.Equal(2, order.OrderItems.Count);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ReturnsFalseWhenOrderMissing()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var updated = await service.UpdateOrderStatusAsync(Guid.NewGuid(), "Completed");

        Assert.False(updated);
    }

    [Fact]
    public async Task AddOrderItemAsync_AddsItemToExistingOrder()
    {
        using var dbContext = CreateDbContext();
        var order = new Order { UserId = Guid.NewGuid(), TotalAmount = 10, Status = "Pending" };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var item = await service.AddOrderItemAsync(order.Id, new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 10 });

        Assert.NotNull(item);
        Assert.Single(await dbContext.OrderItems.ToListAsync());
    }

    private static OrdersService CreateService(OrdersDbContext dbContext) => new(dbContext, CreateCache());

    private static OrdersDbContext CreateDbContext() => new(new DbContextOptionsBuilder<OrdersDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}

public class OrdersControllerIntegrationTests
{
    [Fact]
    public async Task CreateOrder_ReturnsCreatedAtAction()
    {
        using var dbContext = CreateDbContext();
        var controller = new OrdersController(CreateService(dbContext));

        var result = await controller.CreateOrder(new CreateOrderRequest
        {
            UserId = Guid.NewGuid(),
            OrderItems = [new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 25 }]
        });

        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact]
    public async Task GetOrderById_ReturnsNotFoundForMissingOrder()
    {
        using var dbContext = CreateDbContext();
        var controller = new OrdersController(CreateService(dbContext));

        var result = await controller.GetOrderById(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    private static OrdersService CreateService(OrdersDbContext dbContext) => new(dbContext, CreateCache());

    private static OrdersDbContext CreateDbContext() => new(new DbContextOptionsBuilder<OrdersDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}
