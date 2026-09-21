using Inventory.Api.Dtos;
using Inventory.Api.Entities;

namespace Inventory.Api.Services;

public interface IInventoryService
{
    Task<IReadOnlyList<InventoryItem>> GetInventoryItemsAsync();
    Task<InventoryItem?> GetInventoryItemByIdAsync(Guid inventoryItemId);
    Task<InventoryItem?> GetInventoryByProductIdAsync(Guid productId);
    Task<InventoryItem> CreateInventoryItemAsync(CreateInventoryItemRequest request);
    Task<bool> UpdateInventoryItemAsync(Guid inventoryItemId, UpdateInventoryItemRequest request);
    Task<bool> UpdateInventoryQuantityAsync(Guid inventoryItemId, int quantityAvailable);
    Task<InventoryOperationResult> ReserveInventoryAsync(Guid inventoryItemId, int quantity);
    Task<InventoryOperationResult> ReleaseInventoryAsync(Guid inventoryItemId, int quantity);
    Task<bool> DeleteInventoryItemAsync(Guid inventoryItemId);
}
