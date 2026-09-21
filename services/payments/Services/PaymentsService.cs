using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Payments.Api.Data;
using Payments.Api.Dtos;
using Payments.Api.Entities;
using System.Text.Json;

namespace Payments.Api.Services;

public class PaymentsService : IPaymentsService
{
    private readonly PaymentsDbContext dbContext;
    private readonly IDistributedCache cache;
    private static readonly DistributedCacheEntryOptions CacheOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };

    public PaymentsService(PaymentsDbContext dbContext, IDistributedCache cache)
    {
        this.dbContext = dbContext;
        this.cache = cache;
    }

    public async Task<IReadOnlyList<Payment>> GetPaymentsAsync()
    {
        const string cacheKey = "payments:all";
        var cachedPayments = await cache.GetStringAsync(cacheKey);
        if (cachedPayments is not null) return JsonSerializer.Deserialize<List<Payment>>(cachedPayments) ?? [];

        var payments = await dbContext.Payments.ToListAsync();
        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(payments), CacheOptions);
        return payments;
    }

    public async Task<Payment?> GetPaymentByIdAsync(Guid paymentId)
    {
        var cacheKey = $"payments:{paymentId}";
        var cachedPayment = await cache.GetStringAsync(cacheKey);
        if (cachedPayment is not null) return JsonSerializer.Deserialize<Payment>(cachedPayment);

        var payment = await dbContext.Payments.FindAsync(paymentId);
        if (payment is not null) await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(payment), CacheOptions);
        return payment;
    }

    public async Task<IReadOnlyList<Payment>> GetPaymentsByOrderIdAsync(Guid orderId)
    {
        var cacheKey = $"payments:order:{orderId}";
        var cachedPayments = await cache.GetStringAsync(cacheKey);
        if (cachedPayments is not null) return JsonSerializer.Deserialize<List<Payment>>(cachedPayments) ?? [];

        var payments = await dbContext.Payments.Where(payment => payment.OrderId == orderId).ToListAsync();
        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(payments), CacheOptions);
        return payments;
    }

    public async Task<Payment> CreatePaymentAsync(CreatePaymentRequest request)
    {
        var payment = new Payment
        {
            OrderId = request.OrderId,
            UserId = request.UserId,
            Amount = request.Amount,
            Currency = request.Currency,
            PaymentMethod = request.PaymentMethod,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();
        await RemovePaymentCache(payment.Id, payment.OrderId);
        return payment;
    }

    public async Task<bool> UpdatePaymentStatusAsync(Guid paymentId, UpdatePaymentStatusRequest request)
    {
        var payment = await dbContext.Payments.FindAsync(paymentId);
        if (payment is null) return false;

        payment.Status = request.Status;
        payment.ProviderReference = request.ProviderReference;
        if (request.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
        {
            payment.CompletedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();
        await RemovePaymentCache(paymentId, payment.OrderId);
        return true;
    }

    public async Task<RefundPaymentResult> RefundPaymentAsync(Guid paymentId, RefundPaymentRequest request)
    {
        var payment = await dbContext.Payments.FindAsync(paymentId);
        if (payment is null) return RefundPaymentResult.Missing();
        if (request.Amount <= 0 || request.Amount > payment.Amount)
        {
            return RefundPaymentResult.Failure("Refund amount must be greater than zero and no more than the payment amount.");
        }

        payment.Status = "Refunded";
        await dbContext.SaveChangesAsync();
        await RemovePaymentCache(paymentId, payment.OrderId);
        return RefundPaymentResult.Success(payment);
    }

    private async Task RemovePaymentCache(Guid paymentId, Guid orderId)
    {
        await cache.RemoveAsync("payments:all");
        await cache.RemoveAsync($"payments:{paymentId}");
        await cache.RemoveAsync($"payments:order:{orderId}");
    }
}
