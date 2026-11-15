using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Payments.Api.Controllers;
using Payments.Api.Data;
using Payments.Api.Dtos;
using Payments.Api.Entities;
using Payments.Api.Security;
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
            PaymentMethod = "Card",
            Status = "Pending"
        });

        Assert.NotEqual(Guid.Empty, payment.Id);
        Assert.Equal("INR", payment.Currency);
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

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(30)]
    public async Task RefundPaymentAsync_RejectsInvalidAmountWithoutChangingPayment(decimal amount)
    {
        using var dbContext = CreateDbContext();
        var payment = new Payment { OrderId = Guid.NewGuid(), UserId = Guid.NewGuid(), Amount = 20, PaymentMethod = "Card" };
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.RefundPaymentAsync(payment.Id, new RefundPaymentRequest { Amount = amount, Reason = "Invalid amount" });

        Assert.False(result.IsSuccess);
        Assert.Equal("Pending", (await dbContext.Payments.FindAsync(payment.Id))!.Status);
    }

    [Fact]
    public async Task RefundOrCancelOrderPaymentsAsync_RecordsCompletedAndPendingStates()
    {
        using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var completedPayment = new Payment { OrderId = orderId, UserId = userId, Amount = 20, PaymentMethod = "Card", Status = "Completed" };
        var pendingPayment = new Payment { OrderId = orderId, UserId = userId, Amount = 10, PaymentMethod = "Card", Status = "Pending" };
        dbContext.Payments.AddRange(completedPayment, pendingPayment);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var updated = await service.RefundOrCancelOrderPaymentsAsync(orderId, userId);

        Assert.True(updated);
        Assert.Equal("Refunded", (await dbContext.Payments.FindAsync(completedPayment.Id))!.Status);
        Assert.Equal("Cancelled", (await dbContext.Payments.FindAsync(pendingPayment.Id))!.Status);
        Assert.True(await service.RefundOrCancelOrderPaymentsAsync(orderId, userId));
    }

    [Fact]
    public async Task RefundOrCancelOrderPaymentsAsync_RejectsPaymentsOwnedByAnotherUser()
    {
        using var dbContext = CreateDbContext();
        var payment = new Payment
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Amount = 20,
            PaymentMethod = "Card",
            Status = "Completed"
        };
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var updated = await service.RefundOrCancelOrderPaymentsAsync(payment.OrderId, Guid.NewGuid());

        Assert.False(updated);
        Assert.Equal("Completed", (await dbContext.Payments.FindAsync(payment.Id))!.Status);
    }

    private static PaymentsService CreateService(PaymentsDbContext dbContext) => new(new PaymentsRepository(dbContext), CreateCache());

    private static PaymentsDbContext CreateDbContext() => new(new DbContextOptionsBuilder<PaymentsDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}

public class PaymentsControllerIntegrationTests
{
    [Fact]
    public void PaymentReadCreateAndStatusRoutesRequireTheirPolicies()
    {
        Assert.NotNull(typeof(PaymentsController).GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal(PermissionPolicies.PaymentsRead, GetPolicy(nameof(PaymentsController.GetPayments)));
        Assert.Equal(PermissionPolicies.PaymentsCreate, GetPolicy(nameof(PaymentsController.CreatePayment)));
        Assert.Equal(PermissionPolicies.PaymentsManage, GetPolicy(nameof(PaymentsController.UpdatePaymentStatus)));
    }

    [Fact]
    public void RefundEndpoints_RequireAuthenticationOrRefundPermission()
    {
        var orderRefund = typeof(PaymentsController)
            .GetMethod(nameof(PaymentsController.RefundOrCancelOrderPayments))!
            .GetCustomAttribute<AuthorizeAttribute>();
        var administrativeRefund = typeof(PaymentsController)
            .GetMethod(nameof(PaymentsController.RefundPayment))!
            .GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(orderRefund);
        Assert.Equal(PermissionPolicies.OrderCancellation, orderRefund!.Policy);
        Assert.Equal(PermissionPolicies.PaymentsRefund, administrativeRefund?.Policy);
    }

    [Fact]
    public async Task RefundOrCancelOrderPayments_ReturnsUnauthorizedWithoutUserIdentity()
    {
        using var dbContext = CreateDbContext();
        var controller = new PaymentsController(CreateService(dbContext))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };

        var result = await controller.RefundOrCancelOrderPayments(Guid.NewGuid());

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task RefundOrCancelOrderPayments_RejectsPaymentsOwnedByAnotherUser()
    {
        using var dbContext = CreateDbContext();
        var payment = new Payment
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Amount = 20,
            PaymentMethod = "Card",
            Status = "Completed"
        };
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();
        var controller = new PaymentsController(CreateService(dbContext))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
                        "Test"))
                }
            }
        };

        var result = await controller.RefundOrCancelOrderPayments(payment.OrderId);

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal("Completed", (await dbContext.Payments.FindAsync(payment.Id))!.Status);
    }

    [Fact]
    public async Task RefundOrCancelOrderPayments_RecordsRefundForAuthenticatedOwner()
    {
        using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var payment = new Payment
        {
            OrderId = Guid.NewGuid(),
            UserId = userId,
            Amount = 20,
            PaymentMethod = "Card",
            Status = "Completed"
        };
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();
        var controller = new PaymentsController(CreateService(dbContext))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                        "Test"))
                }
            }
        };

        var result = await controller.RefundOrCancelOrderPayments(payment.OrderId);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("Refunded", (await dbContext.Payments.FindAsync(payment.Id))!.Status);
    }

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

    [Fact]
    public async Task GetPaymentById_ReturnsNotFoundForAnotherUsersPayment()
    {
        using var dbContext = CreateDbContext();
        var payment = new Payment { OrderId = Guid.NewGuid(), UserId = Guid.NewGuid(), Amount = 20, PaymentMethod = "Card" };
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext, Guid.NewGuid());

        var result = await controller.GetPaymentById(payment.Id);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreatePayment_UsesAuthenticatedUserAndPendingStatus()
    {
        using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var controller = CreateController(dbContext, userId);

        var result = await controller.CreatePayment(new CreatePaymentRequest
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Amount = 20,
            PaymentMethod = "Card",
            Status = "Completed"
        });

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var payment = Assert.IsType<Payment>(created.Value);
        Assert.Equal(userId, payment.UserId);
        Assert.Equal("Pending", payment.Status);
    }

    private static string? GetPolicy(string actionName) => typeof(PaymentsController)
        .GetMethod(actionName)!
        .GetCustomAttribute<AuthorizeAttribute>()?.Policy;

    private static PaymentsController CreateController(PaymentsDbContext dbContext, Guid userId) =>
        new(CreateService(dbContext))
        {
            ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                        "Test"))
                }
            }
        };

    private static PaymentsService CreateService(PaymentsDbContext dbContext) => new(new PaymentsRepository(dbContext), CreateCache());

    private static PaymentsDbContext CreateDbContext() => new(new DbContextOptionsBuilder<PaymentsDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}
