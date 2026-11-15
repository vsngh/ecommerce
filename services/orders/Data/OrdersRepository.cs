using Microsoft.EntityFrameworkCore;
using Orders.Api.Entities;

namespace Orders.Api.Data;

public class OrdersRepository : IOrdersRepository
{
    private readonly OrdersDbContext dbContext;

    public OrdersRepository(OrdersDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Order>> GetAllWithItemsAsync() =>
        await dbContext.Orders.Include(order => order.OrderItems).ToListAsync();

    public async Task<IReadOnlyList<Order>> GetByUserIdAsync(Guid userId) =>
        await dbContext.Orders.AsNoTracking()
            .Where(order => order.UserId == userId)
            .Include(order => order.OrderItems)
            .OrderByDescending(order => order.OrderedAt)
            .ToListAsync();

    public Task<Order?> GetByIdWithItemsAsync(Guid orderId) =>
        dbContext.Orders.Include(order => order.OrderItems).FirstOrDefaultAsync(order => order.Id == orderId);

    public async Task<Order?> GetByIdAsync(Guid orderId) => await dbContext.Orders.FindAsync(orderId);

    public Task<Order?> GetByIdForUserAsync(Guid orderId, Guid userId, CancellationToken cancellationToken) =>
        dbContext.Orders.FirstOrDefaultAsync(
            order => order.Id == orderId && order.UserId == userId,
            cancellationToken);

    public Task<bool> ExistsAsync(Guid orderId) => dbContext.Orders.AnyAsync(order => order.Id == orderId);

    public async Task<IReadOnlyList<OrderItem>> GetItemsAsync(Guid orderId) =>
        await dbContext.OrderItems.Where(orderItem => orderItem.OrderId == orderId).ToListAsync();

    public Task<OrderItem?> GetItemByIdAsync(Guid orderId, Guid orderItemId) =>
        dbContext.OrderItems.FirstOrDefaultAsync(item => item.OrderId == orderId && item.Id == orderItemId);

    public async Task AddAsync(Order order)
    {
        await dbContext.Orders.AddAsync(order);
        await dbContext.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Order> orders)
    {
        await dbContext.Orders.AddRangeAsync(orders);
        await dbContext.SaveChangesAsync();
    }

    public async Task AddItemAsync(OrderItem orderItem)
    {
        await dbContext.OrderItems.AddAsync(orderItem);
        await dbContext.SaveChangesAsync();
    }

    public Task UpdateAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    public async Task DeleteAsync(Order order)
    {
        dbContext.Orders.Remove(order);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteItemAsync(OrderItem orderItem)
    {
        dbContext.OrderItems.Remove(orderItem);
        await dbContext.SaveChangesAsync();
    }
}