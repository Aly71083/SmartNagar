using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SmartNagar.ViewModels
{
    public class ComplaintWizardVM
    {
        // STEP 1
        [Required(ErrorMessage = "Please select a category.")]
        public string Category { get; set; } = "";

        // STEP 2
        [Required]
        public string Priority { get; set; } = "Low"; // Low/Medium/High/Critical

        [Required]
        public int WardNumber { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = "";

        [Required]
        public string Description { get; set; } = "";

        [Required, MaxLength(120)]
        public string AreaLocality { get; set; } = "";

        [MaxLength(120)]
        public string? Landmark { get; set; }

        [Required, MaxLength(300)]
        public string Address { get; set; } = "";

        [Required, MaxLength(20)]
        public string ContactNumber { get; set; } = "";

        [EmailAddress]
        public string? Email { get; set; }

        // Optional photos (UI + form submission works; you can store later)
        public List<IFormFile>? Photos { get; set; }
    }
}
