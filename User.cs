using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SmartNagar.Models
{
    public class User : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        [Required]
        public string Role { get; set; }   // Citizen, Admin, Officer

        [MaxLength(200)]
        public string? Address { get; set; }

        
        public bool IsActive { get; set; }


    }
}


