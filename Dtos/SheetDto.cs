using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ForwardMessage.Dtos
{
    [BsonIgnoreExtraElements]
    public class SheetDto
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string Username { get; set; }
        public string ChatId { get; set; }



        [BsonElement("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
