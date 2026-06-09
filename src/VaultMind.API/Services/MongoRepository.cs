using System.Linq.Expressions;
using MongoDB.Driver;
using VaultMind.API.Interfaces;

namespace VaultMind.API.Services;

public class MongoRepository<T> : IMongoRepository<T> where T : class, IEntity
{
    private readonly IMongoCollection<T> _collection;

    public MongoRepository(IMongoDbContext context)
    {
        var collectionName = typeof(T).Name + "s";
        _collection = context.Database.GetCollection<T>(collectionName);
    }

    public IMongoCollection<T> Collection => _collection;

    public async Task<List<T>> GetAllAsync() =>
        await _collection.Find(_ => true).ToListAsync();

    public async Task<T?> GetByIdAsync(Guid id) =>
        await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task<T?> FindOneAsync(Expression<Func<T, bool>> filterExpression) =>
        await _collection.Find(filterExpression).FirstOrDefaultAsync();

    public async Task<List<T>> FindAsync(Expression<Func<T, bool>> filterExpression) =>
        await _collection.Find(filterExpression).ToListAsync();

    public async Task InsertOneAsync(T document) =>
        await _collection.InsertOneAsync(document);

    public async Task ReplaceOneAsync(T document) =>
        await _collection.ReplaceOneAsync(x => x.Id == document.Id, document);

    public async Task UpdateOneAsync(Expression<Func<T, bool>> filterExpression, UpdateDefinition<T> update) =>
        await _collection.UpdateOneAsync(filterExpression, update);

    public async Task UpdateManyAsync(Expression<Func<T, bool>> filterExpression, UpdateDefinition<T> update) =>
        await _collection.UpdateManyAsync(filterExpression, update);

    public async Task DeleteByIdAsync(Guid id) =>
        await _collection.DeleteOneAsync(x => x.Id == id);

    public async Task DeleteOneAsync(Expression<Func<T, bool>> filterExpression) =>
        await _collection.DeleteOneAsync(filterExpression);

    public async Task DeleteManyAsync(Expression<Func<T, bool>> filterExpression) =>
        await _collection.DeleteManyAsync(filterExpression);
}
