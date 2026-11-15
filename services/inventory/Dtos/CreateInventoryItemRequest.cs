namespace Inventory.Api.Dtos;

public class CreateInventoryItemRequest
{
    public Guid ProductId { get; set; }
    public int QuantityAvailable { get; set; }
    public int QuantityReserved { get; set; }
}
