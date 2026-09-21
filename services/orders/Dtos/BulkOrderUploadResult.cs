namespace Orders.Api.Dtos;

public record BulkOrderUploadResult(
    bool IsSuccess,
    string? Error,
    object? Details,
    int OrdersCreated,
    int OrderItemsCreated,
    int BatchSize)
{
    public static BulkOrderUploadResult Failure(string error, object? details = null)
    {
        return new BulkOrderUploadResult(false, error, details, 0, 0, 0);
    }

    public static BulkOrderUploadResult Success(int ordersCreated, int orderItemsCreated, int batchSize)
    {
        return new BulkOrderUploadResult(true, null, null, ordersCreated, orderItemsCreated, batchSize);
    }
}
