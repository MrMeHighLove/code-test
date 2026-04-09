using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using ProductsApi.Models;

namespace ProductsApi.Services;

public class ProductService(IMongoDbContext context, ILogger<ProductService> logger) : IProductService
{
    public async Task<PagedResult<Product>> GetAllAsync(
        string? colour = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var filter = string.IsNullOrWhiteSpace(colour)
            ? Builders<Product>.Filter.Empty
            : Builders<Product>.Filter.Regex(
                nameof(Product.Colour).ToLowerInvariant(),
                new BsonRegularExpression(RegexEscape(colour), "i"));

        var totalCountTask = context.Products.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var productsTask = GetPagedProductsAsync(filter, normalizedPage, normalizedPageSize, cancellationToken);

        await Task.WhenAll(totalCountTask, productsTask);

        var totalItems = totalCountTask.Result;
        var products = productsTask.Result;

        logger.LogInformation(
            "Fetched {Count} products for colour filter {ColourFilter} on page {Page} with page size {PageSize}. Total items: {TotalItems}.",
            products.Count,
            colour,
            normalizedPage,
            normalizedPageSize,
            totalItems);

        return new PagedResult<Product>(products, normalizedPage, normalizedPageSize, totalItems);
    }

    private async Task<List<Product>> GetPagedProductsAsync(
        FilterDefinition<Product> filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        using var cursor = await context.Products.FindAsync(
            filter,
            new FindOptions<Product, Product>
            {
                Sort = Builders<Product>.Sort.Descending(p => p.CreatedAt),
                Skip = (page - 1) * pageSize,
                Limit = pageSize
            },
            cancellationToken);

        return await cursor.ToListAsync(cancellationToken);
    }

    public async Task<Product?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        using var cursor = await context.Products.FindAsync(p => p.Id == id, cancellationToken: cancellationToken);
        return await cursor.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Product> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Price = request.Price,
            Colour = request.Colour.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await context.Products.InsertOneAsync(product, cancellationToken: cancellationToken);
        logger.LogInformation("Created product {ProductId} ({ProductName}).", product.Id, product.Name);
        return product;
    }

    private static string RegexEscape(string value) => Regex.Escape(value.Trim());
}
