using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Payments.Api.Controllers;
using Payments.Api.Data;
using Payments.Api.Dtos;
using Payments.Api.Entities;
using Payments.Api.Services;
using Xunit;

namespace Payments.Api.Tests;

public class PaymentsServiceTests
{
    [Fact]
    public async Task CreatePaymentAsync_PersistsPayment()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var payment = await service.CreatePaymentAsync(new CreatePaymentRequest
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Amount = 99,
            Currency = "USD",
            PaymentMethod = "Card",
            Status = "Pending"
        });

        Assert.NotEqual(Guid.Empty, payment.Id);
        Assert.Single(await dbContext.Payments.ToListAsync());
    }

    [Fact]
    public async Task UpdatePaymentStatusAsync_SetsCompletedAtWhenCompleted()
    {
        using var dbContext = CreateDbContext();
        var payment = new Payment { OrderId = Guid.NewGuid(), UserId = Guid.NewGuid(), Amount = 20, PaymentMethod = "Card" };
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var updated = await service.UpdatePaymentStatusAsync(payment.Id, new UpdatePaymentStatusRequest { Status = "Completed", ProviderReference = "provider-1" });

        Assert.True(updated);
        Assert.NotNull((await dbContext.Payments.FindAsync(payment.Id))!.CompletedAt);
    }

    [Fact]
    public async Task RefundPaymentAsync_RejectsAmountGreaterThanPayment()
    {
        using var dbContext = CreateDbContext();
        var payment = new Payment { OrderId = Guid.NewGuid(), UserId = Guid.NewGuid(), Amount = 20, PaymentMethod = "Card" };
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.RefundPaymentAsync(payment.Id, new RefundPaymentRequest { Amount = 30, Reason = "Too much" });

        Assert.False(result.IsSuccess);
    }

    private static PaymentsService CreateService(PaymentsDbContext dbContext) => new(dbContext, CreateCache());

    private static PaymentsDbContext CreateDbContext() => new(new DbContextOptionsBuilder<PaymentsDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}

public class PaymentsControllerIntegrationTests
{
    [Fact]
    public async Task RefundPayment_ReturnsAcceptedForValidRefund()
    {
        using var dbContext = CreateDbContext();
        var payment = new Payment { OrderId = Guid.NewGuid(), UserId = Guid.NewGuid(), Amount = 20, PaymentMethod = "Card" };
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();
        var controller = new PaymentsController(CreateService(dbContext));

        var result = await controller.RefundPayment(payment.Id, new RefundPaymentRequest { Amount = 5, Reason = "Customer request" });

        Assert.IsType<AcceptedResult>(result);
    }

    [Fact]
    public async Task GetPaymentById_ReturnsNotFoundForMissingPayment()
    {
        using var dbContext = CreateDbContext();
        var controller = new PaymentsController(CreateService(dbContext));

        var result = await controller.GetPaymentById(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    private static PaymentsService CreateService(PaymentsDbContext dbContext) => new(dbContext, CreateCache());

    private static PaymentsDbContext CreateDbContext() => new(new DbContextOptionsBuilder<PaymentsDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}
