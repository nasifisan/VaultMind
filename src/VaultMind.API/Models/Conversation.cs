using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using VaultMind.API.Interfaces;

namespace VaultMind.API.Models;

public class Conversation : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }

    public string Title { get; set; } = "New Chat";

    public List<ConversationMessage> Messages { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ConversationMessage
{
    public string Role { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
