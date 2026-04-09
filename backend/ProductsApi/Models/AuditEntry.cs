namespace ProductsApi.Models;

public sealed record AuditEntry(
    string EventType,
    string Outcome,
    string? SubjectId,
    string? Username,
    string? CorrelationId,
    string? IpAddress,
    string? Metadata = null);
