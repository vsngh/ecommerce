namespace Payments.Api.Dtos;

using Payments.Api.Entities;

public record RefundPaymentResult(bool IsSuccess, bool NotFound, string? Error, Payment? Payment)
{
    public static RefundPaymentResult Success(Payment payment) => new(true, false, null, payment);
    public static RefundPaymentResult Missing() => new(false, true, null, null);
    public static RefundPaymentResult Failure(string error) => new(false, false, error, null);
}
