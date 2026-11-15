using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Dtos;
using Orders.Api.Entities;
using Orders.Api.Hubs;
using Orders.Api.Notifications;
using Xunit;

namespace Orders.Api.Tests;

public class OrderNotificationsTests
{
    [Fact]
    public void HubRequiresAuthentication()
    {
        var authorization = typeof(OrderNotificationsHub).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorization);
    }

    [Fact]
    public async Task OutboxPersistsNotificationsAndTracksDispatchFailures()
    {
        using var dbContext = new OrdersDbContext(new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var outbox = new OrderStatusOutboxRepository(dbContext);
        var notification = new OrderStatusChangedNotification(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Shipped",
            DateTime.UtcNow);
        outbox.Add(notification);
        await dbContext.SaveChangesAsync();

        var pending = Assert.Single(await outbox.GetPendingAsync(10, CancellationToken.None));
        Assert.Equal(notification.OrderId, pending.OrderId);

        await outbox.RecordFailedAttemptAsync(pending.Id, "temporary failure", CancellationToken.None);
        var retried = Assert.Single(await outbox.GetPendingAsync(10, CancellationToken.None));
        Assert.Equal(1, retried.Attempts);
        Assert.Equal("temporary failure", retried.LastError);

        await outbox.MarkDispatchedAsync(pending.Id, CancellationToken.None);
        Assert.Empty(await outbox.GetPendingAsync(10, CancellationToken.None));
    }

    [Fact]
    public async Task DispatcherSendsNotificationToOwnerAndMarksItDispatched()
    {
        await using var services = CreateServices();
        var notification = await AddNotificationAsync(services);
        var sender = new CapturingNotificationSender();
        var dispatcher = CreateDispatcher(services, sender);

        await dispatcher.DispatchPendingAsync(CancellationToken.None);

        var sent = Assert.Single(sender.Notifications);
        Assert.Equal(notification.OrderId, sent.OrderId);
        Assert.Equal(notification.UserId, sent.UserId);
        Assert.Equal("Shipped", sent.Status);
        await using var scope = services.CreateAsyncScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOrderStatusOutboxRepository>();
        Assert.Empty(await outbox.GetPendingAsync(10, CancellationToken.None));
    }

    [Fact]
    public async Task DispatcherRecordsFailedSendAndKeepsNotificationPending()
    {
        await using var services = CreateServices();
        await AddNotificationAsync(services);
        var sender = new CapturingNotificationSender(_ => throw new InvalidOperationException("temporary failure"));
        var dispatcher = CreateDispatcher(services, sender);

        await dispatcher.DispatchPendingAsync(CancellationToken.None);

        await using var scope = services.CreateAsyncScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOrderStatusOutboxRepository>();
        var pending = Assert.Single(await outbox.GetPendingAsync(10, CancellationToken.None));
        Assert.Equal(1, pending.Attempts);
        Assert.Equal("temporary failure", pending.LastError);
    }

    [Fact]
    public async Task OutboxStopsOfferingNotificationsAfterTenFailedAttempts()
    {
        using var dbContext = new OrdersDbContext(new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var outbox = new OrderStatusOutboxRepository(dbContext);
        outbox.Add(new OrderStatusChangedNotification(Guid.NewGuid(), Guid.NewGuid(), "Shipped", DateTime.UtcNow));
        await dbContext.SaveChangesAsync();

        var pending = Assert.Single(await outbox.GetPendingAsync(10, CancellationToken.None));
        for (var attempt = 0; attempt < 10; attempt++)
        {
            await outbox.RecordFailedAttemptAsync(pending.Id, "failure", CancellationToken.None);
        }

        Assert.Empty(await outbox.GetPendingAsync(10, CancellationToken.None));
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();
        services.AddDbContext<OrdersDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IOrderStatusOutboxRepository, OrderStatusOutboxRepository>();
        return services.BuildServiceProvider();
    }

    private static async Task<OrderStatusChangedNotification> AddNotificationAsync(ServiceProvider services)
    {
        var notification = new OrderStatusChangedNotification(Guid.NewGuid(), Guid.NewGuid(), "Shipped", DateTime.UtcNow);
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOrderStatusOutboxRepository>();
        outbox.Add(notification);
        await dbContext.SaveChangesAsync();
        return notification;
    }

    private static OrderStatusNotificationDispatcher CreateDispatcher(
        ServiceProvider services,
        IOrderStatusNotificationSender sender) =>
        new(services.GetRequiredService<IServiceScopeFactory>(), sender, NullLogger<OrderStatusNotificationDispatcher>.Instance);

    private sealed class CapturingNotificationSender(
        Action<OrderStatusChangedNotification>? onSend = null) : IOrderStatusNotificationSender
    {
        public List<OrderStatusChangedNotification> Notifications { get; } = [];

        public Task SendAsync(OrderStatusChangedNotification notification, CancellationToken cancellationToken)
        {
            onSend?.Invoke(notification);
            Notifications.Add(notification);
            return Task.CompletedTask;
        }
    }
}