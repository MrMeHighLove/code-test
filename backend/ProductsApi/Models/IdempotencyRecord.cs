using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProductsApi.Models;

public class IdempotencyRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("scope")]
    public required string Scope { get; init; }

    [BsonElement("key")]
    public required string Key { get; init; }

    [BsonElement("requestHash")]
    public required string RequestHash { get; init; }

    [BsonElement("status")]
    public required string Status { get; set; }

    [BsonElement("statusCode")]
    public int? StatusCode { get; set; }

    [BsonElement("responseBody")]
    public string? ResponseBody { get; set; }

    [BsonElement("contentType")]
    public string? ContentType { get; set; }

    [BsonElement("location")]
    public string? Location { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; init; }
}
