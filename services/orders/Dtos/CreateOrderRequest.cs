namespace Orders.Api.Dtos;

public class CreateOrderRequest
{
    public Guid UserId { get; set; }

    public string Status { get; set; } = "Pending";

    public ICollection<CreateOrderItemRequest> OrderItems { get; set; } = new List<CreateOrderItemRequest>();
}
