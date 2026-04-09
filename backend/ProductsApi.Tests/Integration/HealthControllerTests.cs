using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Moq;
using ProductsApi.Models;
using ProductsApi.Services;

namespace ProductsApi.Tests.Integration;

public class HealthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthControllerTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("JWT_SECRET", "TestSecretKeyThatIsLongEnoughForHmacSha256Validation!");

        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMongoDbContext));
                if (descriptor is not null)
                {
                    services.Remove(descriptor);
                }

                var mockContext = new Mock<IMongoDbContext>();
                mockContext.Setup(c => c.Products).Returns(new Mock<IMongoCollection<Product>>().Object);
                mockContext.Setup(c => c.Users).Returns(new Mock<IMongoCollection<User>>().Object);
                mockContext.Setup(c => c.RefreshTokens).Returns(new Mock<IMongoCollection<RefreshToken>>().Object);
                mockContext.Setup(c => c.IdempotencyRecords).Returns(new Mock<IMongoCollection<IdempotencyRecord>>().Object);
                mockContext.Setup(c => c.InitializeAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

                services.AddSingleton<IMongoDbContext>(mockContext.Object);
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOk_WithStatusOk()
    {
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var response = await _client.GetAsync("/api/health", cancellationSource.Token);

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync(cancellationSource.Token);
        using var doc = JsonDocument.Parse(content);
        Assert.Equal("ok", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetProducts_Returns401_WhenNotAuthenticated()
    {
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var response = await _client.GetAsync("/api/products", cancellationSource.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
