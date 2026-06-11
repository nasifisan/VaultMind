using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using VaultMind.API.Interfaces;

namespace VaultMind.API.Models;

public class DocumentRecord : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid ConversationId { get; set; }

    public string FileName { get; set; } = null!;

    public string StorageUrl { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long Size { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
