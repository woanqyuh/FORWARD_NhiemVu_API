using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ForwardMessage.Repositories
{
    public interface IBaseRepository<T> where T : class
    {
        Task<List<T>> GetAllAsync(Expression<Func<T, bool>> filter = null);
        Task<T> GetByIdAsync(ObjectId id);
        Task AddAsync(T entity);
        Task<bool> UpdateAsync(ObjectId id, T entity);
        Task<bool> DeleteAsync(ObjectId id);
    }

    public abstract class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected readonly IMongoCollection<T> _collection;

        protected BaseRepository(IMongoDatabase database, string collectionName)
        {
            _collection = database.GetCollection<T>(collectionName);
        }

        public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>> filter = null)
        {
     
            var defaultFilter = Builders<T>.Filter.Eq("IsDeleted", false);

            var combinedFilter = filter == null
                ? defaultFilter
                : Builders<T>.Filter.And(defaultFilter, Builders<T>.Filter.Where(filter));

            var sortDefinition = Builders<T>.Sort.Descending("CreatedAt");
            return await _collection.Find(combinedFilter).Sort(sortDefinition).ToListAsync();
        }


        public async Task<T> GetByIdAsync(ObjectId id)
        {

            var filter = Builders<T>.Filter.Eq("_id", id);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task AddAsync(T entity)
        {
   
            await _collection.InsertOneAsync(entity);
        }

        public async Task<bool> UpdateAsync(ObjectId id, T entity)
        {
   
            var filter = Builders<T>.Filter.Eq("_id", id);
            var result = await _collection.ReplaceOneAsync(filter, entity);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteAsync(ObjectId id)
        {

            var filter = Builders<T>.Filter.Eq("_id", id);
            var update = Builders<T>.Update.Set("IsDeleted", true);
            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }
    }
}
