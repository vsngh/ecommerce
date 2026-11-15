using Cart.Api.Data;
using Cart.Api.Dtos;
using Cart.Api.Entities;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Cart.Api.Services;

public class CartService : ICartService
{
    private readonly ICartRepository cartRepository;
    private readonly IDistributedCache cache;
    private static readonly DistributedCacheEntryOptions CacheOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };

    public CartService(ICartRepository cartRepository, IDistributedCache cache)
    {
        this.cartRepository = cartRepository;
        this.cache = cache;
    }

    public async Task<IReadOnlyList<Entities.Cart>> GetCartsAsync()
    {
        const string cacheKey = "carts:all";
        var cachedCarts = await cache.GetStringAsync(cacheKey);
        if (cachedCarts is not null) return JsonSerializer.Deserialize<List<Entities.Cart>>(cachedCarts) ?? [];

        var carts = await cartRepository.GetAllWithItemsAsync();
        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(carts), CacheOptions);
        return carts;
    }

    public async Task<Entities.Cart?> GetCartByIdAsync(Guid cartId)
    {
        var cacheKey = $"carts:{cartId}";
        var cachedCart = await cache.GetStringAsync(cacheKey);
        if (cachedCart is not null) return JsonSerializer.Deserialize<Entities.Cart>(cachedCart);

        var cart = await cartRepository.GetByIdWithItemsAsync(cartId);
        if (cart is not null) await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(cart), CacheOptions);
        return cart;
    }

    public async Task<Entities.Cart?> GetActiveCartByUserIdAsync(Guid userId)
    {
        var cacheKey = $"carts:user:{userId}:active";
        var cachedCart = await cache.GetStringAsync(cacheKey);
        if (cachedCart is not null) return JsonSerializer.Deserialize<Entities.Cart>(cachedCart);

        var cart = await cartRepository.GetActiveByUserIdAsync(userId);
        if (cart is not null) await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(cart), CacheOptions);
        return cart;
    }

    public async Task<Entities.Cart> CreateCartAsync(CreateCartRequest request)
    {
        var cart = new Entities.Cart
        {
            UserId = request.UserId,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CartItems = request.CartItems.Select(item => new CartItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                AddedAt = DateTime.UtcNow
            }).ToList()
        };

        await cartRepository.AddCartAsync(cart);
        await RemoveCartCache(cart.Id, cart.UserId);
        return cart;
    }

    public async Task<bool> UpdateCartStatusAsync(Guid cartId, string status)
    {
        var cart = await cartRepository.GetByIdAsync(cartId);
        if (cart is null) return false;

        cart.Status = status;
        cart.UpdatedAt = DateTime.UtcNow;
        await cartRepository.SaveChangesAsync();
        await RemoveCartCache(cartId, cart.UserId);
        return true;
    }

    public async Task<bool> DeleteCartAsync(Guid cartId)
    {
        var cart = await cartRepository.GetByIdAsync(cartId);
        if (cart is null) return false;

        await cartRepository.DeleteCartAsync(cart);
        await RemoveCartCache(cartId, cart.UserId);
        return true;
    }

    public async Task<IReadOnlyList<CartItem>?> GetCartItemsAsync(Guid cartId)
    {
        var cacheKey = $"carts:{cartId}:items";
        var cachedItems = await cache.GetStringAsync(cacheKey);
        if (cachedItems is not null) return JsonSerializer.Deserialize<List<CartItem>>(cachedItems) ?? [];

        if (!await cartRepository.ExistsAsync(cartId)) return null;

        var cartItems = await cartRepository.GetItemsAsync(cartId);
        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(cartItems), CacheOptions);
        return cartItems;
    }

    public async Task<CartItem?> GetCartItemByIdAsync(Guid cartId, Guid cartItemId)
    {
        var cacheKey = $"carts:{cartId}:items:{cartItemId}";
        var cachedItem = await cache.GetStringAsync(cacheKey);
        if (cachedItem is not null) return JsonSerializer.Deserialize<CartItem>(cachedItem);

        var cartItem = await cartRepository.GetItemByIdAsync(cartId, cartItemId);
        if (cartItem is not null) await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(cartItem), CacheOptions);
        return cartItem;
    }

    public async Task<CartItem?> AddCartItemAsync(Guid cartId, CreateCartItemRequest request)
    {
        var cart = await cartRepository.GetByIdAsync(cartId);
        if (cart is null) return null;

        var cartItem = new CartItem
        {
            CartId = cartId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            AddedAt = DateTime.UtcNow
        };

        cart.UpdatedAt = DateTime.UtcNow;
        await cartRepository.AddCartItemAsync(cartItem);
        await RemoveCartCache(cartId, cart.UserId);
        await cache.RemoveAsync($"carts:{cartId}:items:{cartItem.Id}");
        return cartItem;
    }

    public async Task<bool> UpdateCartItemAsync(Guid cartId, Guid cartItemId, UpdateCartItemRequest request)
    {
        var cartItem = await cartRepository.GetItemByIdAsync(cartId, cartItemId);
        if (cartItem is null) return false;

        cartItem.ProductId = request.ProductId;
        cartItem.Quantity = request.Quantity;
        cartItem.UnitPrice = request.UnitPrice;

        var cart = await cartRepository.GetByIdAsync(cartId);
        if (cart is not null) cart.UpdatedAt = DateTime.UtcNow;

        await cartRepository.SaveChangesAsync();
        if (cart is not null) await RemoveCartCache(cartId, cart.UserId);
        await cache.RemoveAsync($"carts:{cartId}:items:{cartItemId}");
        return true;
    }

    public async Task<bool> RemoveCartItemAsync(Guid cartId, Guid cartItemId)
    {
        var cartItem = await cartRepository.GetItemByIdAsync(cartId, cartItemId);
        if (cartItem is null) return false;

        var cart = await cartRepository.GetByIdAsync(cartId);
        if (cart is not null) cart.UpdatedAt = DateTime.UtcNow;

        await cartRepository.DeleteCartItemAsync(cartItem);
        if (cart is not null) await RemoveCartCache(cartId, cart.UserId);
        await cache.RemoveAsync($"carts:{cartId}:items:{cartItemId}");
        return true;
    }

    public async Task<CheckoutResult?> CheckoutAsync(Guid cartId, CheckoutRequest request)
    {
        var cart = await cartRepository.GetForCheckoutAsync(cartId, request.UserId);
        if (cart is null) return null;

        cart.Status = "CheckoutStarted";
        cart.UpdatedAt = DateTime.UtcNow;
        await cartRepository.SaveChangesAsync();
        await RemoveCartCache(cartId, cart.UserId);
        return new CheckoutResult(cart.Id, cart.UserId, cart.Status, request.PaymentMethod, cart.CartItems.Count);
    }

    private async Task RemoveCartCache(Guid cartId, Guid userId)
    {
        await cache.RemoveAsync("carts:all");
        await cache.RemoveAsync($"carts:{cartId}");
        await cache.RemoveAsync($"carts:{cartId}:items");
        await cache.RemoveAsync($"carts:user:{userId}:active");
    }
}
