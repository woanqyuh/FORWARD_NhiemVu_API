using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ForwardMessage.Models
{
    public class TaskModel
    {

        public string Id { get; set; }
        public string SearchKey { get; set; }
        //public string CompetitorLink { get; set; }
        //public string TargetLink { get; set; }
        public string ImageUrl { get; set; }

        public string Content { get; set; }
        
        public string[] ChatId { get; set; }

        public List<ChatGroup> ChatGroup { get; set; }
        public bool IsDeleted { get; set; } = false;
        public TaskStatus Status { get; set; }

        [BsonElement("CreatedAt")]
        public DateTime CreatedAt { get; set; } 
        public DateTime SendTime { get; set; } 
        public string CreatedBy { get; set; }

        public string CreatedName { get; set; }
    }
}
