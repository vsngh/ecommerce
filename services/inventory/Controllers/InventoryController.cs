using Inventory.Api.Dtos;
using Inventory.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        this.inventoryService = inventoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetInventoryItems() => Ok(await inventoryService.GetInventoryItemsAsync());

    [HttpGet("{inventoryItemId:guid}")]
    public async Task<IActionResult> GetInventoryItemById(Guid inventoryItemId)
    {
        var item = await inventoryService.GetInventoryItemByIdAsync(inventoryItemId);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("products/{productId:guid}")]
    public async Task<IActionResult> GetInventoryByProductId(Guid productId)
    {
        var item = await inventoryService.GetInventoryByProductIdAsync(productId);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> CreateInventoryItem([FromBody] CreateInventoryItemRequest request)
    {
        var item = await inventoryService.CreateInventoryItemAsync(request);
        return CreatedAtAction(nameof(GetInventoryItemById), new { inventoryItemId = item.Id }, item);
    }

    [HttpPut("{inventoryItemId:guid}")]
    public async Task<IActionResult> UpdateInventoryItem(Guid inventoryItemId, [FromBody] UpdateInventoryItemRequest request)
    {
        return await inventoryService.UpdateInventoryItemAsync(inventoryItemId, request) ? NoContent() : NotFound();
    }

    [HttpPatch("{inventoryItemId:guid}/quantity")]
    public async Task<IActionResult> UpdateInventoryQuantity(Guid inventoryItemId, [FromBody] UpdateInventoryQuantityRequest request)
    {
        return await inventoryService.UpdateInventoryQuantityAsync(inventoryItemId, request.QuantityAvailable) ? NoContent() : NotFound();
    }

    [HttpPatch("{inventoryItemId:guid}/reserve")]
    public async Task<IActionResult> ReserveInventory(Guid inventoryItemId, [FromBody] ReserveInventoryRequest request)
    {
        var result = await inventoryService.ReserveInventoryAsync(inventoryItemId, request.Quantity);
        return ToActionResult(result);
    }

    [HttpPatch("{inventoryItemId:guid}/release")]
    public async Task<IActionResult> ReleaseInventory(Guid inventoryItemId, [FromBody] ReleaseInventoryRequest request)
    {
        var result = await inventoryService.ReleaseInventoryAsync(inventoryItemId, request.Quantity);
        return ToActionResult(result);
    }

    [HttpDelete("{inventoryItemId:guid}")]
    public async Task<IActionResult> DeleteInventoryItem(Guid inventoryItemId)
    {
        return await inventoryService.DeleteInventoryItemAsync(inventoryItemId) ? NoContent() : NotFound();
    }

    private IActionResult ToActionResult(InventoryOperationResult result)
    {
        if (result.NotFound)
        {
            return NotFound();
        }

        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
}
