using Cart.Api.Entities;
using ShoppingCart = Cart.Api.Entities.Cart;

namespace Cart.Api.Data;

public interface ICartRepository
{
    Task<IReadOnlyList<ShoppingCart>> GetAllWithItemsAsync();
    Task<ShoppingCart?> GetByIdWithItemsAsync(Guid cartId);
    Task<ShoppingCart?> GetActiveByUserIdAsync(Guid userId);
    Task<ShoppingCart?> GetByIdAsync(Guid cartId);
    Task<bool> ExistsAsync(Guid cartId);
    Task<IReadOnlyList<CartItem>> GetItemsAsync(Guid cartId);
    Task<CartItem?> GetItemByIdAsync(Guid cartId, Guid cartItemId);
    Task<ShoppingCart?> GetForCheckoutAsync(Guid cartId, Guid userId);
    Task AddCartAsync(ShoppingCart cart);
    Task DeleteCartAsync(ShoppingCart cart);
    Task AddCartItemAsync(CartItem cartItem);
    Task DeleteCartItemAsync(CartItem cartItem);
    Task SaveChangesAsync();
}