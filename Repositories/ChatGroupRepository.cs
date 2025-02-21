using MongoDB.Driver;
using ForwardMessage.Dtos;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ForwardMessage.Repositories
{
    public interface IChatGroupRepository : IBaseRepository<ChatGroupDto>
    {
        Task<ChatGroupDto> GetByChatId(string chatId);
    }

    public class ChatGroupRepository : BaseRepository<ChatGroupDto>, IChatGroupRepository
    {
        public ChatGroupRepository(IMongoDatabase database) : base(database, "chat_group")
        {

        }
        public async Task<ChatGroupDto> GetByChatId(string chatId)
        {
            var filter = Builders<ChatGroupDto>.Filter.And(
                Builders<ChatGroupDto>.Filter.Eq(u => u.ChatId, chatId),
                Builders<ChatGroupDto>.Filter.Eq(u => u.IsDeleted, false)
            );
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

    }
}
