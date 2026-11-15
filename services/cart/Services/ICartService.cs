using Cart.Api.Dtos;
using Cart.Api.Entities;

namespace Cart.Api.Services;

public interface ICartService
{
    Task<IReadOnlyList<Entities.Cart>> GetCartsAsync();
    Task<Entities.Cart?> GetCartByIdAsync(Guid cartId);
    Task<Entities.Cart?> GetActiveCartByUserIdAsync(Guid userId);
    Task<Entities.Cart> CreateCartAsync(CreateCartRequest request);
    Task<bool> UpdateCartStatusAsync(Guid cartId, string status);
    Task<bool> DeleteCartAsync(Guid cartId);
    Task<IReadOnlyList<CartItem>?> GetCartItemsAsync(Guid cartId);
    Task<CartItem?> GetCartItemByIdAsync(Guid cartId, Guid cartItemId);
    Task<CartItem?> AddCartItemAsync(Guid cartId, CreateCartItemRequest request);
    Task<bool> UpdateCartItemAsync(Guid cartId, Guid cartItemId, UpdateCartItemRequest request);
    Task<bool> RemoveCartItemAsync(Guid cartId, Guid cartItemId);
    Task<CheckoutResult?> CheckoutAsync(Guid cartId, CheckoutRequest request);
}
