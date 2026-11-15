namespace Inventory.Api.Dtos;

public record InventoryOperationResult(bool IsSuccess, bool NotFound, string? Error)
{
    public static InventoryOperationResult Success() => new(true, false, null);
    public static InventoryOperationResult Missing() => new(false, true, null);
    public static InventoryOperationResult Failure(string error) => new(false, false, error);
}
