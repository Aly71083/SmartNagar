using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SmartNagar.Models
{
    public class User : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = "";

        [Required]
        public string Role { get; set; } = "";   // Citizen, Admin, Officer

        [MaxLength(200)]
        public string? Address { get; set; }

        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; } = false;

        public double? LastLat { get; set; }
        public double? LastLng { get; set; }
        public DateTime? LastLocationAt { get; set; }

        [MaxLength(6)]
        public string? EmailOtp { get; set; }

        public DateTime? EmailOtpExpiryUtc { get; set; }

        public DateTime? EmailVerifiedAtUtc { get; set; }
    }
}