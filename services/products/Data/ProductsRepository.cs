using Microsoft.EntityFrameworkCore;
using Products.Api.Entities;

namespace Products.Api.Data;

public class ProductsRepository(ProductsDbContext dbContext) : IProductsRepository
{
    public async Task<IReadOnlyList<Product>> GetAllAsync() => await dbContext.Products.ToListAsync();

    public Task<Product?> GetByIdAsync(Guid productId) => dbContext.Products.FindAsync(productId).AsTask();

    public async Task AddAsync(Product product)
    {
        await dbContext.Products.AddAsync(product);
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        dbContext.Products.Update(product);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Product product)
    {
        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync();
    }
}