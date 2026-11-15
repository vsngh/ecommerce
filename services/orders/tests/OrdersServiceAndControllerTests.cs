using System.Security.Claims;
using System.Reflection;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Orders.Api.Controllers;
using Orders.Api.Data;
using Orders.Api.Dtos;
using Orders.Api.Entities;
using Orders.Api.Payments;
using Orders.Api.Security;
using Orders.Api.Services;
using Xunit;

namespace Orders.Api.Tests;

public class OrdersServiceTests
{
    [Fact]
    public async Task GetOrdersByUserIdAsync_ReturnsOnlyUsersOrdersNewestFirstWithItems()
    {
        using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var otherUserOrder = new Order { UserId = Guid.NewGuid(), OrderedAt = DateTime.UtcNow };
        var olderOrder = new Order { UserId = userId, OrderedAt = DateTime.UtcNow.AddDays(-1) };
        var newerOrder = new Order { UserId = userId, OrderedAt = DateTime.UtcNow };
        newerOrder.OrderItems.Add(new OrderItem
        {
            ProductId = Guid.NewGuid(),
            Quantity = 2,
            UnitPrice = 12.5m
        });
        dbContext.Orders.AddRange(otherUserOrder, olderOrder, newerOrder);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var orders = await service.GetOrdersByUserIdAsync(userId);

        Assert.Equal([newerOrder.Id, olderOrder.Id], orders.Select(order => order.Id));
        Assert.Single(orders[0].OrderItems);
    }

    [Fact]
    public async Task CreateOrderAsync_ComputesTotalAndPersistsItems()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var order = await service.CreateOrderAsync(new CreateOrderRequest
        {
            UserId = Guid.NewGuid(),
            OrderItems =
            [
                new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 2, UnitPrice = 15 },
                new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 10 }
            ]
        });

        Assert.Equal(40, order.TotalAmount);
        Assert.Equal(2, order.OrderItems.Count);
    }

    [Fact]
    public async Task GetOrdersAsync_ReturnsOrdersWithItems()
    {
        using var dbContext = CreateDbContext();
        var order = new Order
        {
            UserId = Guid.NewGuid(),
            OrderItems = [new OrderItem { ProductId = Guid.NewGuid(), Quantity = 2, UnitPrice = 6 }]
        };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var orders = await service.GetOrdersAsync();

        var savedOrder = Assert.Single(orders);
        Assert.Equal(order.Id, savedOrder.Id);
        Assert.Single(savedOrder.OrderItems);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ReturnsFalseWhenOrderMissing()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var updated = await service.UpdateOrderStatusAsync(Guid.NewGuid(), "Completed");

        Assert.False(updated);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_UpdatesExistingOrder()
    {
        using var dbContext = CreateDbContext();
        var order = new Order { UserId = Guid.NewGuid(), Status = "Pending" };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var updated = await service.UpdateOrderStatusAsync(order.Id, "Shipped");

        Assert.True(updated);
        Assert.Equal("Shipped", (await dbContext.Orders.FindAsync(order.Id))!.Status);
        var notification = Assert.Single(await dbContext.OrderStatusNotifications.ToListAsync());
        Assert.Equal(order.Id, notification.OrderId);
        Assert.Equal(order.UserId, notification.UserId);
        Assert.Equal("Shipped", notification.Status);
    }

    [Fact]
    public async Task CancelOrderAsync_CancelsPendingOrderAfterPaymentOperationSucceeds()
    {
        using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var order = new Order { UserId = userId, Status = "Pending" };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var paymentsClient = new FakePaymentsClient(PaymentsClientResult.Success);
        var service = CreateService(dbContext, paymentsClient);

        var result = await service.CancelOrderAsync(order.Id, userId, CancellationToken.None);

        Assert.Equal(OrderCancellationResult.Cancelled, result);
        Assert.Equal("Cancelled", (await dbContext.Orders.FindAsync(order.Id))!.Status);
        Assert.Equal(order.Id, paymentsClient.LastOrderId);
        Assert.Equal(userId, paymentsClient.LastUserId);
        Assert.Equal("Cancelled", Assert.Single(await dbContext.OrderStatusNotifications.ToListAsync()).Status);
    }

    [Fact]
    public async Task CancelOrderAsync_LeavesOrderPendingWhenPaymentsIsUnavailable()
    {
        using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var order = new Order { UserId = userId, Status = "Pending" };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var service = CreateService(
            dbContext,
            new FakePaymentsClient(PaymentsClientResult.Unavailable));

        var result = await service.CancelOrderAsync(order.Id, userId, CancellationToken.None);

        Assert.Equal(OrderCancellationResult.PaymentsUnavailable, result);
        Assert.Equal("Pending", (await dbContext.Orders.FindAsync(order.Id))!.Status);
        Assert.Empty(await dbContext.OrderStatusNotifications.ToListAsync());
    }

    [Fact]
    public async Task CancelOrderAsync_DoesNotCallPaymentsForAnotherUsersOrder()
    {
        using var dbContext = CreateDbContext();
        var order = new Order { UserId = Guid.NewGuid(), Status = "Pending" };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var paymentsClient = new FakePaymentsClient(PaymentsClientResult.Success);
        var service = CreateService(dbContext, paymentsClient);

        var result = await service.CancelOrderAsync(order.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(OrderCancellationResult.NotFound, result);
        Assert.False(paymentsClient.WasCalled);
        Assert.Equal("Pending", (await dbContext.Orders.FindAsync(order.Id))!.Status);
    }

    [Fact]
    public async Task CancelOrderAsync_RejectsOrdersThatAreNotPending()
    {
        using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var order = new Order { UserId = userId, Status = "Shipped" };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var paymentsClient = new FakePaymentsClient(PaymentsClientResult.Success);
        var service = CreateService(dbContext, paymentsClient);

        var result = await service.CancelOrderAsync(order.Id, userId, CancellationToken.None);

        Assert.Equal(OrderCancellationResult.Conflict, result);
        Assert.False(paymentsClient.WasCalled);
        Assert.Equal("Shipped", (await dbContext.Orders.FindAsync(order.Id))!.Status);
    }

    [Fact]
    public async Task AddOrderItemAsync_AddsItemToExistingOrder()
    {
        using var dbContext = CreateDbContext();
        var order = new Order { UserId = Guid.NewGuid(), TotalAmount = 10, Status = "Pending" };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var item = await service.AddOrderItemAsync(order.Id, new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 10 });

        Assert.NotNull(item);
        Assert.Single(await dbContext.OrderItems.ToListAsync());
    }

    [Fact]
    public async Task UpdateOrderAsync_UpdatesExistingOrder()
    {
        using var dbContext = CreateDbContext();
        var order = new Order { UserId = Guid.NewGuid(), TotalAmount = 10, Status = "Pending" };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);
        var userId = Guid.NewGuid();

        var updated = await service.UpdateOrderAsync(order.Id, new UpdateOrderRequest
        {
            UserId = userId,
            TotalAmount = 25,
            Status = "Processing"
        });

        Assert.True(updated);
        var savedOrder = await dbContext.Orders.FindAsync(order.Id);
        Assert.Equal(userId, savedOrder!.UserId);
        Assert.Equal(25, savedOrder.TotalAmount);
        Assert.Equal("Processing", savedOrder.Status);
        var notification = Assert.Single(await dbContext.OrderStatusNotifications.ToListAsync());
        Assert.Equal("Processing", notification.Status);
        Assert.Equal(userId, notification.UserId);
    }

    [Fact]
    public async Task UpdateOrderAsync_DoesNotNotifyWhenStatusIsUnchanged()
    {
        using var dbContext = CreateDbContext();
        var order = new Order { UserId = Guid.NewGuid(), TotalAmount = 10, Status = "Pending" };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var updated = await service.UpdateOrderAsync(order.Id, new UpdateOrderRequest
        {
            UserId = order.UserId,
            TotalAmount = 20,
            Status = "pending"
        });

        Assert.True(updated);
        Assert.Empty(await dbContext.OrderStatusNotifications.ToListAsync());
    }

    [Fact]
    public async Task DeleteOrderAsync_RemovesExistingOrder()
    {
        using var dbContext = CreateDbContext();
        var order = new Order { UserId = Guid.NewGuid() };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var deleted = await service.DeleteOrderAsync(order.Id);

        Assert.True(deleted);
        Assert.Null(await dbContext.Orders.FindAsync(order.Id));
    }

    [Fact]
    public async Task GetOrderItemsAsync_ReturnsNullWhenOrderDoesNotExist()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var items = await service.GetOrderItemsAsync(Guid.NewGuid());

        Assert.Null(items);
    }

    [Fact]
    public async Task GetOrderItemsAsync_ReturnsItemsForExistingOrder()
    {
        using var dbContext = CreateDbContext();
        var order = new Order
        {
            UserId = Guid.NewGuid(),
            OrderItems = [new OrderItem { ProductId = Guid.NewGuid(), Quantity = 2, UnitPrice = 6 }]
        };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var items = await service.GetOrderItemsAsync(order.Id);

        Assert.NotNull(items);
        Assert.Single(items!);
    }

    [Fact]
    public async Task GetOrderItemByIdAsync_ReturnsMatchingItem()
    {
        using var dbContext = CreateDbContext();
        var item = new OrderItem { ProductId = Guid.NewGuid(), Quantity = 2, UnitPrice = 6 };
        var order = new Order { UserId = Guid.NewGuid(), OrderItems = [item] };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var savedItem = await service.GetOrderItemByIdAsync(order.Id, item.Id);

        Assert.NotNull(savedItem);
        Assert.Equal(item.Id, savedItem!.Id);
        Assert.Equal(2, savedItem.Quantity);
    }

    [Fact]
    public async Task AddOrderItemAsync_ReturnsNullWhenOrderDoesNotExist()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var item = await service.AddOrderItemAsync(Guid.NewGuid(), new CreateOrderItemRequest
        {
            ProductId = Guid.NewGuid(),
            Quantity = 1,
            UnitPrice = 10
        });

        Assert.Null(item);
    }

    [Fact]
    public async Task UpdateOrderItemAsync_UpdatesExistingItem()
    {
        using var dbContext = CreateDbContext();
        var item = new OrderItem { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 10 };
        var order = new Order { UserId = Guid.NewGuid(), OrderItems = [item] };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);
        var productId = Guid.NewGuid();

        var updated = await service.UpdateOrderItemAsync(order.Id, item.Id, new UpdateOrderItemRequest
        {
            ProductId = productId,
            Quantity = 4,
            UnitPrice = 7
        });

        Assert.True(updated);
        var savedItem = await dbContext.OrderItems.FindAsync(item.Id);
        Assert.Equal(productId, savedItem!.ProductId);
        Assert.Equal(4, savedItem.Quantity);
        Assert.Equal(7, savedItem.UnitPrice);
    }

    [Fact]
    public async Task RemoveOrderItemAsync_RemovesExistingItem()
    {
        using var dbContext = CreateDbContext();
        var item = new OrderItem { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 10 };
        var order = new Order { UserId = Guid.NewGuid(), OrderItems = [item] };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var removed = await service.RemoveOrderItemAsync(order.Id, item.Id);

        Assert.True(removed);
        Assert.Null(await dbContext.OrderItems.FindAsync(item.Id));
    }

    [Fact]
    public async Task BulkUploadOrdersAsync_RequiresFile()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.BulkUploadOrdersAsync(new BulkOrderUploadRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal("An Excel file is required.", result.Error);
    }

    [Fact]
    public async Task BulkUploadOrdersAsync_RejectsUnsupportedFileExtension()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        using var stream = new MemoryStream([1]);
        var file = new FormFile(stream, 0, stream.Length, "file", "orders.csv");

        var result = await service.BulkUploadOrdersAsync(new BulkOrderUploadRequest { File = file });

        Assert.False(result.IsSuccess);
        Assert.Equal("Only .xlsx files are supported.", result.Error);
    }

    [Fact]
    public async Task BulkUploadOrdersAsync_GroupsRowsAndPersistsConfiguredBatches()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var userId = Guid.NewGuid();
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        using var stream = CreateWorkbook(worksheet =>
        {
            WriteBulkOrderHeaders(worksheet);
            WriteBulkOrderRow(worksheet, 2, "order-a", userId, firstProductId, 2, 12.5m, "Pending");
            WriteBulkOrderRow(worksheet, 3, "order-a", userId, secondProductId, 1, 5m, "Shipped");
            WriteBulkOrderRow(worksheet, 4, "order-b", Guid.NewGuid(), Guid.NewGuid(), 1, 7.5m, "");
        });
        var file = new FormFile(stream, 0, stream.Length, "file", "orders.xlsx");

        var result = await service.BulkUploadOrdersAsync(new BulkOrderUploadRequest { File = file, BatchSize = 1 });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.OrdersCreated);
        Assert.Equal(3, result.OrderItemsCreated);
        Assert.Equal(1, result.BatchSize);
        var orders = await dbContext.Orders.Include(order => order.OrderItems).ToListAsync();
        Assert.Equal(2, orders.Count);
        var groupedOrder = Assert.Single(orders, order => order.UserId == userId);
        Assert.Equal(30m, groupedOrder.TotalAmount);
        Assert.Equal("Pending", groupedOrder.Status);
        Assert.Equal(2, groupedOrder.OrderItems.Count);
    }

    [Fact]
    public async Task BulkUploadOrdersAsync_RejectsWorkbookWithMissingHeaders()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        using var stream = CreateWorkbook(worksheet => worksheet.Cell(1, 1).Value = "UserId");
        var file = new FormFile(stream, 0, stream.Length, "file", "orders.xlsx");

        var result = await service.BulkUploadOrdersAsync(new BulkOrderUploadRequest { File = file });

        Assert.False(result.IsSuccess);
        Assert.Equal("The Excel file is missing required headers.", result.Error);
        Assert.Empty(await dbContext.Orders.ToListAsync());
    }

    [Fact]
    public async Task BulkUploadOrdersAsync_RejectsInvalidQuantityWithoutPersisting()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        using var stream = CreateWorkbook(worksheet =>
        {
            WriteBulkOrderHeaders(worksheet);
            WriteBulkOrderRow(worksheet, 2, "order-a", Guid.NewGuid(), Guid.NewGuid(), 0, 5m, "Pending");
        });
        var file = new FormFile(stream, 0, stream.Length, "file", "orders.xlsx");

        var result = await service.BulkUploadOrdersAsync(new BulkOrderUploadRequest { File = file });

        Assert.False(result.IsSuccess);
        Assert.Equal("Quantity must be a positive integer at row 2.", result.Error);
        Assert.Empty(await dbContext.Orders.ToListAsync());
    }

    private static MemoryStream CreateWorkbook(Action<IXLWorksheet> populate)
    {
        var stream = new MemoryStream();
        using var workbook = new XLWorkbook();
        populate(workbook.AddWorksheet("Orders"));
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private static void WriteBulkOrderHeaders(IXLWorksheet worksheet)
    {
        worksheet.Cell(1, 1).Value = "OrderReference";
        worksheet.Cell(1, 2).Value = "UserId";
        worksheet.Cell(1, 3).Value = "ProductId";
        worksheet.Cell(1, 4).Value = "Quantity";
        worksheet.Cell(1, 5).Value = "UnitPrice";
        worksheet.Cell(1, 6).Value = "Status";
    }

    private static void WriteBulkOrderRow(
        IXLWorksheet worksheet,
        int row,
        string orderReference,
        Guid userId,
        Guid productId,
        int quantity,
        decimal unitPrice,
        string status)
    {
        worksheet.Cell(row, 1).Value = orderReference;
        worksheet.Cell(row, 2).Value = userId.ToString();
        worksheet.Cell(row, 3).Value = productId.ToString();
        worksheet.Cell(row, 4).Value = quantity;
        worksheet.Cell(row, 5).Value = unitPrice;
        worksheet.Cell(row, 6).Value = status;
    }

    private static OrdersService CreateService(
        OrdersDbContext dbContext,
        IPaymentsClient? paymentsClient = null) =>
        new(
            new OrdersRepository(dbContext),
            CreateCache(),
            paymentsClient ?? new FakePaymentsClient(PaymentsClientResult.Success),
            new OrderStatusOutboxRepository(dbContext));

    private static OrdersDbContext CreateDbContext() => new(new DbContextOptionsBuilder<OrdersDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}

public class OrdersControllerIntegrationTests
{
    [Fact]
    public void OrderCreateAndMutationRoutesRequirePermissions()
    {
        Assert.Equal(PermissionPolicies.OrdersCreate, GetPolicy(nameof(OrdersController.CreateOrder)));
        Assert.Equal(PermissionPolicies.OrdersManage, GetPolicy(nameof(OrdersController.UpdateOrder)));
        Assert.Equal(PermissionPolicies.OrdersManage, GetPolicy(nameof(OrdersController.DeleteOrder)));
        Assert.Equal(PermissionPolicies.OrdersManage, GetPolicy(nameof(OrdersController.AddOrderItem)));
        Assert.Equal(PermissionPolicies.OrdersManage, GetPolicy(nameof(OrdersController.UpdateOrderItem)));
        Assert.Equal(PermissionPolicies.OrdersManage, GetPolicy(nameof(OrdersController.RemoveOrderItem)));
    }

    [Fact]
    public void GenericOrderStatusUpdate_RequiresOrdersManagePolicy()
    {
        var authorization = typeof(OrdersController)
            .GetMethod(nameof(OrdersController.UpdateOrderStatus))!
            .GetCustomAttribute<AuthorizeAttribute>();

        Assert.Equal(PermissionPolicies.OrdersManage, authorization?.Policy);
    }

    [Fact]
    public async Task CancelOrder_ReturnsUnauthorizedWithoutUserIdentity()
    {
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);

        var result = await controller.CancelOrder(Guid.NewGuid());

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task CancelOrder_ReturnsNoContentForSuccessfulCancellation()
    {
        using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var order = new Order { UserId = userId, Status = "Pending" };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext, userId);
        controller.Request.Headers.Authorization = "Bearer customer-token";

        var result = await controller.CancelOrder(order.Id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task CancelOrder_ReturnsServiceUnavailableWhenPaymentsCannotBeUpdated()
    {
        using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var order = new Order { UserId = userId, Status = "Pending" };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var controller = CreateController(
            dbContext,
            userId,
            new FakePaymentsClient(PaymentsClientResult.Unavailable));
        controller.Request.Headers.Authorization = "Bearer customer-token";

        var result = await controller.CancelOrder(order.Id);

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, problem.StatusCode);
        Assert.Equal("Pending", (await dbContext.Orders.FindAsync(order.Id))!.Status);
    }

    [Fact]
    public async Task GetMyOrders_ReturnsOnlyOrdersForAuthenticatedUser()
    {
        using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        dbContext.Orders.AddRange(
            new Order { UserId = userId },
            new Order { UserId = Guid.NewGuid() });
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext, userId);

        var result = await controller.GetMyOrders();

        var response = Assert.IsType<OkObjectResult>(result);
        var orders = Assert.IsAssignableFrom<IReadOnlyList<Order>>(response.Value);
        Assert.Single(orders);
        Assert.Equal(userId, orders[0].UserId);
    }

    [Fact]
    public async Task GetMyOrders_ReturnsUnauthorizedWhenUserIdClaimIsMissing()
    {
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);

        var result = await controller.GetMyOrders();

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetOrderById_ReturnsNotFoundForAnotherUsersOrder()
    {
        using var dbContext = CreateDbContext();
        var order = new Order { UserId = Guid.NewGuid(), TotalAmount = 20 };
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext, Guid.NewGuid());

        var result = await controller.GetOrderById(order.Id);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreateOrder_ReturnsCreatedAtAction()
    {
        using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var controller = CreateController(dbContext, userId);

        var result = await controller.CreateOrder(new CreateOrderRequest
        {
            UserId = Guid.NewGuid(),
            Status = "Completed",
            OrderItems = [new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 25 }]
        });

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var order = Assert.IsType<Order>(created.Value);
        Assert.Equal(userId, order.UserId);
        Assert.Equal("Pending", order.Status);
    }

    [Fact]
    public async Task GetOrderById_ReturnsNotFoundForMissingOrder()
    {
        using var dbContext = CreateDbContext();
        var controller = new OrdersController(CreateService(dbContext));

        var result = await controller.GetOrderById(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    private static OrdersController CreateController(
        OrdersDbContext dbContext,
        Guid? userId = null,
        IPaymentsClient? paymentsClient = null)
    {
        var identity = userId is null
            ? new ClaimsIdentity()
            : new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())], "Test");
        var controller = new OrdersController(CreateService(dbContext, paymentsClient))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };

        return controller;
    }

    private static string? GetPolicy(string actionName) => typeof(OrdersController)
        .GetMethod(actionName)!
        .GetCustomAttribute<AuthorizeAttribute>()?.Policy;

    private static OrdersService CreateService(
        OrdersDbContext dbContext,
        IPaymentsClient? paymentsClient = null) =>
        new(
            new OrdersRepository(dbContext),
            CreateCache(),
            paymentsClient ?? new FakePaymentsClient(PaymentsClientResult.Success),
            new OrderStatusOutboxRepository(dbContext));

    private static OrdersDbContext CreateDbContext() => new(new DbContextOptionsBuilder<OrdersDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}

public sealed class FakePaymentsClient(PaymentsClientResult result) : IPaymentsClient
{
    public bool WasCalled { get; private set; }
    public Guid LastOrderId { get; private set; }
    public Guid LastUserId { get; private set; }

    public Task<PaymentsClientResult> RefundOrCancelOrderPaymentsAsync(
        Guid orderId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        WasCalled = true;
        LastOrderId = orderId;
        LastUserId = userId;
        return Task.FromResult(result);
    }
}
