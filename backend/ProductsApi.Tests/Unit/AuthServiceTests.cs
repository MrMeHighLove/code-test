using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using ProductsApi.Configuration;
using ProductsApi.Models;
using ProductsApi.Services;

namespace ProductsApi.Tests.Unit;

public class AuthServiceTests
{
    private readonly Mock<IMongoDbContext> _mockContext = new();
    private readonly Mock<IMongoCollection<User>> _mockUsersCollection = new();
    private readonly Mock<IMongoCollection<RefreshToken>> _mockRefreshTokensCollection = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _mockContext.Setup(c => c.Users).Returns(_mockUsersCollection.Object);
        _mockContext.Setup(c => c.RefreshTokens).Returns(_mockRefreshTokensCollection.Object);
        _mockContext.Setup(c => c.IdempotencyRecords).Returns(new Mock<IMongoCollection<IdempotencyRecord>>().Object);

        var jwtSettings = Options.Create(new JwtSettings
        {
            Secret = "ThisIsATestSecretKeyThatIsLongEnoughForHmacSha256!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        });

        _sut = new AuthService(_mockContext.Object, jwtSettings, Mock.Of<ILogger<AuthService>>());
    }

    private static Mock<IAsyncCursor<T>> CreateCursor<T>(params T[] items)
    {
        var cursor = new Mock<IAsyncCursor<T>>();
        var hasItems = items.Length > 0;

        cursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(hasItems)
            .Returns(false);
        cursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasItems)
            .ReturnsAsync(false);
        cursor.Setup(c => c.Current).Returns(items);

        return cursor;
    }

    [Fact]
    public async Task RegisterAsync_Succeeds_ForNewUsername()
    {
        using var cancellationSource = new CancellationTokenSource();
        var request = new RegisterRequest { Username = "newuser", Password = "password123" };

        _mockUsersCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                cancellationSource.Token))
            .ReturnsAsync(CreateCursor<User>().Object);

        _mockUsersCollection.Setup(c => c.InsertOneAsync(
                It.IsAny<User>(),
                It.IsAny<InsertOneOptions>(),
                cancellationSource.Token))
            .Returns(Task.CompletedTask);

        var (success, message) = await _sut.RegisterAsync(request, cancellationSource.Token);

        Assert.True(success);
        Assert.Equal("User registered successfully.", message);
        _mockUsersCollection.Verify(c => c.InsertOneAsync(
            It.IsAny<User>(),
            It.IsAny<InsertOneOptions>(),
            cancellationSource.Token), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_Fails_ForDuplicateUsername()
    {
        using var cancellationSource = new CancellationTokenSource();
        var request = new RegisterRequest { Username = "existinguser", Password = "password123" };
        var existingUser = new User
        {
            Id = "abc123",
            Username = "existinguser",
            PasswordHash = "somehash"
        };

        _mockUsersCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                cancellationSource.Token))
            .ReturnsAsync(CreateCursor(existingUser).Object);

        var (success, message) = await _sut.RegisterAsync(request, cancellationSource.Token);

        Assert.False(success);
        Assert.Equal("Username already exists.", message);
        _mockUsersCollection.Verify(c => c.InsertOneAsync(
            It.IsAny<User>(),
            It.IsAny<InsertOneOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_Fails_WithWrongPassword()
    {
        using var cancellationSource = new CancellationTokenSource();
        var request = new LoginRequest { Username = "testuser", Password = "wrongpassword" };
        var user = new User
        {
            Id = "user123",
            Username = "testuser",
            PasswordHash = "AAAAAAAAAAAAAAAAAAAAAA==.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="
        };

        _mockUsersCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                cancellationSource.Token))
            .ReturnsAsync(CreateCursor(user).Object);

        var (success, accessToken, refreshToken, message) = await _sut.LoginAsync(request, cancellationSource.Token);

        Assert.False(success);
        Assert.Equal(string.Empty, accessToken);
        Assert.Equal(string.Empty, refreshToken);
        Assert.Equal("Invalid username or password.", message);
    }

    [Fact]
    public async Task LoginAsync_Succeeds_WithCorrectCredentials()
    {
        using var cancellationSource = new CancellationTokenSource();
        var registerRequest = new RegisterRequest { Username = "loginuser", Password = "correctpassword" };

        _mockUsersCollection.SetupSequence(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                cancellationSource.Token))
            .ReturnsAsync(CreateCursor<User>().Object)
            .ReturnsAsync(CreateCursor(new User
            {
                Id = "user456",
                Username = "loginuser",
                PasswordHash = "placeholder"
            }).Object);

        User? capturedUser = null;
        _mockUsersCollection.Setup(c => c.InsertOneAsync(
                It.IsAny<User>(),
                It.IsAny<InsertOneOptions>(),
                cancellationSource.Token))
            .Callback<User, InsertOneOptions, CancellationToken>((user, _, _) =>
            {
                user.Id = "user456";
                capturedUser = user;
            })
            .Returns(Task.CompletedTask);

        _mockRefreshTokensCollection.Setup(c => c.InsertOneAsync(
                It.IsAny<RefreshToken>(),
                It.IsAny<InsertOneOptions>(),
                cancellationSource.Token))
            .Returns(Task.CompletedTask);

        await _sut.RegisterAsync(registerRequest, cancellationSource.Token);
        Assert.NotNull(capturedUser);

        _mockUsersCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<User>>(),
                It.IsAny<FindOptions<User, User>>(),
                cancellationSource.Token))
            .ReturnsAsync(CreateCursor(capturedUser!).Object);

        var loginRequest = new LoginRequest { Username = "loginuser", Password = "correctpassword" };
        var (success, accessToken, refreshToken, message) = await _sut.LoginAsync(loginRequest, cancellationSource.Token);

        Assert.True(success);
        Assert.NotEmpty(accessToken);
        Assert.NotEmpty(refreshToken);
        Assert.Equal("Login successful.", message);
    }
}
