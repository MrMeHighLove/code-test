using System.Security.Cryptography;
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
[Authorize]
public class ProductsController(
    IProductService productService,
    IIdempotencyService idempotencyService,
    IAuditService auditService,
    ILogger<ProductsController> logger) : ControllerBase
{
    private const string IdempotencyReservationItemKey = "IdempotencyReservation";

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? colour = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var pagedProducts = await productService.GetAllAsync(colour, page, pageSize, cancellationToken);
        var response = new PagedProductsResponse(
            pagedProducts.Items.Select(MapProduct).ToArray(),
            pagedProducts.Page,
            pagedProducts.PageSize,
            pagedProducts.TotalItems,
            pagedProducts.TotalPages);

        var etag = ComputeEtag(response, $"{colour}:{pagedProducts.Page}:{pagedProducts.PageSize}");

        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = ApiConstants.Defaults.CacheControlNoCache;

        if (Request.Headers.IfNoneMatch.Contains(etag))
        {
            LogMessages.ProductsNotModified(logger, colour);
            return StatusCode(StatusCodes.Status304NotModified);
        }

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken = default)
    {
        var product = await productService.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return NotFound(new ErrorResponse(ApiConstants.Errors.ProductNotFound));
        }

        var response = MapProduct(product);
        var etag = ComputeEtag(response, id);

        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = ApiConstants.Defaults.CacheControlNoCache;

        if (Request.Headers.IfNoneMatch.Contains(etag))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var replay = await TryBeginIdempotentRequestAsync(
            $"{ApiConstants.AuditEvents.ProductCreate}:{User.Identity?.Name ?? ApiConstants.Defaults.AnonymousUser}",
            request,
            cancellationToken);

        if (replay is not null)
        {
            return replay;
        }

        var reservation = HttpContext.Items[IdempotencyReservationItemKey] as IdempotencyRecord;

        try
        {
            var product = await productService.CreateAsync(request, cancellationToken);
            var response = MapProduct(product);
            var result = CreatedAtAction(nameof(GetById), new { id = product.Id }, response);

            await CompleteIdempotentRequestAsync(reservation, StatusCodes.Status201Created, response, result, cancellationToken);
            await WriteAuditAsync(ApiConstants.AuditEvents.ProductCreate, ApiConstants.AuditOutcomes.Success, product.Id, product.Name, cancellationToken);

            return result;
        }
        catch
        {
            if (reservation is not null)
            {
                await idempotencyService.FailRequestAsync(reservation, cancellationToken);
            }

            await WriteAuditAsync(ApiConstants.AuditEvents.ProductCreate, ApiConstants.AuditOutcomes.Failure, null, request.Name, cancellationToken);
            throw;
        }
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

        var record = execution.Record!;
        if (!string.IsNullOrWhiteSpace(record.Location))
        {
            Response.Headers.Location = record.Location;
        }

        Response.Headers.Append(ApiConstants.Headers.IdempotentReplayed, bool.TrueString.ToLowerInvariant());
        return new ContentResult
        {
            Content = record.ResponseBody,
            ContentType = record.ContentType ?? ApiConstants.Defaults.JsonContentType,
            StatusCode = record.StatusCode ?? StatusCodes.Status200OK
        };
    }

    private async Task CompleteIdempotentRequestAsync<TResponse>(
        IdempotencyRecord? reservation,
        int statusCode,
        TResponse response,
        CreatedAtActionResult result,
        CancellationToken cancellationToken)
    {
        if (reservation is null)
        {
            return;
        }

        var location = Url.Action(result.ActionName, result.ControllerName, result.RouteValues);
        await idempotencyService.CompleteRequestAsync(
            reservation,
            statusCode,
            JsonSerializer.Serialize(response),
            ApiConstants.Defaults.JsonContentType,
            location,
            cancellationToken);
    }

    private async Task WriteAuditAsync(
        string eventType,
        string outcome,
        string? subjectId,
        string? metadata,
        CancellationToken cancellationToken)
    {
        await auditService.WriteAsync(
            new AuditEntry(
                eventType,
                outcome,
                subjectId,
                User.Identity?.Name,
                HttpContext.Items[RequestCorrelationMiddleware.HeaderName]?.ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                metadata),
            cancellationToken);
    }

    private static ProductResponse MapProduct(Product product) => new(
        product.Id!,
        product.Name,
        product.Description,
        product.Price,
        product.Colour,
        product.CreatedAt);

    private static string ComputeEtag<TPayload>(TPayload payload, string? discriminator)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(new { discriminator, payload });
        return $"\"{Convert.ToHexString(SHA256.HashData(json))}\"";
    }
}
