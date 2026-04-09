using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using ProductsApi.Models;

namespace ProductsApi.Services;

public sealed class AuditService(Channel<AuditEntry> channel) : IAuditService
{
    public ValueTask WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        channel.Writer.TryWrite(entry);
        return ValueTask.CompletedTask;
    }
}

public sealed class AuditBackgroundService(
    Channel<AuditEntry> channel,
    ILogger<AuditBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var entry in channel.Reader.ReadAllAsync(stoppingToken))
        {
            logger.LogInformation(
                "AUDIT {EventType} outcome={Outcome} subjectId={SubjectId} username={Username} correlationId={CorrelationId} ip={IpAddress} metadata={Metadata}",
                entry.EventType,
                entry.Outcome,
                entry.SubjectId,
                entry.Username,
                entry.CorrelationId,
                entry.IpAddress,
                entry.Metadata);
        }
    }
}
