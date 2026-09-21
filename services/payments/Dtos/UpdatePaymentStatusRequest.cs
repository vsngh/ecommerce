namespace Payments.Api.Dtos;

public class UpdatePaymentStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? ProviderReference { get; set; }
}
