using Cart.Api.Controllers;
using Cart.Api.Data;
using Cart.Api.Dtos;
using Cart.Api.Entities;
using Cart.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cart.Api.Tests;

public class CartServiceTests
{
    [Fact]
    public async Task CreateCartAsync_PersistsCartWithItems()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var cart = await service.CreateCartAsync(new CreateCartRequest
        {
            UserId = Guid.NewGuid(),
            CartItems =
            [
                new CreateCartItemRequest { ProductId = Guid.NewGuid(), Quantity = 2, UnitPrice = 10 }
            ]
        });

        Assert.NotEqual(Guid.Empty, cart.Id);
        Assert.Single(cart.CartItems);
    }

    [Fact]
    public async Task CheckoutAsync_ChangesStatusAndReturnsSummary()
    {
        using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var cart = new Entities.Cart
        {
            UserId = userId,
            Status = "Active",
            CartItems = [new CartItem { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 10 }]
        };
        dbContext.Carts.Add(cart);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.CheckoutAsync(cart.Id, new CheckoutRequest { UserId = userId, PaymentMethod = "Card" });

        Assert.NotNull(result);
        Assert.Equal("CheckoutStarted", result!.Status);
        Assert.Equal(1, result.ItemCount);
    }

    [Fact]
    public async Task AddCartItemAsync_ReturnsNullWhenCartMissing()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var item = await service.AddCartItemAsync(Guid.NewGuid(), new CreateCartItemRequest { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 5 });

        Assert.Null(item);
    }

    private static CartService CreateService(CartDbContext dbContext) => new(dbContext, CreateCache());

    private static CartDbContext CreateDbContext() => new(new DbContextOptionsBuilder<CartDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}

public class CartControllerIntegrationTests
{
    [Fact]
    public async Task CreateCart_ReturnsCreatedAtAction()
    {
        using var dbContext = CreateDbContext();
        var controller = new CartController(CreateService(dbContext));

        var result = await controller.CreateCart(new CreateCartRequest { UserId = Guid.NewGuid() });

        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact]
    public async Task Checkout_ReturnsNotFoundForMissingCart()
    {
        using var dbContext = CreateDbContext();
        var controller = new CartController(CreateService(dbContext));

        var result = await controller.Checkout(Guid.NewGuid(), new CheckoutRequest { UserId = Guid.NewGuid(), PaymentMethod = "Card" });

        Assert.IsType<NotFoundResult>(result);
    }

    private static CartService CreateService(CartDbContext dbContext) => new(dbContext, CreateCache());

    private static CartDbContext CreateDbContext() => new(new DbContextOptionsBuilder<CartDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}
