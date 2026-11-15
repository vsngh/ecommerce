using Microsoft.EntityFrameworkCore;
using Inventory.Api.Entities;

namespace Inventory.Api.Data;

public class InventoryRepository : IInventoryRepository
{
    private readonly InventoryDbContext dbContext;

    public InventoryRepository(InventoryDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<InventoryItem>> GetAllAsync() => await dbContext.InventoryItems.ToListAsync();

    public Task<InventoryItem?> GetByIdAsync(Guid inventoryItemId) => dbContext.InventoryItems.FindAsync(inventoryItemId).AsTask();

    public Task<InventoryItem?> GetByProductIdAsync(Guid productId) =>
        dbContext.InventoryItems.FirstOrDefaultAsync(item => item.ProductId == productId);

    public async Task AddAsync(InventoryItem item)
    {
        await dbContext.InventoryItems.AddAsync(item);
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(InventoryItem item)
    {
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(InventoryItem item)
    {
        dbContext.InventoryItems.Remove(item);
        await dbContext.SaveChangesAsync();
    }
}