using Cart.Api.Controllers;
using Cart.Api.Data;
using Cart.Api.Dtos;
using Cart.Api.Entities;
using Cart.Api.Security;
using Cart.Api.Services;
using System.Security.Claims;
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

    [Fact]
    public async Task GetActiveCartByUserIdAsync_ReturnsActiveCartWithItems()
    {
        using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        dbContext.Carts.AddRange(
            new Entities.Cart { UserId = userId, Status = "Completed" },
            new Entities.Cart
            {
                UserId = userId,
                Status = "Active",
                CartItems = [new CartItem { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 8 }]
            });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var cart = await service.GetActiveCartByUserIdAsync(userId);

        Assert.NotNull(cart);
        Assert.Equal("Active", cart!.Status);
        Assert.Single(cart.CartItems);
    }

    [Fact]
    public async Task GetCartItemsAsync_ReturnsNullWhenCartDoesNotExist()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var items = await service.GetCartItemsAsync(Guid.NewGuid());

        Assert.Null(items);
    }

    [Fact]
    public async Task GetCartItemByIdAsync_ReturnsItemForCart()
    {
        using var dbContext = CreateDbContext();
        var cart = new Entities.Cart
        {
            UserId = Guid.NewGuid(),
            CartItems = [new CartItem { ProductId = Guid.NewGuid(), Quantity = 2, UnitPrice = 12 }]
        };
        dbContext.Carts.Add(cart);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var item = await service.GetCartItemByIdAsync(cart.Id, cart.CartItems.Single().Id);

        Assert.NotNull(item);
        Assert.Equal(2, item!.Quantity);
    }

    [Fact]
    public async Task AddCartItemAsync_PersistsItemAndUpdatesCartTimestamp()
    {
        using var dbContext = CreateDbContext();
        var cart = new Entities.Cart { UserId = Guid.NewGuid() };
        dbContext.Carts.Add(cart);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);
        var updateStartedAt = DateTime.UtcNow;

        var item = await service.AddCartItemAsync(cart.Id, new CreateCartItemRequest
        {
            ProductId = Guid.NewGuid(),
            Quantity = 3,
            UnitPrice = 4
        });

        Assert.NotNull(item);
        Assert.Equal(3, item!.Quantity);
        Assert.True((await dbContext.Carts.FindAsync(cart.Id))!.UpdatedAt >= updateStartedAt);
    }

    [Fact]
    public async Task UpdateCartItemAsync_UpdatesItemAndCartTimestamp()
    {
        using var dbContext = CreateDbContext();
        var item = new CartItem { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 10 };
        var cart = new Entities.Cart { UserId = Guid.NewGuid(), CartItems = [item] };
        dbContext.Carts.Add(cart);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);
        var updateStartedAt = DateTime.UtcNow;

        var updated = await service.UpdateCartItemAsync(cart.Id, item.Id, new UpdateCartItemRequest
        {
            ProductId = Guid.NewGuid(),
            Quantity = 5,
            UnitPrice = 7
        });

        Assert.True(updated);
        Assert.Equal(5, (await dbContext.CartItems.FindAsync(item.Id))!.Quantity);
        Assert.True((await dbContext.Carts.FindAsync(cart.Id))!.UpdatedAt >= updateStartedAt);
    }

    [Fact]
    public async Task UpdateCartItemAsync_ReturnsFalseWhenItemDoesNotExist()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var updated = await service.UpdateCartItemAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateCartItemRequest
        {
            ProductId = Guid.NewGuid(),
            Quantity = 1,
            UnitPrice = 2
        });

        Assert.False(updated);
    }

    [Fact]
    public async Task RemoveCartItemAsync_RemovesExistingItem()
    {
        using var dbContext = CreateDbContext();
        var item = new CartItem { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 10 };
        var cart = new Entities.Cart { UserId = Guid.NewGuid(), CartItems = [item] };
        dbContext.Carts.Add(cart);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var removed = await service.RemoveCartItemAsync(cart.Id, item.Id);

        Assert.True(removed);
        Assert.Null(await dbContext.CartItems.FindAsync(item.Id));
    }

    [Fact]
    public async Task RemoveCartItemAsync_ReturnsFalseWhenItemDoesNotExist()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var removed = await service.RemoveCartItemAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(removed);
    }

    [Fact]
    public async Task DeleteCartAsync_ReturnsFalseWhenCartDoesNotExist()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var deleted = await service.DeleteCartAsync(Guid.NewGuid());

        Assert.False(deleted);
    }

    [Fact]
    public async Task UpdateCartStatusAsync_PersistsStatusAndUpdatedAt()
    {
        using var dbContext = CreateDbContext();
        var cart = new Entities.Cart { UserId = Guid.NewGuid(), Status = "Active" };
        dbContext.Carts.Add(cart);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);
        var updateStartedAt = DateTime.UtcNow;

        var updated = await service.UpdateCartStatusAsync(cart.Id, "CheckedOut");

        Assert.True(updated);
        var savedCart = await dbContext.Carts.FindAsync(cart.Id);
        Assert.Equal("CheckedOut", savedCart!.Status);
        Assert.True(savedCart.UpdatedAt >= updateStartedAt);
    }

    private static CartService CreateService(CartDbContext dbContext) => new(new CartRepository(dbContext), CreateCache());

    private static CartDbContext CreateDbContext() => new(new DbContextOptionsBuilder<CartDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}

public class CartControllerIntegrationTests
{
    [Fact]
    public void CartControllerRequiresAuthenticationAndProtectsAdministrativeRoutes()
    {
        Assert.NotNull(typeof(CartController).GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true).SingleOrDefault());
        Assert.Equal(PermissionPolicies.CartsRead, typeof(CartController)
            .GetMethod(nameof(CartController.GetCarts))!
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Single().Policy);
        Assert.Equal(PermissionPolicies.CartsManage, typeof(CartController)
            .GetMethod(nameof(CartController.Checkout))!
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Single().Policy);
    }

    [Fact]
    public async Task CreateCart_ReturnsCreatedAtAction()
    {
        using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var controller = CreateController(dbContext, userId);

        var result = await controller.CreateCart(new CreateCartRequest
        {
            UserId = Guid.NewGuid(),
            Status = "Completed"
        });

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var cart = Assert.IsType<Entities.Cart>(created.Value);
        Assert.Equal(userId, cart.UserId);
        Assert.Equal("Active", cart.Status);
    }

    [Fact]
    public async Task Checkout_ReturnsNotFoundForMissingCart()
    {
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext, Guid.NewGuid());

        var result = await controller.Checkout(Guid.NewGuid(), new CheckoutRequest { UserId = Guid.NewGuid(), PaymentMethod = "Card" });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetCartById_ReturnsNotFoundForAnotherUsersCart()
    {
        using var dbContext = CreateDbContext();
        var cart = new Entities.Cart { UserId = Guid.NewGuid() };
        dbContext.Carts.Add(cart);
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext, Guid.NewGuid());

        var result = await controller.GetCartById(cart.Id);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Checkout_UsesAuthenticatedUserInsteadOfRequestUserId()
    {
        using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var cart = new Entities.Cart { UserId = userId, Status = "Active" };
        dbContext.Carts.Add(cart);
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext, userId);

        var result = await controller.Checkout(cart.Id, new CheckoutRequest
        {
            UserId = Guid.NewGuid(),
            PaymentMethod = "Card"
        });

        var accepted = Assert.IsType<AcceptedResult>(result);
        var checkout = Assert.IsType<CheckoutResult>(accepted.Value);
        Assert.Equal(userId, checkout.UserId);
    }

    private static CartService CreateService(CartDbContext dbContext) => new(new CartRepository(dbContext), CreateCache());

    private static CartController CreateController(CartDbContext dbContext, Guid userId) => new(CreateService(dbContext))
    {
        ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                    "Test"))
            }
        }
    };

    private static CartDbContext CreateDbContext() => new(new DbContextOptionsBuilder<CartDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}
