namespace Orders.Api.Dtos;

public class UpdateOrderItemRequest
{
    public Guid ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}
