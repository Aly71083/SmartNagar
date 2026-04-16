using System.ComponentModel.DataAnnotations;

namespace SmartNagar.ViewModels
{
    public class ComplaintCreateViewModel
    {
        [Required]
        public string Category { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        [MaxLength(300)]
        public string Address { get; set; }   
    }
}
