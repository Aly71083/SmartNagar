using System.ComponentModel.DataAnnotations;

namespace SmartNagar.Models
{
    public class EmergencyAlert
    {
        public int Id { get; set; }

        [Required]
        public string CitizenId { get; set; } = "";

        public User? Citizen { get; set; }

        [Required, MaxLength(50)]
        public string AlertType { get; set; } = "";

        [Required, MaxLength(20)]
        public string Priority { get; set; } = "URGENT";

        [Required, MaxLength(20)]
        public string Status { get; set; } = "Pending";

        [MaxLength(500)]
        public string Message { get; set; } = "";

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AcknowledgedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}