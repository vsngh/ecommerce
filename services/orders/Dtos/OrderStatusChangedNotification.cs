namespace Orders.Api.Dtos;

public record OrderStatusChangedNotification(Guid OrderId, Guid UserId, string Status, DateTime OccurredAt);