using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductsApi.Constants;
using ProductsApi.Logging;
using ProductsApi.Models;
using ProductsApi.Services;

namespace ProductsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IAuthService authService,
    IIdempotencyService idempotencyService,
    IAuditService auditService,
    ILogger<AuthController> logger) : ControllerBase
{
    private const string IdempotencyReservationItemKey = "IdempotencyReservation";

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var replay = await TryBeginIdempotentRequestAsync(ApiConstants.AuditEvents.AuthRegister, request, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        var reservation = HttpContext.Items[IdempotencyReservationItemKey] as IdempotencyRecord;

        try
        {
            var (success, message) = await authService.RegisterAsync(request, cancellationToken);
            var response = new AuthResponse(message);

            await CompleteIdempotentRequestAsync(
                reservation,
                success ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest,
                response,
                cancellationToken);

            await WriteAuditAsync(
                ApiConstants.AuditEvents.AuthRegister,
                success ? ApiConstants.AuditOutcomes.Success : ApiConstants.AuditOutcomes.Failure,
                request.Username,
                cancellationToken);

            return success ? Ok(response) : BadRequest(response);
        }
        catch
        {
            if (reservation is not null)
            {
                await idempotencyService.FailRequestAsync(reservation, cancellationToken);
            }

            await WriteAuditAsync(ApiConstants.AuditEvents.AuthRegister, ApiConstants.AuditOutcomes.Failure, request.Username, cancellationToken);
            throw;
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
    {
        var (success, accessToken, refreshToken, message) = await authService.LoginAsync(request, cancellationToken);
        if (!success)
        {
            await WriteAuditAsync(ApiConstants.AuditEvents.AuthLogin, ApiConstants.AuditOutcomes.Failure, request.Username, cancellationToken);
            return Unauthorized(new AuthResponse(message));
        }

        SetTokenCookies(accessToken, refreshToken);
        await WriteAuditAsync(ApiConstants.AuditEvents.AuthLogin, ApiConstants.AuditOutcomes.Success, request.Username, cancellationToken);
        return Ok(new AuthResponse(message));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken = default)
    {
        var refreshToken = Request.Cookies[ApiConstants.Defaults.RefreshTokenCookie];
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized(new AuthResponse(ApiConstants.Errors.NoRefreshTokenProvided));
        }

        var (success, accessToken, newRefreshToken, message) = await authService.RefreshAsync(refreshToken, cancellationToken);
        if (!success)
        {
            await WriteAuditAsync(ApiConstants.AuditEvents.AuthRefresh, ApiConstants.AuditOutcomes.Failure, null, cancellationToken);
            return Unauthorized(new AuthResponse(message));
        }

        SetTokenCookies(accessToken, newRefreshToken);
        await WriteAuditAsync(ApiConstants.AuditEvents.AuthRefresh, ApiConstants.AuditOutcomes.Success, null, cancellationToken);
        return Ok(new AuthResponse(message));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken = default)
    {
        var refreshToken = Request.Cookies[ApiConstants.Defaults.RefreshTokenCookie];
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await authService.LogoutAsync(refreshToken, cancellationToken);
        }

        Response.Cookies.Delete(ApiConstants.Defaults.AccessTokenCookie);
        Response.Cookies.Delete(ApiConstants.Defaults.RefreshTokenCookie);

        await WriteAuditAsync(ApiConstants.AuditEvents.AuthLogout, ApiConstants.AuditOutcomes.Success, User.Identity?.Name, cancellationToken);
        return Ok(new AuthResponse(ApiConstants.Errors.LoggedOutSuccessfully));
    }

    private void SetTokenCookies(string accessToken, string refreshToken)
    {
        var secure = Request.IsHttps;
        var sameSite = secure ? SameSiteMode.None : SameSiteMode.Lax;

        Response.Cookies.Append(ApiConstants.Defaults.AccessTokenCookie, accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = sameSite,
            Expires = DateTimeOffset.UtcNow.AddMinutes(15)
        });

        Response.Cookies.Append(ApiConstants.Defaults.RefreshTokenCookie, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = sameSite,
            Path = ApiConstants.Routes.RefreshTokenPath,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }

    private async Task<IActionResult?> TryBeginIdempotentRequestAsync<TRequest>(
        string scope,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var key = Request.Headers[ApiConstants.Headers.IdempotencyKey].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var execution = await idempotencyService.BeginRequestAsync(
            scope,
            key,
            IdempotencyService.ComputeRequestHash(request),
            cancellationToken);

        if (execution.ShouldExecute)
        {
            if (execution.Record is not null)
            {
                HttpContext.Items[IdempotencyReservationItemKey] = execution.Record;
            }

            return null;
        }

        if (execution.IsHashMismatch)
        {
            return Conflict(new ErrorResponse(ApiConstants.Errors.IdempotencyPayloadMismatch));
        }

        if (execution.IsInProgress)
        {
            Response.Headers.RetryAfter = ApiConstants.Defaults.RetryAfterSeconds.ToString();
            return Conflict(new ErrorResponse(ApiConstants.Errors.IdempotencyInProgress));
        }

        Response.Headers.Append(ApiConstants.Headers.IdempotentReplayed, bool.TrueString.ToLowerInvariant());
        return new ContentResult
        {
            Content = execution.Record?.ResponseBody,
            ContentType = execution.Record?.ContentType ?? ApiConstants.Defaults.JsonContentType,
            StatusCode = execution.Record?.StatusCode ?? StatusCodes.Status200OK
        };
    }

    private async Task CompleteIdempotentRequestAsync<TResponse>(
        IdempotencyRecord? reservation,
        int statusCode,
        TResponse response,
        CancellationToken cancellationToken)
    {
        if (reservation is null)
        {
            return;
        }

        await idempotencyService.CompleteRequestAsync(
            reservation,
            statusCode,
            JsonSerializer.Serialize(response),
            ApiConstants.Defaults.JsonContentType,
            null,
            cancellationToken);
    }

    private async Task WriteAuditAsync(
        string eventType,
        string outcome,
        string? subjectId,
        CancellationToken cancellationToken)
    {
        LogMessages.AuthEvent(logger, eventType, outcome, subjectId);

        await auditService.WriteAsync(
            new AuditEntry(
                eventType,
                outcome,
                subjectId,
                User.Identity?.Name,
                HttpContext.Items[RequestCorrelationMiddleware.HeaderName]?.ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString()),
            cancellationToken);
    }
}
