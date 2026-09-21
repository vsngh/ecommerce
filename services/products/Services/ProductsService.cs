using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Products.Api.Data;
using Products.Api.Dtos;
using Products.Api.Entities;
using System.Text.Json;

namespace Products.Api.Services;

public class ProductsService : IProductsService
{
    private readonly ProductsDbContext dbContext;
    private readonly IDistributedCache cache;
    private static readonly DistributedCacheEntryOptions CacheOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };

    public ProductsService(ProductsDbContext dbContext, IDistributedCache cache)
    {
        this.dbContext = dbContext;
        this.cache = cache;
    }

    public async Task<IReadOnlyList<Product>> GetProductsAsync()
    {
        const string cacheKey = "products:all";
        var cachedProducts = await cache.GetStringAsync(cacheKey);
        if (cachedProducts is not null)
        {
            return JsonSerializer.Deserialize<List<Product>>(cachedProducts) ?? [];
        }

        var products = await dbContext.Products.ToListAsync();
        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(products), CacheOptions);
        return products;
    }

    public async Task<Product?> GetProductByIdAsync(Guid productId)
    {
        var cacheKey = $"products:{productId}";
        var cachedProduct = await cache.GetStringAsync(cacheKey);
        if (cachedProduct is not null)
        {
            return JsonSerializer.Deserialize<Product>(cachedProduct);
        }

        var product = await dbContext.Products.FindAsync(productId);
        if (product is not null)
        {
            await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(product), CacheOptions);
        }

        return product;
    }

    public async Task<Product> CreateProductAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        await RemoveProductCache(product.Id);
        return product;
    }

    public async Task<bool> UpdateProductAsync(Guid productId, UpdateProductRequest request)
    {
        var product = await dbContext.Products.FindAsync(productId);
        if (product is null) return false;

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;

        await dbContext.SaveChangesAsync();
        await RemoveProductCache(productId);
        return true;
    }

    public async Task<bool> UpdateProductStockAsync(Guid productId, int stockQuantity)
    {
        var product = await dbContext.Products.FindAsync(productId);
        if (product is null) return false;

        product.StockQuantity = stockQuantity;
        await dbContext.SaveChangesAsync();
        await RemoveProductCache(productId);
        return true;
    }

    public async Task<bool> UpdateProductPriceAsync(Guid productId, decimal price)
    {
        var product = await dbContext.Products.FindAsync(productId);
        if (product is null) return false;

        product.Price = price;
        await dbContext.SaveChangesAsync();
        await RemoveProductCache(productId);
        return true;
    }

    public async Task<bool> DeleteProductAsync(Guid productId)
    {
        var product = await dbContext.Products.FindAsync(productId);
        if (product is null) return false;

        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync();
        await RemoveProductCache(productId);
        return true;
    }

    private async Task RemoveProductCache(Guid productId)
    {
        await cache.RemoveAsync("products:all");
        await cache.RemoveAsync($"products:{productId}");
    }
}
