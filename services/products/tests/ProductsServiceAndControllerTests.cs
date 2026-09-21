using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Products.Api.Controllers;
using Products.Api.Data;
using Products.Api.Dtos;
using Products.Api.Entities;
using Products.Api.Services;
using Xunit;

namespace Products.Api.Tests;

public class ProductsServiceTests
{
    [Fact]
    public async Task CreateProductAsync_PersistsProductAndSetsCreatedAt()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var product = await service.CreateProductAsync(new CreateProductRequest
        {
            Name = "Keyboard",
            Description = "Mechanical keyboard",
            Price = 120,
            StockQuantity = 10
        });

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("Keyboard", product.Name);
        Assert.Single(await dbContext.Products.ToListAsync());
    }

    [Fact]
    public async Task UpdateProductStockAsync_ReturnsFalseWhenProductDoesNotExist()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var updated = await service.UpdateProductStockAsync(Guid.NewGuid(), 5);

        Assert.False(updated);
    }

    [Fact]
    public async Task UpdateProductPriceAsync_UpdatesExistingProduct()
    {
        using var dbContext = CreateDbContext();
        var product = new Product { Name = "Lamp", Description = "Desk lamp", Price = 20, StockQuantity = 3 };
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var updated = await service.UpdateProductPriceAsync(product.Id, 25);

        Assert.True(updated);
        Assert.Equal(25, (await dbContext.Products.FindAsync(product.Id))!.Price);
    }

    private static ProductsService CreateService(ProductsDbContext dbContext) => new(dbContext, CreateCache());

    private static ProductsDbContext CreateDbContext()
    {
        return new ProductsDbContext(new DbContextOptionsBuilder<ProductsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}

public class ProductsControllerIntegrationTests
{
    [Fact]
    public async Task CreateProduct_ReturnsCreatedAtAction()
    {
        using var dbContext = CreateDbContext();
        var controller = new ProductsController(CreateService(dbContext));

        var result = await controller.CreateProduct(new CreateProductRequest
        {
            Name = "Mouse",
            Description = "Wireless mouse",
            Price = 45,
            StockQuantity = 15
        });

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ProductsController.GetProductById), created.ActionName);
    }

    [Fact]
    public async Task GetProductById_ReturnsNotFoundForMissingProduct()
    {
        using var dbContext = CreateDbContext();
        var controller = new ProductsController(CreateService(dbContext));

        var result = await controller.GetProductById(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    private static ProductsService CreateService(ProductsDbContext dbContext) => new(dbContext, CreateCache());

    private static ProductsDbContext CreateDbContext()
    {
        return new ProductsDbContext(new DbContextOptionsBuilder<ProductsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}
