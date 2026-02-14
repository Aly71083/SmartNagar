using System.ComponentModel.DataAnnotations;

namespace SmartNagar.Models
{
    public class CitizenNotification
    {
        public int Id { get; set; }

        [Required]
        public string CitizenId { get; set; } = "";
        public User? Citizen { get; set; }

        [Required, MaxLength(120)]
        public string Title { get; set; } = "";

        [Required, MaxLength(400)]
        public string Message { get; set; } = "";

        [MaxLength(50)]
        public string Type { get; set; } = "General"; // ComplaintUpdate / Notice / General

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // optional: link to complaint
        public int? ComplaintId { get; set; }
    }
}
