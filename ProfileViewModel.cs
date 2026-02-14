using System.ComponentModel.DataAnnotations;

namespace SmartNagar.ViewModels
{
    public class ProfileViewModel
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

        public string? TotalComplaints { get; set; }

            public string? ResolvedComplaints { get; set; }
    
            public string? MemberYears { get; set; }

    }
}
