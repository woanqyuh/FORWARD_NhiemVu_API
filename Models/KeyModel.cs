using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ForwardMessage.Models
{
    public class KeyModel
    {
        public string Id { get; set; }
        public string SearchKey { get; set; }
        public DateTime CreatedAt { get; set; }

        public string CreatedBy { get; set; }

        public string CreatedName { get; set; }
    }

    public class KeyRequest
    {
        public string SearchKey { get; set; }
    }
}
