using System.ComponentModel.DataAnnotations;

namespace SmartNagar.ViewModels
{
    public class ForgotPasswordVM
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";
    }
}