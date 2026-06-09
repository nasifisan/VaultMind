using MongoDB.Driver;
using VaultMind.API.Interfaces;
using VaultMind.API.Models;

namespace VaultMind.API.Services;

public class MongoDbInitializer : IHostedService
{
    private readonly IMongoRepository<RefreshToken> _refreshTokenRepo;
    private readonly IMongoRepository<ActiveAccessToken> _activeTokenRepo;
    private readonly ILogger<MongoDbInitializer> _logger;

    public MongoDbInitializer(
        IMongoRepository<RefreshToken> refreshTokenRepo,
        IMongoRepository<ActiveAccessToken> activeTokenRepo,
        ILogger<MongoDbInitializer> logger)
    {
        _refreshTokenRepo = refreshTokenRepo;
        _activeTokenRepo = activeTokenRepo;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing MongoDB indexes...");

        try
        {
            // ── Create TTL Index for RefreshTokens ──
            var refreshTokenKeys = Builders<RefreshToken>.IndexKeys.Ascending(t => t.ExpiresAt);
            var refreshTokenOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.Zero };
            var refreshTokenIndexModel = new CreateIndexModel<RefreshToken>(refreshTokenKeys, refreshTokenOptions);
            await _refreshTokenRepo.Collection.Indexes.CreateOneAsync(refreshTokenIndexModel, cancellationToken: cancellationToken);
            _logger.LogInformation("MongoDB TTL Index on RefreshTokens created successfully.");

            // ── Create TTL Index for ActiveAccessTokens ──
            var activeTokenKeys = Builders<ActiveAccessToken>.IndexKeys.Ascending(t => t.ExpiresAt);
            var activeTokenOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.Zero };
            var activeTokenIndexModel = new CreateIndexModel<ActiveAccessToken>(activeTokenKeys, activeTokenOptions);
            await _activeTokenRepo.Collection.Indexes.CreateOneAsync(activeTokenIndexModel, cancellationToken: cancellationToken);
            _logger.LogInformation("MongoDB TTL Index on ActiveAccessTokens created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MongoDB indexes.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
