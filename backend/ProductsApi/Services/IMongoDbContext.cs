using MongoDB.Driver;
using ProductsApi.Models;

namespace ProductsApi.Services;

public interface IMongoDbContext
{
    IMongoCollection<Product> Products { get; }
    IMongoCollection<User> Users { get; }
    IMongoCollection<RefreshToken> RefreshTokens { get; }
    IMongoCollection<IdempotencyRecord> IdempotencyRecords { get; }

    Task InitializeAsync(CancellationToken cancellationToken);
}
