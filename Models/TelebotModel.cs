using System.ComponentModel.DataAnnotations;

namespace ForwardMessage.Models
{
    public class TelebotModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "SearchKey là bắt buộc.")]
        public string SearchKey { get; set; }

        //[Required(ErrorMessage = "Link trỏ là bắt buộc")]
        //public string CompetitorLink { get; set; }
        //public string TargetLink { get; set; }
        [Required(ErrorMessage = "Nội dung là bắt buộc")]
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        [Required(ErrorMessage = "Chat Id là bắt buộc")]
        public string[] ChatId { get; set; }
    }

    public class ErrorSendResponse
    {
        public string ChatId { get; set; }

        public string ChatName { get; set; }

        public string ChatError { get; set; }
    }
}
