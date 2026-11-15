using Inventory.Api.Entities;

namespace Inventory.Api.Data;

public interface IInventoryRepository
{
    Task<IReadOnlyList<InventoryItem>> GetAllAsync();
    Task<InventoryItem?> GetByIdAsync(Guid inventoryItemId);
    Task<InventoryItem?> GetByProductIdAsync(Guid productId);
    Task AddAsync(InventoryItem item);
    Task UpdateAsync(InventoryItem item);
    Task DeleteAsync(InventoryItem item);
}