using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ForwardMessage.Dtos
{
    [BsonIgnoreExtraElements]
    public class TaskDto
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string SearchKey { get; set; }
        //public string CompetitorLink { get; set; }
        //public string TargetLink { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }

        public string[] ChatId { get; set; }
        public bool IsDeleted { get; set; } = false;
        public TaskStatus Status { get; set; }

        [BsonElement("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime SendTime { get; set; } = DateTime.UtcNow;

        public ObjectId CreatedBy { get; set; }
    }
}
