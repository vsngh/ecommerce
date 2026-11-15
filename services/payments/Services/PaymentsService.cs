using Microsoft.Extensions.Caching.Distributed;
using Payments.Api.Data;
using Payments.Api.Dtos;
using Payments.Api.Entities;
using System.Text.Json;

namespace Payments.Api.Services;

public class PaymentsService : IPaymentsService
{
    private readonly IPaymentsRepository paymentsRepository;
    private readonly IDistributedCache cache;
    private static readonly DistributedCacheEntryOptions CacheOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };

    public PaymentsService(IPaymentsRepository paymentsRepository, IDistributedCache cache)
    {
        this.paymentsRepository = paymentsRepository;
        this.cache = cache;
    }

    public async Task<IReadOnlyList<Payment>> GetPaymentsAsync()
    {
        const string cacheKey = "payments:all";
        var cachedPayments = await cache.GetStringAsync(cacheKey);
        if (cachedPayments is not null) return JsonSerializer.Deserialize<List<Payment>>(cachedPayments) ?? [];

        var payments = await paymentsRepository.GetAllAsync();
        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(payments), CacheOptions);
        return payments;
    }

    public async Task<Payment?> GetPaymentByIdAsync(Guid paymentId)
    {
        var cacheKey = $"payments:{paymentId}";
        var cachedPayment = await cache.GetStringAsync(cacheKey);
        if (cachedPayment is not null) return JsonSerializer.Deserialize<Payment>(cachedPayment);

        var payment = await paymentsRepository.GetByIdAsync(paymentId);
        if (payment is not null) await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(payment), CacheOptions);
        return payment;
    }

    public async Task<IReadOnlyList<Payment>> GetPaymentsByOrderIdAsync(Guid orderId)
    {
        var cacheKey = $"payments:order:{orderId}";
        var cachedPayments = await cache.GetStringAsync(cacheKey);
        if (cachedPayments is not null) return JsonSerializer.Deserialize<List<Payment>>(cachedPayments) ?? [];

        var payments = await paymentsRepository.GetByOrderIdAsync(orderId);
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

        await paymentsRepository.AddAsync(payment);
        await RemovePaymentCache(payment.Id, payment.OrderId);
        return payment;
    }

    public async Task<bool> UpdatePaymentStatusAsync(Guid paymentId, UpdatePaymentStatusRequest request)
    {
        var payment = await paymentsRepository.GetByIdAsync(paymentId);
        if (payment is null) return false;

        payment.Status = request.Status;
        payment.ProviderReference = request.ProviderReference;
        if (request.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
        {
            payment.CompletedAt = DateTime.UtcNow;
        }

        await paymentsRepository.UpdateAsync(payment);
        await RemovePaymentCache(paymentId, payment.OrderId);
        return true;
    }

    public async Task<RefundPaymentResult> RefundPaymentAsync(Guid paymentId, RefundPaymentRequest request)
    {
        var payment = await paymentsRepository.GetByIdAsync(paymentId);
        if (payment is null) return RefundPaymentResult.Missing();
        if (request.Amount <= 0 || request.Amount > payment.Amount)
        {
            return RefundPaymentResult.Failure("Refund amount must be greater than zero and no more than the payment amount.");
        }

        payment.Status = "Refunded";
        await paymentsRepository.UpdateAsync(payment);
        await RemovePaymentCache(paymentId, payment.OrderId);
        return RefundPaymentResult.Success(payment);
    }

    public async Task<bool> RefundOrCancelOrderPaymentsAsync(Guid orderId, Guid userId)
    {
        var payments = await paymentsRepository.GetByOrderIdAsync(orderId);

        if (payments.Any(payment => payment.UserId != userId))
        {
            return false;
        }

        var changedPayments = new List<Payment>();
        foreach (var payment in payments)
        {
            if (payment.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                payment.Status = "Refunded";
                changedPayments.Add(payment);
            }
            else if (payment.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                payment.Status = "Cancelled";
                changedPayments.Add(payment);
            }
        }

        if (changedPayments.Count > 0)
        {
            await paymentsRepository.UpdateRangeAsync(changedPayments);
            foreach (var payment in changedPayments)
            {
                await RemovePaymentCache(payment.Id, payment.OrderId);
            }
        }

        return true;
    }

    private async Task RemovePaymentCache(Guid paymentId, Guid orderId)
    {
        await cache.RemoveAsync("payments:all");
        await cache.RemoveAsync($"payments:{paymentId}");
        await cache.RemoveAsync($"payments:order:{orderId}");
    }
}
