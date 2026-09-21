namespace Inventory.Api.Entities;

public class InventoryItem
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public int QuantityAvailable { get; set; }

    public int QuantityReserved { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
