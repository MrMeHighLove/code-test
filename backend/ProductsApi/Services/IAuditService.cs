using ProductsApi.Models;

namespace ProductsApi.Services;

public interface IAuditService
{
    ValueTask WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}
