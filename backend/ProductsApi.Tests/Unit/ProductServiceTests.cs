using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using ProductsApi.Models;
using ProductsApi.Services;

namespace ProductsApi.Tests.Unit;

public class ProductServiceTests
{
    private readonly Mock<IMongoDbContext> _mockContext = new();
    private readonly Mock<IMongoCollection<Product>> _mockCollection = new();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _mockContext.Setup(c => c.Products).Returns(_mockCollection.Object);
        _mockContext.Setup(c => c.IdempotencyRecords).Returns(new Mock<IMongoCollection<IdempotencyRecord>>().Object);
        _sut = new ProductService(_mockContext.Object, Mock.Of<ILogger<ProductService>>());
    }

    private static Mock<IAsyncCursor<Product>> CreateCursor(IEnumerable<Product> products)
    {
        var materialized = products.ToList();
        var cursor = new Mock<IAsyncCursor<Product>>();
        cursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        cursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        cursor.Setup(c => c.Current).Returns(materialized);
        return cursor;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllProducts_WhenNoColourFilter()
    {
        using var cancellationSource = new CancellationTokenSource();
        var products = new List<Product>
        {
            new Product { Id = "1", Name = "Widget", Colour = "Red", Price = 9.99m },
            new Product { Id = "2", Name = "Gadget", Colour = "Blue", Price = 19.99m }
        };

        var cursor = CreateCursor(products);
        _mockCollection.Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Product>>(),
                It.IsAny<CountOptions>(),
                cancellationSource.Token))
            .ReturnsAsync(products.Count);

        _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Product>>(),
                It.IsAny<FindOptions<Product, Product>>(),
                cancellationSource.Token))
            .ReturnsAsync(cursor.Object);

        var result = await _sut.GetAllAsync(cancellationToken: cancellationSource.Token);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Widget", result.Items[0].Name);
        Assert.Equal("Gadget", result.Items[1].Name);
        Assert.Equal(2, result.TotalItems);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByColour_WhenColourProvided()
    {
        using var cancellationSource = new CancellationTokenSource();
        var products = new List<Product>
        {
            new Product { Id = "1", Name = "Widget", Colour = "Red", Price = 9.99m }
        };

        var cursor = CreateCursor(products);
        _mockCollection.Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Product>>(),
                It.IsAny<CountOptions>(),
                cancellationSource.Token))
            .ReturnsAsync(products.Count);

        _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Product>>(),
                It.IsAny<FindOptions<Product, Product>>(),
                cancellationSource.Token))
            .ReturnsAsync(cursor.Object);

        var result = await _sut.GetAllAsync("Red", cancellationToken: cancellationSource.Token);

        Assert.Single(result.Items);
        Assert.Equal("Red", result.Items[0].Colour);
        Assert.Equal(1, result.TotalItems);
        _mockCollection.Verify(c => c.FindAsync(
            It.IsAny<FilterDefinition<Product>>(),
            It.IsAny<FindOptions<Product, Product>>(),
            cancellationSource.Token), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedMetadata_WhenPageIsGreaterThanOne()
    {
        using var cancellationSource = new CancellationTokenSource();
        var products = new List<Product>
        {
            new() { Id = "3", Name = "Third", Colour = "Blue", Price = 11m }
        };

        var cursor = CreateCursor(products);
        _mockCollection.Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Product>>(),
                It.IsAny<CountOptions>(),
                cancellationSource.Token))
            .ReturnsAsync(3);

        _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<Product>>(),
                It.IsAny<FindOptions<Product, Product>>(),
                cancellationSource.Token))
            .ReturnsAsync(cursor.Object);

        var result = await _sut.GetAllAsync(page: 2, pageSize: 2, cancellationToken: cancellationSource.Token);

        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalItems);
        Assert.Equal(2, result.TotalPages);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task CreateAsync_CreatesAndReturnsProduct()
    {
        using var cancellationSource = new CancellationTokenSource();
        var request = new CreateProductRequest
        {
            Name = "New Widget",
            Description = "A shiny widget",
            Price = 29.99m,
            Colour = "Green"
        };

        _mockCollection.Setup(c => c.InsertOneAsync(
                It.IsAny<Product>(),
                It.IsAny<InsertOneOptions>(),
                cancellationSource.Token))
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(request, cancellationSource.Token);

        Assert.Equal("New Widget", result.Name);
        Assert.Equal("A shiny widget", result.Description);
        Assert.Equal(29.99m, result.Price);
        Assert.Equal("Green", result.Colour);

        _mockCollection.Verify(c => c.InsertOneAsync(
            It.IsAny<Product>(),
            It.IsAny<InsertOneOptions>(),
            cancellationSource.Token), Times.Once);
    }
}
