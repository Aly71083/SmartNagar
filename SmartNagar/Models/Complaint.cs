using System.ComponentModel.DataAnnotations;

namespace SmartNagar.Models
{
    public class Complaint
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; } = "";

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required, MaxLength(30)]
        public string Status { get; set; } = "Pending";

        [MaxLength(50)]
        public string? Ward { get; set; }


        [MaxLength(50)]
        public string? Category { get; set; }  

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ResolvedAt { get; set; }


        public string? CitizenId { get; set; }
        public User? Citizen { get; set; }

       
        public static readonly List<string> Categories = new()
        {
            "Roads & Infastructure",
            "Water Supply",
            "Garbage Collection",
            "Street Lights",
            "Drainage and Sewage",
            "Parks & Gradens",
            "Illegal Construction",
            "Noise Pollution",
            "Stray Animals",
            "Electricity ",
            "Air Pollution",
            "Other Issues",


        };

        [MaxLength(2000)]
        public string? OfficerRemarks { get; set; }

        //  Assignment (Officer)
        public string? AssignedOfficerId { get; set; }
        public User? AssignedOfficer { get; set; }

        public DateTime? AssignedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<ComplaintPhoto> Photos { get; set; } = new();
    }
}