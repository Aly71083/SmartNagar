using System.ComponentModel.DataAnnotations;

namespace SmartNagar.Models.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "New Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]).{8,}$",
            ErrorMessage = "Password must be at least 8 characters long, contain at least one uppercase letter, one digit, and one special character.")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm New Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmNewPassword { get; set; }
    }
}
