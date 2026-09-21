using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orders.Api.Dtos;
using Orders.Api.Security;
using Orders.Api.Services;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrdersService ordersService;

    public OrdersController(IOrdersService ordersService)
    {
        this.ordersService = ordersService;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var orders = await ordersService.GetOrdersAsync();

        return Ok(orders);
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetOrderById(Guid orderId)
    {
        var order = await ordersService.GetOrderByIdAsync(orderId);

        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var createdOrder = await ordersService.CreateOrderAsync(request);

        return CreatedAtAction(nameof(GetOrderById), new { orderId = createdOrder.Id }, createdOrder);
    }

    [HttpPost("bulk-upload")]
    [Authorize(Policy = PermissionPolicies.BulkOrdersUpload)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> BulkUploadOrders([FromForm] BulkOrderUploadRequest request)
    {
        var result = await ordersService.BulkUploadOrdersAsync(request);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Details is null
                ? result.Error
                : new { Message = result.Error, result.Details });
        }

        return Ok(new
        {
            result.OrdersCreated,
            result.OrderItemsCreated,
            result.BatchSize
        });
    }

    [HttpPut("{orderId:guid}")]
    public async Task<IActionResult> UpdateOrder(Guid orderId, [FromBody] UpdateOrderRequest request)
    {
        var updated = await ordersService.UpdateOrderAsync(orderId, request);

        return updated ? NoContent() : NotFound();
    }

    [HttpPatch("{orderId:guid}/status")]
    public async Task<IActionResult> UpdateOrderStatus(Guid orderId, [FromBody] UpdateOrderStatusRequest request)
    {
        var updated = await ordersService.UpdateOrderStatusAsync(orderId, request.Status);

        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{orderId:guid}")]
    public async Task<IActionResult> DeleteOrder(Guid orderId)
    {
        var deleted = await ordersService.DeleteOrderAsync(orderId);

        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{orderId:guid}/items")]
    public async Task<IActionResult> GetOrderItems(Guid orderId)
    {
        var orderItems = await ordersService.GetOrderItemsAsync(orderId);

        return orderItems is null ? NotFound() : Ok(orderItems);
    }

    [HttpGet("{orderId:guid}/items/{orderItemId:guid}")]
    public async Task<IActionResult> GetOrderItemById(Guid orderId, Guid orderItemId)
    {
        var orderItem = await ordersService.GetOrderItemByIdAsync(orderId, orderItemId);

        return orderItem is null ? NotFound() : Ok(orderItem);
    }

    [HttpPost("{orderId:guid}/items")]
    public async Task<IActionResult> AddOrderItem(Guid orderId, [FromBody] CreateOrderItemRequest request)
    {
        var createdOrderItem = await ordersService.AddOrderItemAsync(orderId, request);

        if (createdOrderItem is null)
        {
            return NotFound();
        }

        return CreatedAtAction(
            nameof(GetOrderItemById),
            new { orderId, orderItemId = createdOrderItem.Id },
            createdOrderItem);
    }

    [HttpPut("{orderId:guid}/items/{orderItemId:guid}")]
    public async Task<IActionResult> UpdateOrderItem(Guid orderId, Guid orderItemId, [FromBody] UpdateOrderItemRequest request)
    {
        var updated = await ordersService.UpdateOrderItemAsync(orderId, orderItemId, request);

        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{orderId:guid}/items/{orderItemId:guid}")]
    public async Task<IActionResult> RemoveOrderItem(Guid orderId, Guid orderItemId)
    {
        var deleted = await ordersService.RemoveOrderItemAsync(orderId, orderItemId);

        return deleted ? NoContent() : NotFound();
    }
}
