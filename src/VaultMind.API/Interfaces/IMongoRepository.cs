using System.Linq.Expressions;
using MongoDB.Driver;

namespace VaultMind.API.Interfaces;

public interface IMongoRepository<T> where T : IEntity
{
    Task<List<T>> GetAllAsync();
    Task<T?> GetByIdAsync(Guid id);
    Task<T?> FindOneAsync(Expression<Func<T, bool>> filterExpression);
    Task<List<T>> FindAsync(Expression<Func<T, bool>> filterExpression);
    Task InsertOneAsync(T document);
    Task ReplaceOneAsync(T document);
    Task UpdateOneAsync(Expression<Func<T, bool>> filterExpression, UpdateDefinition<T> update);
    Task UpdateManyAsync(Expression<Func<T, bool>> filterExpression, UpdateDefinition<T> update);
    Task DeleteByIdAsync(Guid id);
    Task DeleteOneAsync(Expression<Func<T, bool>> filterExpression);
    Task DeleteManyAsync(Expression<Func<T, bool>> filterExpression);
    IMongoCollection<T> Collection { get; }
}
