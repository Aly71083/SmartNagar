using SmartNagar.Models;

namespace SmartNagar.ViewModels
{
    public class OfficerAssignmentsVM
    {
        public string? Q { get; set; }
        public string? Status { get; set; }
        public string Sort { get; set; } = "new"; // new|old
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }

        public List<Complaint> Items { get; set; } = new();

        public List<string> Statuses { get; set; } = new() { "Pending", "In Progress", "Resolved", "Rejected" };
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}