using System.ComponentModel.DataAnnotations;

namespace SmartNagar.ViewModels
{
    public class VerifyEmailOtpVM
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "OTP is required.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits.")]
        [Display(Name = "OTP Code")]
        public string OtpCode { get; set; } = "";
    }
}