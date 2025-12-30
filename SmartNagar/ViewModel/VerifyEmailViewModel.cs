using System.ComponentModel.DataAnnotations;

namespace SmartNagar.Models.ViewModels
{
    public class VerifyEmailViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Verification Code is required")]
        [Display(Name = "Verification Code")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be 6 digits.")]
        public string VerificationCode { get; set; }
    }
}
