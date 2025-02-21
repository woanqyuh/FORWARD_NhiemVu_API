using MongoDB.Driver;
using ForwardMessage.Dtos;
using System.Threading.Tasks;

namespace ForwardMessage.Repositories
{
    public interface ISheetRepository : IBaseRepository<SheetDto>
    {
        Task<SheetDto> GetByUsernameAsync(string username);
        Task SyncSheetAsync(IEnumerable<SheetDto> newData);
    }

    public class SheetRepository : BaseRepository<SheetDto>, ISheetRepository
    {
        public SheetRepository(IMongoDatabase database) : base(database, "sheets")
        {
           
        }

        public async Task SyncSheetAsync(IEnumerable<SheetDto> newData)
        {
          
            if (newData != null && newData.Any())
            {
                await _collection.DeleteManyAsync(Builders<SheetDto>.Filter.Empty);
                await _collection.InsertManyAsync(newData);
            }
        }
        public async Task<SheetDto> GetByUsernameAsync(string username)
        {
            var filter = Builders<SheetDto>.Filter.Eq(u => u.Username, username);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }
    }

}
