using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ForwardMessage.Dtos
{
    [BsonIgnoreExtraElements]
    public class KeyDto
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string SearchKey { get; set; }

        public bool IsDeleted { get; set; } = false;

        [BsonElement("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ObjectId CreatedBy { get; set; }
    }
}
