using Products.Api.Entities;

namespace Products.Api.Data;

public interface IProductsRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(Guid productId);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Product product);
}