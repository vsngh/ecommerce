namespace Cart.Api.Dtos;

public class CreateCartItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
