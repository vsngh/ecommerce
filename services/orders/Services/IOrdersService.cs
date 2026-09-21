using Orders.Api.Entities;
using Orders.Api.Dtos;

namespace Orders.Api.Services;

public interface IOrdersService
{
    Task<IReadOnlyList<Order>> GetOrdersAsync();

    Task<Order?> GetOrderByIdAsync(Guid orderId);

    Task<Order> CreateOrderAsync(CreateOrderRequest request);

    Task<BulkOrderUploadResult> BulkUploadOrdersAsync(BulkOrderUploadRequest request);

    Task<bool> UpdateOrderAsync(Guid orderId, UpdateOrderRequest request);

    Task<bool> UpdateOrderStatusAsync(Guid orderId, string status);

    Task<bool> DeleteOrderAsync(Guid orderId);

    Task<IReadOnlyList<OrderItem>?> GetOrderItemsAsync(Guid orderId);

    Task<OrderItem?> GetOrderItemByIdAsync(Guid orderId, Guid orderItemId);

    Task<OrderItem?> AddOrderItemAsync(Guid orderId, CreateOrderItemRequest request);

    Task<bool> UpdateOrderItemAsync(Guid orderId, Guid orderItemId, UpdateOrderItemRequest request);

    Task<bool> RemoveOrderItemAsync(Guid orderId, Guid orderItemId);
}
