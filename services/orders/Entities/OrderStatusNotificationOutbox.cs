namespace Orders.Api.Entities;

public sealed class OrderStatusNotificationOutbox
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DispatchedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}