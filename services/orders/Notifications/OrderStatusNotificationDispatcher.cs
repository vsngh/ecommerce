using Microsoft.Extensions.DependencyInjection;
using Orders.Api.Data;
using Orders.Api.Dtos;

namespace Orders.Api.Notifications;

public sealed class OrderStatusNotificationDispatcher
{
    private const int BatchSize = 50;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IOrderStatusNotificationSender sender;
    private readonly ILogger<OrderStatusNotificationDispatcher> logger;

    public OrderStatusNotificationDispatcher(
        IServiceScopeFactory scopeFactory,
        IOrderStatusNotificationSender sender,
        ILogger<OrderStatusNotificationDispatcher> logger)
    {
        this.scopeFactory = scopeFactory;
        this.sender = sender;
        this.logger = logger;
    }

    public async Task DispatchPendingAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOrderStatusOutboxRepository>();
        var pendingNotifications = await outbox.GetPendingAsync(BatchSize, cancellationToken);

        foreach (var pending in pendingNotifications)
        {
            try
            {
                var notification = new OrderStatusChangedNotification(
                    pending.OrderId,
                    pending.UserId,
                    pending.Status,
                    pending.OccurredAt);
                await sender.SendAsync(notification, cancellationToken);
                await outbox.MarkDispatchedAsync(pending.Id, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to send an order status notification for order {OrderId}.", pending.OrderId);
                await outbox.RecordFailedAttemptAsync(pending.Id, exception.Message, cancellationToken);
            }
        }
    }
}