using Payments.Api.Entities;

namespace Payments.Api.Data;

public interface IPaymentsRepository
{
    Task<IReadOnlyList<Payment>> GetAllAsync();
    Task<Payment?> GetByIdAsync(Guid paymentId);
    Task<IReadOnlyList<Payment>> GetByOrderIdAsync(Guid orderId);
    Task AddAsync(Payment payment);
    Task UpdateAsync(Payment payment);
    Task UpdateRangeAsync(IEnumerable<Payment> payments);
}