using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ForwardMessage.Dtos
{
    [BsonIgnoreExtraElements]
    public class ChatGroupDto
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string ChatId { get; set; }
        public string ChatName { get; set; }
        [BsonElement("WorkStartTime")]
        public TimeSpan WorkStartTime { get; set; }
        [BsonElement("WorkEndTime")]
        public TimeSpan WorkEndTime { get; set; }

        public bool IsDeleted { get; set; } = false;

        [BsonElement("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ObjectId CreatedBy { get; set; }
    }
}
