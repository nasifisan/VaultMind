using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using VaultMind.API.Interfaces;

namespace VaultMind.API.Models;

public class User : IUser
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
