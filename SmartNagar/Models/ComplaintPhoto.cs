using System.ComponentModel.DataAnnotations;

namespace SmartNagar.Models
{
    public class ComplaintPhoto
    {
        public int Id { get; set; }

        [Required]
        public int ComplaintId { get; set; }
        public Complaint? Complaint { get; set; }

        [Required, MaxLength(260)]
        public string FilePath { get; set; } = "";  // e.g. /uploads/complaints/12/photo1.jpg

        [MaxLength(120)]
        public string? OriginalName { get; set; }

        [MaxLength(60)]
        public string? ContentType { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}