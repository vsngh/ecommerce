using Cart.Api.Entities;
using Microsoft.EntityFrameworkCore;
using ShoppingCart = Cart.Api.Entities.Cart;

namespace Cart.Api.Data;

public class CartRepository : ICartRepository
{
    private readonly CartDbContext dbContext;

    public CartRepository(CartDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ShoppingCart>> GetAllWithItemsAsync() =>
        await dbContext.Carts.Include(cart => cart.CartItems).ToListAsync();

    public Task<ShoppingCart?> GetByIdWithItemsAsync(Guid cartId) =>
        dbContext.Carts.Include(cart => cart.CartItems).FirstOrDefaultAsync(cart => cart.Id == cartId);

    public Task<ShoppingCart?> GetActiveByUserIdAsync(Guid userId) =>
        dbContext.Carts.Include(cart => cart.CartItems)
            .FirstOrDefaultAsync(cart => cart.UserId == userId && cart.Status == "Active");

    public async Task<ShoppingCart?> GetByIdAsync(Guid cartId) => await dbContext.Carts.FindAsync(cartId);

    public Task<bool> ExistsAsync(Guid cartId) => dbContext.Carts.AnyAsync(cart => cart.Id == cartId);

    public async Task<IReadOnlyList<CartItem>> GetItemsAsync(Guid cartId) =>
        await dbContext.CartItems.Where(cartItem => cartItem.CartId == cartId).ToListAsync();

    public Task<CartItem?> GetItemByIdAsync(Guid cartId, Guid cartItemId) =>
        dbContext.CartItems.FirstOrDefaultAsync(item => item.CartId == cartId && item.Id == cartItemId);

    public Task<ShoppingCart?> GetForCheckoutAsync(Guid cartId, Guid userId) =>
        dbContext.Carts.Include(cart => cart.CartItems)
            .FirstOrDefaultAsync(cart => cart.Id == cartId && cart.UserId == userId);

    public async Task AddCartAsync(ShoppingCart cart)
    {
        await dbContext.Carts.AddAsync(cart);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteCartAsync(ShoppingCart cart)
    {
        dbContext.Carts.Remove(cart);
        await dbContext.SaveChangesAsync();
    }

    public async Task AddCartItemAsync(CartItem cartItem)
    {
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteCartItemAsync(CartItem cartItem)
    {
        dbContext.CartItems.Remove(cartItem);
        await dbContext.SaveChangesAsync();
    }

    public Task SaveChangesAsync() => dbContext.SaveChangesAsync();
}