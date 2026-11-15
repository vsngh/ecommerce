using Microsoft.EntityFrameworkCore;
using Orders.Api.Dtos;
using Orders.Api.Entities;

namespace Orders.Api.Data;

public sealed class OrderStatusOutboxRepository : IOrderStatusOutboxRepository
{
    private const int MaxAttempts = 10;
    private readonly OrdersDbContext dbContext;

    public OrderStatusOutboxRepository(OrdersDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public void Add(OrderStatusChangedNotification notification)
    {
        dbContext.OrderStatusNotifications.Add(new OrderStatusNotificationOutbox
        {
            OrderId = notification.OrderId,
            UserId = notification.UserId,
            Status = notification.Status,
            OccurredAt = notification.OccurredAt
        });
    }

    public async Task<IReadOnlyList<OrderStatusNotificationOutbox>> GetPendingAsync(
        int batchSize,
        CancellationToken cancellationToken) =>
        await dbContext.OrderStatusNotifications
            .AsNoTracking()
            .Where(notification => notification.DispatchedAt == null && notification.Attempts < MaxAttempts)
            .OrderBy(notification => notification.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public async Task MarkDispatchedAsync(Guid notificationId, CancellationToken cancellationToken)
    {
        var notification = await dbContext.OrderStatusNotifications.FindAsync([notificationId], cancellationToken);
        if (notification is null) return;

        notification.DispatchedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordFailedAttemptAsync(Guid notificationId, string error, CancellationToken cancellationToken)
    {
        var notification = await dbContext.OrderStatusNotifications.FindAsync([notificationId], cancellationToken);
        if (notification is null) return;

        notification.Attempts++;
        notification.LastError = error.Length <= 1000 ? error : error[..1000];
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}