using Orders.Api.Entities;

namespace Orders.Api.Data;

public interface IOrdersRepository
{
    Task<IReadOnlyList<Order>> GetAllWithItemsAsync();
    Task<IReadOnlyList<Order>> GetByUserIdAsync(Guid userId);
    Task<Order?> GetByIdWithItemsAsync(Guid orderId);
    Task<Order?> GetByIdAsync(Guid orderId);
    Task<Order?> GetByIdForUserAsync(Guid orderId, Guid userId, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid orderId);
    Task<IReadOnlyList<OrderItem>> GetItemsAsync(Guid orderId);
    Task<OrderItem?> GetItemByIdAsync(Guid orderId, Guid orderItemId);
    Task AddAsync(Order order);
    Task AddRangeAsync(IEnumerable<Order> orders);
    Task AddItemAsync(OrderItem orderItem);
    Task UpdateAsync(CancellationToken cancellationToken = default);
    Task DeleteAsync(Order order);
    Task DeleteItemAsync(OrderItem orderItem);
}