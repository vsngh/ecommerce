using Cart.Api.Dtos;
using Cart.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cart.Api.Controllers;

[ApiController]
[Route("api/carts")]
public class CartController : ControllerBase
{
    private readonly ICartService cartService;

    public CartController(ICartService cartService)
    {
        this.cartService = cartService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCarts() => Ok(await cartService.GetCartsAsync());

    [HttpGet("{cartId:guid}")]
    public async Task<IActionResult> GetCartById(Guid cartId)
    {
        var cart = await cartService.GetCartByIdAsync(cartId);
        return cart is null ? NotFound() : Ok(cart);
    }

    [HttpGet("users/{userId:guid}/active")]
    public async Task<IActionResult> GetActiveCartByUserId(Guid userId)
    {
        var cart = await cartService.GetActiveCartByUserIdAsync(userId);
        return cart is null ? NotFound() : Ok(cart);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCart([FromBody] CreateCartRequest request)
    {
        var cart = await cartService.CreateCartAsync(request);
        return CreatedAtAction(nameof(GetCartById), new { cartId = cart.Id }, cart);
    }

    [HttpPatch("{cartId:guid}/status")]
    public async Task<IActionResult> UpdateCartStatus(Guid cartId, [FromBody] UpdateCartStatusRequest request)
    {
        return await cartService.UpdateCartStatusAsync(cartId, request.Status) ? NoContent() : NotFound();
    }

    [HttpDelete("{cartId:guid}")]
    public async Task<IActionResult> DeleteCart(Guid cartId)
    {
        return await cartService.DeleteCartAsync(cartId) ? NoContent() : NotFound();
    }

    [HttpGet("{cartId:guid}/items")]
    public async Task<IActionResult> GetCartItems(Guid cartId)
    {
        var cartItems = await cartService.GetCartItemsAsync(cartId);
        return cartItems is null ? NotFound() : Ok(cartItems);
    }

    [HttpGet("{cartId:guid}/items/{cartItemId:guid}")]
    public async Task<IActionResult> GetCartItemById(Guid cartId, Guid cartItemId)
    {
        var cartItem = await cartService.GetCartItemByIdAsync(cartId, cartItemId);
        return cartItem is null ? NotFound() : Ok(cartItem);
    }

    [HttpPost("{cartId:guid}/items")]
    public async Task<IActionResult> AddCartItem(Guid cartId, [FromBody] CreateCartItemRequest request)
    {
        var cartItem = await cartService.AddCartItemAsync(cartId, request);
        if (cartItem is null) return NotFound();

        return CreatedAtAction(nameof(GetCartItemById), new { cartId, cartItemId = cartItem.Id }, cartItem);
    }

    [HttpPut("{cartId:guid}/items/{cartItemId:guid}")]
    public async Task<IActionResult> UpdateCartItem(Guid cartId, Guid cartItemId, [FromBody] UpdateCartItemRequest request)
    {
        return await cartService.UpdateCartItemAsync(cartId, cartItemId, request) ? NoContent() : NotFound();
    }

    [HttpDelete("{cartId:guid}/items/{cartItemId:guid}")]
    public async Task<IActionResult> RemoveCartItem(Guid cartId, Guid cartItemId)
    {
        return await cartService.RemoveCartItemAsync(cartId, cartItemId) ? NoContent() : NotFound();
    }

    [HttpPost("{cartId:guid}/checkout")]
    public async Task<IActionResult> Checkout(Guid cartId, [FromBody] CheckoutRequest request)
    {
        var result = await cartService.CheckoutAsync(cartId, request);
        return result is null ? NotFound() : Accepted(result);
    }
}
