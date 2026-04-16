using System.ComponentModel.DataAnnotations;

namespace SmartNagar.ViewModels
{
    public class AddReviewVM
    {
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        [StringLength(600)]
        public string Comment { get; set; } = string.Empty;
    }
}