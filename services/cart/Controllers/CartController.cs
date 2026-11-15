using Cart.Api.Dtos;
using Cart.Api.Security;
using Cart.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cart.Api.Controllers;

[ApiController]
[Route("api/carts")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService cartService;

    public CartController(ICartService cartService)
    {
        this.cartService = cartService;
    }

    [HttpGet]
    [Authorize(Policy = PermissionPolicies.CartsRead)]
    public async Task<IActionResult> GetCarts() => Ok(await cartService.GetCartsAsync());

    [HttpGet("{cartId:guid}")]
    [Authorize(Policy = PermissionPolicies.CartsManage)]
    public async Task<IActionResult> GetCartById(Guid cartId)
    {
        var cart = await cartService.GetCartByIdAsync(cartId);
        return cart is null || !CanAccessCart(cart.UserId) ? NotFound() : Ok(cart);
    }

    [HttpGet("users/{userId:guid}/active")]
    [Authorize(Policy = PermissionPolicies.CartsManage)]
    public async Task<IActionResult> GetActiveCartByUserId(Guid userId)
    {
        if (!CanReadAllCarts() && !IsCurrentUser(userId)) return NotFound();

        var cart = await cartService.GetActiveCartByUserIdAsync(userId);
        return cart is null ? NotFound() : Ok(cart);
    }

    [HttpPost]
    [Authorize(Policy = PermissionPolicies.CartsManage)]
    public async Task<IActionResult> CreateCart([FromBody] CreateCartRequest request)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();

        request.UserId = CanReadAllCarts() ? request.UserId : userId;
        if (request.UserId == Guid.Empty) return BadRequest();
        request.Status = "Active";
        var cart = await cartService.CreateCartAsync(request);
        return CreatedAtAction(nameof(GetCartById), new { cartId = cart.Id }, cart);
    }

    [HttpPatch("{cartId:guid}/status")]
    [Authorize(Policy = PermissionPolicies.CartsRead)]
    public async Task<IActionResult> UpdateCartStatus(Guid cartId, [FromBody] UpdateCartStatusRequest request)
    {
        return await cartService.UpdateCartStatusAsync(cartId, request.Status) ? NoContent() : NotFound();
    }

    [HttpDelete("{cartId:guid}")]
    [Authorize(Policy = PermissionPolicies.CartsManage)]
    public async Task<IActionResult> DeleteCart(Guid cartId)
    {
        if (!await CanAccessCartAsync(cartId)) return NotFound();
        return await cartService.DeleteCartAsync(cartId) ? NoContent() : NotFound();
    }

    [HttpGet("{cartId:guid}/items")]
    [Authorize(Policy = PermissionPolicies.CartsManage)]
    public async Task<IActionResult> GetCartItems(Guid cartId)
    {
        if (!await CanAccessCartAsync(cartId)) return NotFound();
        var cartItems = await cartService.GetCartItemsAsync(cartId);
        return cartItems is null ? NotFound() : Ok(cartItems);
    }

    [HttpGet("{cartId:guid}/items/{cartItemId:guid}")]
    [Authorize(Policy = PermissionPolicies.CartsManage)]
    public async Task<IActionResult> GetCartItemById(Guid cartId, Guid cartItemId)
    {
        if (!await CanAccessCartAsync(cartId)) return NotFound();
        var cartItem = await cartService.GetCartItemByIdAsync(cartId, cartItemId);
        return cartItem is null ? NotFound() : Ok(cartItem);
    }

    [HttpPost("{cartId:guid}/items")]
    [Authorize(Policy = PermissionPolicies.CartsManage)]
    public async Task<IActionResult> AddCartItem(Guid cartId, [FromBody] CreateCartItemRequest request)
    {
        if (!await CanAccessCartAsync(cartId)) return NotFound();
        var cartItem = await cartService.AddCartItemAsync(cartId, request);
        if (cartItem is null) return NotFound();

        return CreatedAtAction(nameof(GetCartItemById), new { cartId, cartItemId = cartItem.Id }, cartItem);
    }

    [HttpPut("{cartId:guid}/items/{cartItemId:guid}")]
    [Authorize(Policy = PermissionPolicies.CartsManage)]
    public async Task<IActionResult> UpdateCartItem(Guid cartId, Guid cartItemId, [FromBody] UpdateCartItemRequest request)
    {
        if (!await CanAccessCartAsync(cartId)) return NotFound();
        return await cartService.UpdateCartItemAsync(cartId, cartItemId, request) ? NoContent() : NotFound();
    }

    [HttpDelete("{cartId:guid}/items/{cartItemId:guid}")]
    [Authorize(Policy = PermissionPolicies.CartsManage)]
    public async Task<IActionResult> RemoveCartItem(Guid cartId, Guid cartItemId)
    {
        if (!await CanAccessCartAsync(cartId)) return NotFound();
        return await cartService.RemoveCartItemAsync(cartId, cartItemId) ? NoContent() : NotFound();
    }

    [HttpPost("{cartId:guid}/checkout")]
    [Authorize(Policy = PermissionPolicies.CartsManage)]
    public async Task<IActionResult> Checkout(Guid cartId, [FromBody] CheckoutRequest request)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        if (!await CanAccessCartAsync(cartId)) return NotFound();

        var result = await cartService.CheckoutAsync(cartId, new CheckoutRequest
        {
            UserId = userId,
            PaymentMethod = request.PaymentMethod
        });
        return result is null ? NotFound() : Accepted(result);
    }

    private bool CanReadAllCarts() => User.HasClaim("permission", "carts.read");

    private bool CanAccessCart(Guid ownerId) => CanReadAllCarts() || IsCurrentUser(ownerId);

    private async Task<bool> CanAccessCartAsync(Guid cartId)
    {
        var cart = await cartService.GetCartByIdAsync(cartId);
        return cart is not null && CanAccessCart(cart.UserId);
    }

    private bool IsCurrentUser(Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId) &&
        currentUserId == userId;

    private bool TryGetCurrentUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
