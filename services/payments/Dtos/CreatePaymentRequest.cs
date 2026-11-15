namespace Payments.Api.Dtos;

public class CreatePaymentRequest
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
}
