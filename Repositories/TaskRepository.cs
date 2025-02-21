using MongoDB.Driver;
using ForwardMessage.Dtos;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ForwardMessage.Repositories
{
    public interface ITaskRepository : IBaseRepository<TaskDto>
    {

    }

    public class TaskRepository : BaseRepository<TaskDto>, ITaskRepository
    {
        public TaskRepository(IMongoDatabase database) : base(database, "tasks")
        {
        }


    }
}
