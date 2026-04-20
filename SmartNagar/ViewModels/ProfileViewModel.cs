using System.ComponentModel.DataAnnotations;

namespace SmartNagar.ViewModels
{
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Full name can contain only letters and spaces.")]
        [MaxLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        public string FullName { get; set; } = "";

        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; } = "";

        [MaxLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
        public string? Address { get; set; }

        public int TotalComplaints { get; set; }
        public int ResolvedComplaints { get; set; }
        public int MemberYears { get; set; }
    }
}