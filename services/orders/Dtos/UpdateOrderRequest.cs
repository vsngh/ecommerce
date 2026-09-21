namespace Orders.Api.Dtos;

public class UpdateOrderRequest
{
    public Guid UserId { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = "Pending";
}
