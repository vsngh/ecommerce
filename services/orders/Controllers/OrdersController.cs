using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orders.Api.Dtos;
using Orders.Api.Security;
using Orders.Api.Services;
using System.Security.Claims;

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
    [Authorize(Policy = PermissionPolicies.OrdersRead)]
    public async Task<IActionResult> GetOrders()
    {
        var orders = await ordersService.GetOrdersAsync();

        return Ok(orders);
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> GetMyOrders()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        return Ok(await ordersService.GetOrdersByUserIdAsync(userId));
    }

    [HttpGet("{orderId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetOrderById(Guid orderId)
    {
        var order = await ordersService.GetOrderByIdAsync(orderId);

        if (order is null || (!CanReadAllOrders() && !IsCurrentUser(order.UserId)))
        {
            return NotFound();
        }

        return Ok(order);
    }

    [HttpPost]
    [Authorize(Policy = PermissionPolicies.OrdersCreate)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        if (!User.HasClaim("permission", "orders.manage"))
        {
            request.UserId = userId;
            request.Status = "Pending";
        }

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
    [Authorize(Policy = PermissionPolicies.OrdersManage)]
    public async Task<IActionResult> UpdateOrder(Guid orderId, [FromBody] UpdateOrderRequest request)
    {
        var updated = await ordersService.UpdateOrderAsync(orderId, request);

        return updated ? NoContent() : NotFound();
    }

    [HttpPatch("{orderId:guid}/status")]
    [Authorize(Policy = PermissionPolicies.OrdersManage)]
    public async Task<IActionResult> UpdateOrderStatus(Guid orderId, [FromBody] UpdateOrderStatusRequest request)
    {
        var updated = await ordersService.UpdateOrderStatusAsync(orderId, request.Status);

        return updated ? NoContent() : NotFound();
    }

    [HttpPatch("{orderId:guid}/cancel")]
    [Authorize]
    public async Task<IActionResult> CancelOrder(Guid orderId)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var result = await ordersService.CancelOrderAsync(
            orderId,
            userId,
            HttpContext.RequestAborted);

        return result switch
        {
            OrderCancellationResult.Cancelled => NoContent(),
            OrderCancellationResult.NotFound => NotFound(),
            OrderCancellationResult.Conflict => Conflict(new { Message = "Only Pending orders can be cancelled." }),
            OrderCancellationResult.PaymentsUnavailable => Problem(
                title: "Payment state could not be recorded.",
                detail: "The order remains Pending. Retry cancellation later.",
                statusCode: StatusCodes.Status503ServiceUnavailable),
            _ => throw new InvalidOperationException($"Unknown order cancellation result: {result}.")
        };
    }

    [HttpDelete("{orderId:guid}")]
    [Authorize(Policy = PermissionPolicies.OrdersManage)]
    public async Task<IActionResult> DeleteOrder(Guid orderId)
    {
        var deleted = await ordersService.DeleteOrderAsync(orderId);

        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{orderId:guid}/items")]
    [Authorize]
    public async Task<IActionResult> GetOrderItems(Guid orderId)
    {
        if (!await CanReadOrderAsync(orderId))
        {
            return NotFound();
        }

        var orderItems = await ordersService.GetOrderItemsAsync(orderId);

        return orderItems is null ? NotFound() : Ok(orderItems);
    }

    [HttpGet("{orderId:guid}/items/{orderItemId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetOrderItemById(Guid orderId, Guid orderItemId)
    {
        if (!await CanReadOrderAsync(orderId))
        {
            return NotFound();
        }

        var orderItem = await ordersService.GetOrderItemByIdAsync(orderId, orderItemId);

        return orderItem is null ? NotFound() : Ok(orderItem);
    }

    [HttpPost("{orderId:guid}/items")]
    [Authorize(Policy = PermissionPolicies.OrdersManage)]
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
    [Authorize(Policy = PermissionPolicies.OrdersManage)]
    public async Task<IActionResult> UpdateOrderItem(Guid orderId, Guid orderItemId, [FromBody] UpdateOrderItemRequest request)
    {
        var updated = await ordersService.UpdateOrderItemAsync(orderId, orderItemId, request);

        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{orderId:guid}/items/{orderItemId:guid}")]
    [Authorize(Policy = PermissionPolicies.OrdersManage)]
    public async Task<IActionResult> RemoveOrderItem(Guid orderId, Guid orderItemId)
    {
        var deleted = await ordersService.RemoveOrderItemAsync(orderId, orderItemId);

        return deleted ? NoContent() : NotFound();
    }

    private bool CanReadAllOrders() => User.HasClaim("permission", "orders.read");

    private bool IsCurrentUser(Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId) &&
        currentUserId == userId;

    private async Task<bool> CanReadOrderAsync(Guid orderId)
    {
        if (CanReadAllOrders())
        {
            return true;
        }

        var order = await ordersService.GetOrderByIdAsync(orderId);
        return order is not null && IsCurrentUser(order.UserId);
    }
}
