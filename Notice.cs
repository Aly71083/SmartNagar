using System.ComponentModel.DataAnnotations;

namespace SmartNagar.Models
{
    public class Notice
    {
        public int Id { get; set; }

        [Required, MaxLength(140)]
        public string Title { get; set; } = "";

        [Required]
        public string Description { get; set; } = "";

        [Required, MaxLength(20)]
        public string Priority { get; set; } = "Normal"; // Low/Normal/High

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }
    }
}
