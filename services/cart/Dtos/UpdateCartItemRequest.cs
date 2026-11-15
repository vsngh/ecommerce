namespace Cart.Api.Dtos;

public class UpdateCartItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
