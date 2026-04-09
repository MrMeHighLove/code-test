using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProductsApi.Models;

public class Product
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("name")]
    public required string Name { get; init; }

    [BsonElement("description")]
    public string? Description { get; init; }

    [BsonElement("price")]
    public decimal Price { get; init; }

    [BsonElement("colour")]
    public required string Colour { get; init; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
