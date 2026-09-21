using Payments.Api.Dtos;
using Payments.Api.Entities;

namespace Payments.Api.Services;

public interface IPaymentsService
{
    Task<IReadOnlyList<Payment>> GetPaymentsAsync();
    Task<Payment?> GetPaymentByIdAsync(Guid paymentId);
    Task<IReadOnlyList<Payment>> GetPaymentsByOrderIdAsync(Guid orderId);
    Task<Payment> CreatePaymentAsync(CreatePaymentRequest request);
    Task<bool> UpdatePaymentStatusAsync(Guid paymentId, UpdatePaymentStatusRequest request);
    Task<RefundPaymentResult> RefundPaymentAsync(Guid paymentId, RefundPaymentRequest request);
}
