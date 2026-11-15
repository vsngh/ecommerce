namespace Cart.Api.Dtos;

public class CheckoutRequest
{
    public Guid UserId { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
}
