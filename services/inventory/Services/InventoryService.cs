using Inventory.Api.Data;
using Inventory.Api.Dtos;
using Inventory.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Inventory.Api.Services;

public class InventoryService : IInventoryService
{
    private readonly InventoryDbContext dbContext;
    private readonly IDistributedCache cache;
    private static readonly DistributedCacheEntryOptions CacheOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };

    public InventoryService(InventoryDbContext dbContext, IDistributedCache cache)
    {
        this.dbContext = dbContext;
        this.cache = cache;
    }

    public async Task<IReadOnlyList<InventoryItem>> GetInventoryItemsAsync()
    {
        const string cacheKey = "inventory:all";
        var cachedItems = await cache.GetStringAsync(cacheKey);
        if (cachedItems is not null) return JsonSerializer.Deserialize<List<InventoryItem>>(cachedItems) ?? [];

        var items = await dbContext.InventoryItems.ToListAsync();
        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(items), CacheOptions);
        return items;
    }

    public async Task<InventoryItem?> GetInventoryItemByIdAsync(Guid inventoryItemId)
    {
        var cacheKey = $"inventory:{inventoryItemId}";
        var cachedItem = await cache.GetStringAsync(cacheKey);
        if (cachedItem is not null) return JsonSerializer.Deserialize<InventoryItem>(cachedItem);

        var item = await dbContext.InventoryItems.FindAsync(inventoryItemId);
        if (item is not null) await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(item), CacheOptions);
        return item;
    }

    public async Task<InventoryItem?> GetInventoryByProductIdAsync(Guid productId)
    {
        var cacheKey = $"inventory:product:{productId}";
        var cachedItem = await cache.GetStringAsync(cacheKey);
        if (cachedItem is not null) return JsonSerializer.Deserialize<InventoryItem>(cachedItem);

        var item = await dbContext.InventoryItems.FirstOrDefaultAsync(item => item.ProductId == productId);
        if (item is not null) await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(item), CacheOptions);
        return item;
    }

    public async Task<InventoryItem> CreateInventoryItemAsync(CreateInventoryItemRequest request)
    {
        var item = new InventoryItem
        {
            ProductId = request.ProductId,
            QuantityAvailable = request.QuantityAvailable,
            QuantityReserved = request.QuantityReserved,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.InventoryItems.Add(item);
        await dbContext.SaveChangesAsync();
        await RemoveInventoryCache(item.Id, item.ProductId);
        return item;
    }

    public async Task<bool> UpdateInventoryItemAsync(Guid inventoryItemId, UpdateInventoryItemRequest request)
    {
        var item = await dbContext.InventoryItems.FindAsync(inventoryItemId);
        if (item is null) return false;

        var oldProductId = item.ProductId;
        item.ProductId = request.ProductId;
        item.QuantityAvailable = request.QuantityAvailable;
        item.QuantityReserved = request.QuantityReserved;
        item.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        await RemoveInventoryCache(inventoryItemId, oldProductId);
        await RemoveInventoryCache(inventoryItemId, item.ProductId);
        return true;
    }

    public async Task<bool> UpdateInventoryQuantityAsync(Guid inventoryItemId, int quantityAvailable)
    {
        var item = await dbContext.InventoryItems.FindAsync(inventoryItemId);
        if (item is null) return false;

        item.QuantityAvailable = quantityAvailable;
        item.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        await RemoveInventoryCache(inventoryItemId, item.ProductId);
        return true;
    }

    public async Task<InventoryOperationResult> ReserveInventoryAsync(Guid inventoryItemId, int quantity)
    {
        var item = await dbContext.InventoryItems.FindAsync(inventoryItemId);
        if (item is null) return InventoryOperationResult.Missing();
        if (quantity > item.QuantityAvailable) return InventoryOperationResult.Failure("Not enough inventory available.");

        item.QuantityAvailable -= quantity;
        item.QuantityReserved += quantity;
        item.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        await RemoveInventoryCache(inventoryItemId, item.ProductId);
        return InventoryOperationResult.Success();
    }

    public async Task<InventoryOperationResult> ReleaseInventoryAsync(Guid inventoryItemId, int quantity)
    {
        var item = await dbContext.InventoryItems.FindAsync(inventoryItemId);
        if (item is null) return InventoryOperationResult.Missing();
        if (quantity > item.QuantityReserved) return InventoryOperationResult.Failure("Cannot release more inventory than is reserved.");

        item.QuantityAvailable += quantity;
        item.QuantityReserved -= quantity;
        item.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        await RemoveInventoryCache(inventoryItemId, item.ProductId);
        return InventoryOperationResult.Success();
    }

    public async Task<bool> DeleteInventoryItemAsync(Guid inventoryItemId)
    {
        var item = await dbContext.InventoryItems.FindAsync(inventoryItemId);
        if (item is null) return false;

        dbContext.InventoryItems.Remove(item);
        await dbContext.SaveChangesAsync();
        await RemoveInventoryCache(inventoryItemId, item.ProductId);
        return true;
    }

    private async Task RemoveInventoryCache(Guid inventoryItemId, Guid productId)
    {
        await cache.RemoveAsync("inventory:all");
        await cache.RemoveAsync($"inventory:{inventoryItemId}");
        await cache.RemoveAsync($"inventory:product:{productId}");
    }
}
