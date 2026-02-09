using System.ComponentModel.DataAnnotations;

namespace SmartNagar.Models
{
    public class Complaint
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string Category { get; set; } = "";

        [Required, MaxLength(200)]
        public string Title { get; set; } = "";

        [Required]
        public string Description { get; set; } = "";

        [Required, MaxLength(30)]
        public string Status { get; set; } = "Pending"; // Pending / Resolved

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // optional: citizen link
        public string? CitizenId { get; set; }
        public User? Citizen { get; set; }
    }
}
