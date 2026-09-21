namespace Payments.Api.Entities;

public class Payment
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid UserId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "USD";

    public string PaymentMethod { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";

    public string? ProviderReference { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }
}
