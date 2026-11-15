using Orders.Api.Dtos;

namespace Orders.Api.Notifications;

public interface IOrderStatusNotificationSender
{
    Task SendAsync(OrderStatusChangedNotification notification, CancellationToken cancellationToken);
}