using ProductsApi.Models;

namespace ProductsApi.Services;

public interface IIdempotencyService
{
    Task<IdempotencyExecutionResult> BeginRequestAsync(
        string scope,
        string key,
        string requestHash,
        CancellationToken cancellationToken = default);

    Task CompleteRequestAsync(
        IdempotencyRecord reservation,
        int statusCode,
        string responseBody,
        string contentType,
        string? location,
        CancellationToken cancellationToken = default);

    Task FailRequestAsync(IdempotencyRecord reservation, CancellationToken cancellationToken = default);
}

public sealed record IdempotencyExecutionResult(
    bool ShouldExecute,
    bool IsInProgress,
    bool IsHashMismatch,
    IdempotencyRecord? Record);
