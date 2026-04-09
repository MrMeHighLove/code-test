using ProductsApi.Models;

namespace ProductsApi.Services;

public interface IProductService
{
    Task<PagedResult<Product>> GetAllAsync(
        string? colour = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Product> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalItems)
{
    public int TotalPages => TotalItems == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
}
