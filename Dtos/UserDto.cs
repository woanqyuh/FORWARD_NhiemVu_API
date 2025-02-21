using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ForwardMessage.Dtos
{
    [BsonIgnoreExtraElements]
    public class UserDto
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string Username { get; set; }
        public string Fullname { get; set; }
        public string Password { get; set; }
        public string TeleUser { get; set; }
        public UserRole Role { get; set; }
        public bool IsDeleted { get; set; } = false;

        [BsonElement("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ObjectId CreatedBy { get; set; }
    }
}