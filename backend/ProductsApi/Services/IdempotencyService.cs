using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MongoDB.Driver;
using ProductsApi.Models;

namespace ProductsApi.Services;

public sealed class IdempotencyService(
    IMongoDbContext context,
    ILogger<IdempotencyService> logger) : IIdempotencyService
{
    private static readonly TimeSpan ReservationTtl = TimeSpan.FromHours(24);

    public async Task<IdempotencyExecutionResult> BeginRequestAsync(
        string scope,
        string key,
        string requestHash,
        CancellationToken cancellationToken = default)
    {
        var reservation = new IdempotencyRecord
        {
            Scope = scope,
            Key = key,
            RequestHash = requestHash,
            Status = "Pending",
            ExpiresAt = DateTime.UtcNow.Add(ReservationTtl)
        };

        try
        {
            await context.IdempotencyRecords.InsertOneAsync(reservation, cancellationToken: cancellationToken);
            return new IdempotencyExecutionResult(true, false, false, reservation);
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            var existing = await context.IdempotencyRecords
                .Find(r => r.Scope == scope && r.Key == key)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is null)
            {
                logger.LogWarning("Duplicate idempotency reservation for {Scope}/{Key} but no existing record was loaded.", scope, key);
                return new IdempotencyExecutionResult(true, false, false, null);
            }

            if (!FixedTimeEquals(existing.RequestHash, requestHash))
            {
                return new IdempotencyExecutionResult(false, false, true, existing);
            }

            return new IdempotencyExecutionResult(false, existing.Status == "Pending", false, existing);
        }
    }

    public Task CompleteRequestAsync(
        IdempotencyRecord reservation,
        int statusCode,
        string responseBody,
        string contentType,
        string? location,
        CancellationToken cancellationToken = default)
    {
        var update = Builders<IdempotencyRecord>.Update
            .Set(r => r.Status, "Completed")
            .Set(r => r.StatusCode, statusCode)
            .Set(r => r.ResponseBody, responseBody)
            .Set(r => r.ContentType, contentType)
            .Set(r => r.Location, location)
            .Set(r => r.CompletedAt, DateTime.UtcNow);

        return context.IdempotencyRecords.UpdateOneAsync(
            r => r.Id == reservation.Id,
            update,
            cancellationToken: cancellationToken);
    }

    public Task FailRequestAsync(IdempotencyRecord reservation, CancellationToken cancellationToken = default)
    {
        return context.IdempotencyRecords.DeleteOneAsync(r => r.Id == reservation.Id, cancellationToken);
    }

    public static string ComputeRequestHash<T>(T request)
    {
        var payload = JsonSerializer.Serialize(request);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(left),
            Encoding.UTF8.GetBytes(right));
    }
}
