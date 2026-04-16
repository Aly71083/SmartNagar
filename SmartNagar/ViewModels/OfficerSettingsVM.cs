using System.ComponentModel.DataAnnotations;

namespace SmartNagar.ViewModels
{
    public class OfficerSettingsVM
    {
        // Profile
        [Required, MaxLength(100)]
        public string FullName { get; set; } = "";

        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }

        [MaxLength(50)]
        public string? DefaultWard { get; set; }

        // Password
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmNewPassword { get; set; }
    }
}