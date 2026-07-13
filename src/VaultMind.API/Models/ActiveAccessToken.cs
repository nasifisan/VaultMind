using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using VaultMind.API.Interfaces;

namespace VaultMind.API.Models;

public class ActiveAccessToken : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string JwtId { get; set; } = null!; // jti claim inside JWT

    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }

    public DateTime ExpiresAt { get; set; } // Targets the TTL index

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
