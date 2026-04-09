using ProductsApi.Models;

namespace ProductsApi.Services;

public interface IAuthService
{
    Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string AccessToken, string RefreshToken, string Message)> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string AccessToken, string NewRefreshToken, string Message)> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
}
