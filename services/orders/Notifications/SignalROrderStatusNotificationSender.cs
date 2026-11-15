using Microsoft.AspNetCore.SignalR;
using Orders.Api.Dtos;
using Orders.Api.Hubs;

namespace Orders.Api.Notifications;

public sealed class SignalROrderStatusNotificationSender : IOrderStatusNotificationSender
{
    private readonly IHubContext<OrderNotificationsHub> hubContext;

    public SignalROrderStatusNotificationSender(IHubContext<OrderNotificationsHub> hubContext)
    {
        this.hubContext = hubContext;
    }

    public Task SendAsync(OrderStatusChangedNotification notification, CancellationToken cancellationToken) =>
        hubContext.Clients.User(notification.UserId.ToString()).SendAsync(
            "OrderStatusChanged",
            notification,
            cancellationToken);
}