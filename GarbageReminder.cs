using System.ComponentModel.DataAnnotations;

namespace SmartNagar.Models
{
    public class GarbageReminder
    {
        public int Id { get; set; }

        [Required]
        public string CitizenId { get; set; } = "";

        [Required]
        public int WardNumber { get; set; }

        [Required, MaxLength(100)]
        public string CollectionDays { get; set; } = "Sunday – Friday";

        [Required, MaxLength(80)]
        public string CollectionTime { get; set; } = "6:00 AM – 10:00 AM";

        [MaxLength(200)]
        public string? Notes { get; set; } = "All Types of Waste";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
