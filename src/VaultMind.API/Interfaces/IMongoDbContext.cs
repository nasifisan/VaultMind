using MongoDB.Driver;

namespace VaultMind.API.Interfaces;

public interface IMongoDbContext
{
    IMongoDatabase Database { get; }
}
