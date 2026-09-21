namespace Payments.Api.Dtos;

public class RefundPaymentRequest
{
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
}
