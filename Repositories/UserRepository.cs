using MongoDB.Driver;
using ForwardMessage.Dtos;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ForwardMessage.Repositories
{
    public interface IUserRepository : IBaseRepository<UserDto>
    {
        Task<UserDto> GetByUsernameAsync(string username);
    }

    public class UserRepository : BaseRepository<UserDto>, IUserRepository
    {
        public UserRepository(IMongoDatabase database) : base(database, "users")
        {
        }

        public async Task<UserDto> GetByUsernameAsync(string username)
        {
            var filter = Builders<UserDto>.Filter.And(
                Builders<UserDto>.Filter.Eq(u => u.Username, username),
                Builders<UserDto>.Filter.Eq(u => u.IsDeleted, false)
            );
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }
    }
}
