using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ProductsApi.Configuration;
using ProductsApi.Models;

namespace ProductsApi.Services;

public class MongoDbContext : IMongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoCollection<Product> Products => _database.GetCollection<Product>("products");
    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    public IMongoCollection<RefreshToken> RefreshTokens => _database.GetCollection<RefreshToken>("refreshTokens");
    public IMongoCollection<IdempotencyRecord> IdempotencyRecords => _database.GetCollection<IdempotencyRecord>("idempotencyRecords");

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await Users.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending(u => u.Username),
                    new CreateIndexOptions
                    {
                        Unique = true,
                        Name = "ux_users_username"
                    })
            ],
            cancellationToken: cancellationToken);

        await Products.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<Product>(
                    Builders<Product>.IndexKeys.Ascending(p => p.Colour).Descending(p => p.CreatedAt),
                    new CreateIndexOptions
                    {
                        Name = "ix_products_colour_createdAt"
                    })
            ],
            cancellationToken: cancellationToken);

        await RefreshTokens.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<RefreshToken>(
                    Builders<RefreshToken>.IndexKeys.Ascending(r => r.Token),
                    new CreateIndexOptions
                    {
                        Unique = true,
                        Name = "ux_refreshTokens_token"
                    }),
                new CreateIndexModel<RefreshToken>(
                    Builders<RefreshToken>.IndexKeys.Ascending(r => r.ExpiresAt),
                    new CreateIndexOptions
                    {
                        Name = "ttl_refreshTokens_expiresAt",
                        ExpireAfter = TimeSpan.Zero
                    })
            ],
            cancellationToken: cancellationToken);

        await IdempotencyRecords.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<IdempotencyRecord>(
                    Builders<IdempotencyRecord>.IndexKeys
                        .Ascending(r => r.Scope)
                        .Ascending(r => r.Key),
                    new CreateIndexOptions
                    {
                        Unique = true,
                        Name = "ux_idempotency_scope_key"
                    }),
                new CreateIndexModel<IdempotencyRecord>(
                    Builders<IdempotencyRecord>.IndexKeys.Ascending(r => r.ExpiresAt),
                    new CreateIndexOptions
                    {
                        Name = "ttl_idempotency_expiresAt",
                        ExpireAfter = TimeSpan.Zero
                    })
            ],
            cancellationToken: cancellationToken);
    }
}
