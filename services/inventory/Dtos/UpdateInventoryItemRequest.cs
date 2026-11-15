namespace Inventory.Api.Dtos;

public class UpdateInventoryItemRequest
{
    public Guid ProductId { get; set; }
    public int QuantityAvailable { get; set; }
    public int QuantityReserved { get; set; }
}
