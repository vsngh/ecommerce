namespace Orders.Api.Entities;

public class Order
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateTime OrderedAt { get; set; } = DateTime.UtcNow;

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = "Pending";

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
