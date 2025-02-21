using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace ForwardMessage.Models
{
    public class ChatGroup
    {

        public string Id { get; set; }
        public string ChatId { get; set; }
        public string ChatName { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string WorkStartTime { get; set; }
        public string WorkEndTime { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedName { get; set; }
    }

    public class ChatGroupRequest
    {

        [Required(ErrorMessage = "ChatId là bắt buộc.")]
        public string ChatId { get; set; }

        [Required(ErrorMessage = "ChatName là bắt buộc.")]
        public string ChatName { get; set; }

        [Required(ErrorMessage = "WorkStartTime là bắt buộc.")]
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "WorkStartTime phải có định dạng HH:mm.")]
        public string WorkStartTime { get; set; }

        [Required(ErrorMessage = "WorkEndTime là bắt buộc.")]
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "WorkEndTime phải có định dạng HH:mm.")]
        public string WorkEndTime { get; set; }
    }

}
