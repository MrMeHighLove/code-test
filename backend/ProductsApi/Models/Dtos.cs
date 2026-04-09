using System.ComponentModel.DataAnnotations;

namespace ProductsApi.Models;

public record RegisterRequest
{
    [Required]
    [MinLength(3)]
    public required string Username { get; init; }

    [Required]
    [MinLength(6)]
    public required string Password { get; init; }
}

public record LoginRequest
{
    [Required]
    public required string Username { get; init; }

    [Required]
    public required string Password { get; init; }
}

public record CreateProductRequest
{
    [Required]
    public required string Name { get; init; }

    public string? Description { get; init; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; init; }

    [Required]
    public required string Colour { get; init; }
}

public record ProductResponse(
    string Id,
    string Name,
    string? Description,
    decimal Price,
    string Colour,
    DateTime CreatedAt
);

public record PagedProductsResponse(
    IReadOnlyList<ProductResponse> Items,
    int Page,
    int PageSize,
    long TotalItems,
    int TotalPages
);

public record AuthResponse(string Message);

public record ErrorResponse(string Message);
