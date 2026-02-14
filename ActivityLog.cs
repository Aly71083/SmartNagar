using System;

namespace SmartNagar.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }

        public string Type { get; set; } = "";   // "User", "Notice", "Complaint"
        public string Title { get; set; } = "";
        public string Detail { get; set; } = "";

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
