using Microsoft.AspNetCore.Mvc;
using Products.Api.Dtos;
using Products.Api.Services;

namespace Products.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductsService productsService;

    public ProductsController(IProductsService productsService)
    {
        this.productsService = productsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts() => Ok(await productsService.GetProductsAsync());

    [HttpGet("{productId:guid}")]
    public async Task<IActionResult> GetProductById(Guid productId)
    {
        var product = await productsService.GetProductByIdAsync(productId);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var product = await productsService.CreateProductAsync(request);
        return CreatedAtAction(nameof(GetProductById), new { productId = product.Id }, product);
    }

    [HttpPut("{productId:guid}")]
    public async Task<IActionResult> UpdateProduct(Guid productId, [FromBody] UpdateProductRequest request)
    {
        return await productsService.UpdateProductAsync(productId, request) ? NoContent() : NotFound();
    }

    [HttpPatch("{productId:guid}/stock")]
    public async Task<IActionResult> UpdateProductStock(Guid productId, [FromBody] UpdateProductStockRequest request)
    {
        return await productsService.UpdateProductStockAsync(productId, request.StockQuantity) ? NoContent() : NotFound();
    }

    [HttpPatch("{productId:guid}/price")]
    public async Task<IActionResult> UpdateProductPrice(Guid productId, [FromBody] UpdateProductPriceRequest request)
    {
        return await productsService.UpdateProductPriceAsync(productId, request.Price) ? NoContent() : NotFound();
    }

    [HttpDelete("{productId:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid productId)
    {
        return await productsService.DeleteProductAsync(productId) ? NoContent() : NotFound();
    }
}
