using Orders.Api.Dtos;
using Orders.Api.Entities;

namespace Orders.Api.Data;

public interface IOrderStatusOutboxRepository
{
    void Add(OrderStatusChangedNotification notification);
    Task<IReadOnlyList<OrderStatusNotificationOutbox>> GetPendingAsync(int batchSize, CancellationToken cancellationToken);
    Task MarkDispatchedAsync(Guid notificationId, CancellationToken cancellationToken);
    Task RecordFailedAttemptAsync(Guid notificationId, string error, CancellationToken cancellationToken);
}