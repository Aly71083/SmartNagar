using System.ComponentModel.DataAnnotations;

namespace SmartNagar.ViewModels
{
    public class PublishNoticeVM
    {
        [Required]
        public string Title { get; set; } = "";

        [Required]
        public string Description { get; set; } = "";

        [Required]
        public string Priority { get; set; } = "Normal"; // Low/Normal/High
    }
}
