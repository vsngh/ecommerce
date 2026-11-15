using ClosedXML.Excel;
using Microsoft.Extensions.Caching.Distributed;
using Orders.Api.Data;
using Orders.Api.Dtos;
using Orders.Api.Entities;
using Orders.Api.Payments;
using System.Text.Json;

namespace Orders.Api.Services;

public class OrdersService : IOrdersService
{
    private readonly IOrdersRepository ordersRepository;
    private readonly IDistributedCache cache;
    private readonly IPaymentsClient paymentsClient;
    private readonly IOrderStatusOutboxRepository notificationOutbox;

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public OrdersService(
        IOrdersRepository ordersRepository,
        IDistributedCache cache,
        IPaymentsClient paymentsClient,
        IOrderStatusOutboxRepository notificationOutbox)
    {
        this.ordersRepository = ordersRepository;
        this.cache = cache;
        this.paymentsClient = paymentsClient;
        this.notificationOutbox = notificationOutbox;
    }

    public async Task<IReadOnlyList<Order>> GetOrdersAsync()
    {
        const string cacheKey = "orders:all";
        var cachedOrders = await cache.GetStringAsync(cacheKey);

        if (cachedOrders is not null)
        {
            return JsonSerializer.Deserialize<List<Order>>(cachedOrders) ?? [];
        }

        var orders = await ordersRepository.GetAllWithItemsAsync();

        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(orders), CacheOptions);

        return orders;
    }

    public async Task<IReadOnlyList<Order>> GetOrdersByUserIdAsync(Guid userId)
    {
        return await ordersRepository.GetByUserIdAsync(userId);
    }

    public async Task<Order?> GetOrderByIdAsync(Guid orderId)
    {
        var cacheKey = $"orders:{orderId}";
        var cachedOrder = await cache.GetStringAsync(cacheKey);

        if (cachedOrder is not null)
        {
            return JsonSerializer.Deserialize<Order>(cachedOrder);
        }

        var order = await ordersRepository.GetByIdWithItemsAsync(orderId);

        if (order is not null)
        {
            await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(order), CacheOptions);
        }

        return order;
    }

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        var orderItems = request.OrderItems.Select(orderItem => new OrderItem
        {
            ProductId = orderItem.ProductId,
            Quantity = orderItem.Quantity,
            UnitPrice = orderItem.UnitPrice
        }).ToList();

        var order = new Order
        {
            UserId = request.UserId,
            OrderedAt = DateTime.UtcNow,
            Status = request.Status,
            TotalAmount = orderItems.Sum(orderItem => orderItem.Quantity * orderItem.UnitPrice),
            OrderItems = orderItems
        };

        await ordersRepository.AddAsync(order);
        await RemoveOrderCache(order.Id);

        return order;
    }

    public async Task<BulkOrderUploadResult> BulkUploadOrdersAsync(BulkOrderUploadRequest request)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BulkOrderUploadResult.Failure("An Excel file is required.");
        }

        if (!Path.GetExtension(request.File.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BulkOrderUploadResult.Failure("Only .xlsx files are supported.");
        }

        var batchSize = request.BatchSize <= 0 ? 100 : request.BatchSize;
        var parsedRows = new List<BulkOrderRow>();

        using (var stream = request.File.OpenReadStream())
        using (var workbook = new XLWorkbook(stream))
        {
            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet is null)
            {
                return BulkOrderUploadResult.Failure("The Excel workbook does not contain any worksheets.");
            }

            var headerMap = GetHeaderMap(worksheet.Row(1));
            var requiredHeaders = new[] { "OrderReference", "UserId", "ProductId", "Quantity", "UnitPrice" };
            var missingHeaders = requiredHeaders
                .Where(header => !headerMap.ContainsKey(header))
                .ToList();

            if (missingHeaders.Count > 0)
            {
                return BulkOrderUploadResult.Failure(
                    "The Excel file is missing required headers.",
                    new { MissingHeaders = missingHeaders });
            }

            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                var rowNumber = row.RowNumber();
                var orderReference = GetCellValue(row, headerMap, "OrderReference");
                var status = GetCellValue(row, headerMap, "Status");

                if (string.IsNullOrWhiteSpace(orderReference))
                {
                    orderReference = $"row-{rowNumber}";
                }

                if (!Guid.TryParse(GetCellValue(row, headerMap, "UserId"), out var userId))
                {
                    return BulkOrderUploadResult.Failure($"Invalid UserId at row {rowNumber}.");
                }

                if (!Guid.TryParse(GetCellValue(row, headerMap, "ProductId"), out var productId))
                {
                    return BulkOrderUploadResult.Failure($"Invalid ProductId at row {rowNumber}.");
                }

                if (!int.TryParse(GetCellValue(row, headerMap, "Quantity"), out var quantity) || quantity <= 0)
                {
                    return BulkOrderUploadResult.Failure($"Quantity must be a positive integer at row {rowNumber}.");
                }

                if (!decimal.TryParse(GetCellValue(row, headerMap, "UnitPrice"), out var unitPrice) || unitPrice < 0)
                {
                    return BulkOrderUploadResult.Failure($"UnitPrice must be a non-negative decimal at row {rowNumber}.");
                }

                parsedRows.Add(new BulkOrderRow(
                    orderReference,
                    userId,
                    productId,
                    quantity,
                    unitPrice,
                    string.IsNullOrWhiteSpace(status) ? "Pending" : status));
            }
        }

        if (parsedRows.Count == 0)
        {
            return BulkOrderUploadResult.Failure("The Excel file does not contain any order rows.");
        }

        var orders = parsedRows
            .GroupBy(row => row.OrderReference)
            .Select(group =>
            {
                var firstRow = group.First();
                var orderItems = group.Select(row => new OrderItem
                {
                    ProductId = row.ProductId,
                    Quantity = row.Quantity,
                    UnitPrice = row.UnitPrice
                }).ToList();

                return new Order
                {
                    UserId = firstRow.UserId,
                    Status = firstRow.Status,
                    OrderedAt = DateTime.UtcNow,
                    TotalAmount = orderItems.Sum(item => item.Quantity * item.UnitPrice),
                    OrderItems = orderItems
                };
            })
            .ToList();

        var insertedOrders = 0;
        var insertedOrderItems = 0;

        foreach (var batch in orders.Chunk(batchSize))
        {
            await ordersRepository.AddRangeAsync(batch);

            insertedOrders += batch.Length;
            insertedOrderItems += batch.Sum(order => order.OrderItems.Count);
        }

        await cache.RemoveAsync("orders:all");

        return BulkOrderUploadResult.Success(insertedOrders, insertedOrderItems, batchSize);
    }

    public async Task<bool> UpdateOrderAsync(Guid orderId, UpdateOrderRequest request)
    {
        var existingOrder = await ordersRepository.GetByIdAsync(orderId);

        if (existingOrder is null)
        {
            return false;
        }

        var statusChanged = !string.Equals(existingOrder.Status, request.Status, StringComparison.OrdinalIgnoreCase);
        existingOrder.UserId = request.UserId;
        existingOrder.TotalAmount = request.TotalAmount;
        existingOrder.Status = request.Status;
        if (statusChanged)
        {
            notificationOutbox.Add(new OrderStatusChangedNotification(
                existingOrder.Id,
                existingOrder.UserId,
                existingOrder.Status,
                DateTime.UtcNow));
        }

        await ordersRepository.UpdateAsync();
        await RemoveOrderCache(orderId);

        return true;
    }

    public async Task<bool> UpdateOrderStatusAsync(Guid orderId, string status)
    {
        var order = await ordersRepository.GetByIdAsync(orderId);

        if (order is null)
        {
            return false;
        }

        order.Status = status;
        notificationOutbox.Add(new OrderStatusChangedNotification(
            order.Id,
            order.UserId,
            order.Status,
            DateTime.UtcNow));
        await ordersRepository.UpdateAsync();
        await RemoveOrderCache(orderId);

        return true;
    }

    public async Task<OrderCancellationResult> CancelOrderAsync(
        Guid orderId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var order = await ordersRepository.GetByIdForUserAsync(orderId, userId, cancellationToken);
        if (order is null)
        {
            return OrderCancellationResult.NotFound;
        }

        if (!order.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
        {
            return OrderCancellationResult.Conflict;
        }

        var paymentsResult = await paymentsClient.RefundOrCancelOrderPaymentsAsync(
            orderId,
            userId,
            cancellationToken);
        if (paymentsResult == PaymentsClientResult.NotFound)
        {
            return OrderCancellationResult.NotFound;
        }

        if (paymentsResult != PaymentsClientResult.Success)
        {
            return OrderCancellationResult.PaymentsUnavailable;
        }

        order.Status = "Cancelled";
        notificationOutbox.Add(new OrderStatusChangedNotification(
            order.Id,
            order.UserId,
            order.Status,
            DateTime.UtcNow));
        await ordersRepository.UpdateAsync(cancellationToken);
        await RemoveOrderCache(orderId);
        return OrderCancellationResult.Cancelled;
    }

    public async Task<bool> DeleteOrderAsync(Guid orderId)
    {
        var order = await ordersRepository.GetByIdAsync(orderId);

        if (order is null)
        {
            return false;
        }

        await ordersRepository.DeleteAsync(order);
        await RemoveOrderCache(orderId);

        return true;
    }

    public async Task<IReadOnlyList<OrderItem>?> GetOrderItemsAsync(Guid orderId)
    {
        var cacheKey = $"orders:{orderId}:items";
        var cachedItems = await cache.GetStringAsync(cacheKey);

        if (cachedItems is not null)
        {
            return JsonSerializer.Deserialize<List<OrderItem>>(cachedItems) ?? [];
        }

        var orderExists = await ordersRepository.ExistsAsync(orderId);

        if (!orderExists)
        {
            return null;
        }

        var orderItems = await ordersRepository.GetItemsAsync(orderId);

        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(orderItems), CacheOptions);

        return orderItems;
    }

    public async Task<OrderItem?> GetOrderItemByIdAsync(Guid orderId, Guid orderItemId)
    {
        var cacheKey = $"orders:{orderId}:items:{orderItemId}";
        var cachedItem = await cache.GetStringAsync(cacheKey);

        if (cachedItem is not null)
        {
            return JsonSerializer.Deserialize<OrderItem>(cachedItem);
        }

        var orderItem = await ordersRepository.GetItemByIdAsync(orderId, orderItemId);

        if (orderItem is not null)
        {
            await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(orderItem), CacheOptions);
        }

        return orderItem;
    }

    public async Task<OrderItem?> AddOrderItemAsync(Guid orderId, CreateOrderItemRequest request)
    {
        var orderExists = await ordersRepository.ExistsAsync(orderId);

        if (!orderExists)
        {
            return null;
        }

        var orderItem = new OrderItem
        {
            OrderId = orderId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice
        };

        await ordersRepository.AddItemAsync(orderItem);
        await RemoveOrderCache(orderId);
        await cache.RemoveAsync($"orders:{orderId}:items:{orderItem.Id}");

        return orderItem;
    }

    public async Task<bool> UpdateOrderItemAsync(Guid orderId, Guid orderItemId, UpdateOrderItemRequest request)
    {
        var existingOrderItem = await ordersRepository.GetItemByIdAsync(orderId, orderItemId);

        if (existingOrderItem is null)
        {
            return false;
        }

        existingOrderItem.ProductId = request.ProductId;
        existingOrderItem.Quantity = request.Quantity;
        existingOrderItem.UnitPrice = request.UnitPrice;

        await ordersRepository.UpdateAsync();
        await RemoveOrderCache(orderId);
        await cache.RemoveAsync($"orders:{orderId}:items:{orderItemId}");

        return true;
    }

    public async Task<bool> RemoveOrderItemAsync(Guid orderId, Guid orderItemId)
    {
        var orderItem = await ordersRepository.GetItemByIdAsync(orderId, orderItemId);

        if (orderItem is null)
        {
            return false;
        }

        await ordersRepository.DeleteItemAsync(orderItem);
        await RemoveOrderCache(orderId);
        await cache.RemoveAsync($"orders:{orderId}:items:{orderItemId}");

        return true;
    }

    private async Task RemoveOrderCache(Guid orderId)
    {
        await cache.RemoveAsync("orders:all");
        await cache.RemoveAsync($"orders:{orderId}");
        await cache.RemoveAsync($"orders:{orderId}:items");
    }

    private static Dictionary<string, int> GetHeaderMap(IXLRow headerRow)
    {
        return headerRow.CellsUsed()
            .ToDictionary(
                cell => cell.GetString().Trim(),
                cell => cell.Address.ColumnNumber,
                StringComparer.OrdinalIgnoreCase);
    }

    private static string GetCellValue(IXLRow row, Dictionary<string, int> headerMap, string header)
    {
        return headerMap.TryGetValue(header, out var columnNumber)
            ? row.Cell(columnNumber).GetFormattedString().Trim()
            : string.Empty;
    }

    private record BulkOrderRow(
        string OrderReference,
        Guid UserId,
        Guid ProductId,
        int Quantity,
        decimal UnitPrice,
        string Status);
}
