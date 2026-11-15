using Products.Api.Dtos;
using Products.Api.Entities;

namespace Products.Api.Services;

public interface IProductsService
{
    Task<IReadOnlyList<Product>> GetProductsAsync();
    Task<Product?> GetProductByIdAsync(Guid productId);
    Task<Product> CreateProductAsync(CreateProductRequest request);
    Task<bool> UpdateProductAsync(Guid productId, UpdateProductRequest request);
    Task<bool> UpdateProductStockAsync(Guid productId, int stockQuantity);
    Task<bool> UpdateProductPriceAsync(Guid productId, decimal price);
    Task<bool> DeleteProductAsync(Guid productId);
}
