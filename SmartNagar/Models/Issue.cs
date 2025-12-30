using System;
using System.ComponentModel.DataAnnotations;

namespace SmartNagar.Models
{
    public class Issue
    {
        public int Id { get; set; }

        [Required]
        public string Category { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string Location { get; set; }

        public string ImagePath { get; set; }

        public string Status { get; set; }

        public DateTime CreatedDate { get; set; }

        public string UserId { get; set; }
    }
}
