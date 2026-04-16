using System.ComponentModel.DataAnnotations;

namespace SmartNagar.Models
{
    public class GarbageSchedule
    {
        public int Id { get; set; }

        [Required]
        public int WardNumber { get; set; }

        [Required]
        [MaxLength(100)]
        public string CollectionDays { get; set; } = "";

        [Required]
        [MaxLength(80)]
        public string CollectionTime { get; set; } = "";

        [MaxLength(200)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}