namespace SmartNagar.ViewModels
{
    public class ComplaintDetailsVM
    {
        public int Id { get; set; }

        public string Title { get; set; } = "";
        public string? Description { get; set; }

        public string? Category { get; set; }
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; }

        public string CitizenName { get; set; } = "Citizen";
        public string CitizenEmail { get; set; } = "";

        // For update form
        public string NewStatus { get; set; } = "Pending";
    }
}