using System.ComponentModel.DataAnnotations;

namespace SmartNagar.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public string CitizenId { get; set; } = string.Empty;
        public User? Citizen { get; set; }

        [Required]
        [StringLength(120)]
        public string CitizenName { get; set; } = string.Empty;

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        [StringLength(600)]
        public string Comment { get; set; } = string.Empty;

        public bool IsApproved { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}