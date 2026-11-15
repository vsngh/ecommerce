using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Orders.Api.Hubs;

[Authorize]
public sealed class OrderNotificationsHub : Hub
{
	public const string Route = "/api/orders/hubs/notifications";
}