using System.ComponentModel.DataAnnotations;

namespace ForwardMessage.Models
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "Username là bắt buộc.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Fullname là bắt buộc.")]
        public string Fullname { get; set; }

        [Required(ErrorMessage = "Password là bắt buộc.")]
        [MinLength(6, ErrorMessage = "Password phải có ít nhất 6 ký tự.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "TeleUser là bắt buộc.")]
        public string TeleUser { get; set; }

        [Required(ErrorMessage = "ConfirmPassword là bắt buộc.")]
        [Compare("Password", ErrorMessage = "Password và ConfirmPassword phải khớp.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Role là bắt buộc.")]
        public UserRole Role { get; set; }
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Username là bắt buộc.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password là bắt buộc.")]
        [MinLength(6, ErrorMessage = "Password phải có ít nhất 6 ký tự.")]
        public string Password { get; set; }
    }

    public class TokenRequest
    {
        public string RefreshToken { get; set; }

    }
}
