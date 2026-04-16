using System.ComponentModel.DataAnnotations;

namespace SmartNagar.ViewModels
{
    public class SendNoticeVM
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = "";

        [Required]
        public string Description { get; set; } = "";

        [Required, MaxLength(20)]
        public string Priority { get; set; } = "Normal"; // Low / Medium / High / Normal
    }
}