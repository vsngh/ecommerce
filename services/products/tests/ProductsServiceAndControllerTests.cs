using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Products.Api.Controllers;
using Products.Api.Data;
using Products.Api.Dtos;
using Products.Api.Entities;
using Products.Api.Security;
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
    public async Task GetProductsAsync_ReturnsProductsFromRepository()
    {
        using var dbContext = CreateDbContext();
        dbContext.Products.Add(new Product { Name = "Keyboard", Description = "Mechanical", Price = 120 });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var products = await service.GetProductsAsync();

        Assert.Single(products);
        Assert.Equal("Keyboard", products[0].Name);
    }

    [Fact]
    public async Task GetProductByIdAsync_ReturnsNullWhenProductDoesNotExist()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var product = await service.GetProductByIdAsync(Guid.NewGuid());

        Assert.Null(product);
    }

    [Fact]
    public async Task UpdateProductAsync_UpdatesExistingProduct()
    {
        using var dbContext = CreateDbContext();
        var product = new Product { Name = "Lamp", Description = "Desk lamp", Price = 20, StockQuantity = 3 };
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var updated = await service.UpdateProductAsync(product.Id, new UpdateProductRequest
        {
            Name = "Reading lamp",
            Description = "LED desk lamp",
            Price = 30,
            StockQuantity = 5
        });

        Assert.True(updated);
        var savedProduct = await dbContext.Products.FindAsync(product.Id);
        Assert.Equal("Reading lamp", savedProduct!.Name);
        Assert.Equal("LED desk lamp", savedProduct.Description);
        Assert.Equal(30, savedProduct.Price);
        Assert.Equal(5, savedProduct.StockQuantity);
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
    public async Task UpdateProductStockAsync_UpdatesExistingProduct()
    {
        using var dbContext = CreateDbContext();
        var product = new Product { Name = "Lamp", Description = "Desk lamp", Price = 20, StockQuantity = 3 };
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var updated = await service.UpdateProductStockAsync(product.Id, 8);

        Assert.True(updated);
        Assert.Equal(8, (await dbContext.Products.FindAsync(product.Id))!.StockQuantity);
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

    [Fact]
    public async Task DeleteProductAsync_RemovesExistingProduct()
    {
        using var dbContext = CreateDbContext();
        var product = new Product { Name = "Lamp", Description = "Desk lamp", Price = 20, StockQuantity = 3 };
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var deleted = await service.DeleteProductAsync(product.Id);

        Assert.True(deleted);
        Assert.Null(await dbContext.Products.FindAsync(product.Id));
    }

    private static ProductsService CreateService(ProductsDbContext dbContext) => new(new ProductsRepository(dbContext), CreateCache());

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
    public void ProductMutationEndpoints_RequireProductsManagePolicy()
    {
        var mutationEndpoints = new[]
        {
            nameof(ProductsController.CreateProduct),
            nameof(ProductsController.UpdateProduct),
            nameof(ProductsController.UpdateProductStock),
            nameof(ProductsController.UpdateProductPrice),
            nameof(ProductsController.DeleteProduct)
        };

        foreach (var endpoint in mutationEndpoints)
        {
            var method = typeof(ProductsController).GetMethod(endpoint);
            Assert.NotNull(method);
            var authorization = method!.GetCustomAttribute<AuthorizeAttribute>();
            Assert.Equal(PermissionPolicies.ProductsManage, authorization?.Policy);
        }
    }

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

    private static ProductsService CreateService(ProductsDbContext dbContext) => new(new ProductsRepository(dbContext), CreateCache());

    private static ProductsDbContext CreateDbContext()
    {
        return new ProductsDbContext(new DbContextOptionsBuilder<ProductsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}
