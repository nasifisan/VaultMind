using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using VaultMind.API.Interfaces;

namespace VaultMind.API.Models;

public class RefreshToken : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid Token { get; set; } = default!;

    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; } // Can be Guid.Empty for "anonymous"

    public DateTime ExpiresAt { get; set; } // Targets the TTL index

    public bool IsRevoked { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
