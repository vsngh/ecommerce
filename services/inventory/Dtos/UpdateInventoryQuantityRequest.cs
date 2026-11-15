using System.ComponentModel.DataAnnotations;

namespace Inventory.Api.Dtos;

public class UpdateInventoryQuantityRequest
{
    [Range(0, int.MaxValue)]
    public int QuantityAvailable { get; set; }
}
