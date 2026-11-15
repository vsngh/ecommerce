namespace Orders.Api.Payments;

public interface IPaymentsClient
{
    Task<PaymentsClientResult> RefundOrCancelOrderPaymentsAsync(
        Guid orderId,
        Guid userId,
        CancellationToken cancellationToken);
}

public enum PaymentsClientResult
{
    Success,
    NotFound,
    Unavailable
}
