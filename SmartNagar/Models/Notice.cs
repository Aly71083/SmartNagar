using System;
using System.ComponentModel.DataAnnotations;

namespace SmartNagar.Models
{
    public class Notice
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = "";

        [Required]
        public string Description { get; set; } = "";

        [Required, MaxLength(20)]
        public string Priority { get; set; } = "Normal"; // Low, Medium, High

        // ✅ NEW
        [MaxLength(50)]
        public string CreatedByRole { get; set; } = "";   // Admin / MunicipalOfficer

        [MaxLength(120)]
        public string CreatedByName { get; set; } = "";   // FullName

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
