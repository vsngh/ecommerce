using Microsoft.EntityFrameworkCore;
using Payments.Api.Entities;

namespace Payments.Api.Data;

public class PaymentsRepository : IPaymentsRepository
{
    private readonly PaymentsDbContext dbContext;

    public PaymentsRepository(PaymentsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Payment>> GetAllAsync() => await dbContext.Payments.ToListAsync();

    public Task<Payment?> GetByIdAsync(Guid paymentId) => dbContext.Payments.FindAsync(paymentId).AsTask();

    public async Task<IReadOnlyList<Payment>> GetByOrderIdAsync(Guid orderId) =>
        await dbContext.Payments.Where(payment => payment.OrderId == orderId).ToListAsync();

    public async Task AddAsync(Payment payment)
    {
        await dbContext.Payments.AddAsync(payment);
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Payment payment)
    {
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateRangeAsync(IEnumerable<Payment> payments)
    {
        await dbContext.SaveChangesAsync();
    }
}