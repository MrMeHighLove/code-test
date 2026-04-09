using Microsoft.Extensions.Hosting;

namespace ProductsApi.Services;

public sealed class MongoIndexInitializerHostedService(
    IServiceProvider serviceProvider,
    ILogger<MongoIndexInitializerHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IMongoDbContext>();
        await context.InitializeAsync(cancellationToken);
        logger.LogInformation("MongoDB indexes initialized.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
