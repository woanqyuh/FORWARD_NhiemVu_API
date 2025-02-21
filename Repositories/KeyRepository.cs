using MongoDB.Driver;
using ForwardMessage.Dtos;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ForwardMessage.Repositories
{
    public interface IKeyRepository : IBaseRepository<KeyDto>
    {
        Task<KeyDto> GetBySearchKeyAsync(string searchKey);
    }

    public class KeyRepository : BaseRepository<KeyDto>, IKeyRepository
    {
        public KeyRepository(IMongoDatabase database) : base(database, "keys")
        {

        }
        public async Task<KeyDto> GetBySearchKeyAsync(string searchKey)
        {
            var filter = Builders<KeyDto>.Filter.And(
                Builders<KeyDto>.Filter.Eq(u => u.SearchKey, searchKey),
                Builders<KeyDto>.Filter.Eq(u => u.IsDeleted, false)
            );
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }
    }
}
