namespace SmartNagar.ViewModels
{
    public class OfficerDashboardVM
    {
        public string FullName { get; set; } = "Municipal Officer";

        public int TotalComplaints { get; set; }
        public int PendingComplaints { get; set; }
        public int InProgressComplaints { get; set; }
        public int ResolvedComplaints { get; set; }

        public List<CategoryCountVM> CategoryDistribution { get; set; } = new();
        public List<RecentComplaintRowVM> RecentComplaints { get; set; } = new();
    }

    public class CategoryCountVM
    {
        public string Category { get; set; } = "";
        public int Count { get; set; }
    }

    public class RecentComplaintRowVM
    {
        public int Id { get; set; }
        public string Subject { get; set; } = "";
        public string CitizenName { get; set; } = "Citizen";
        public string Priority { get; set; } = "Normal"; // if you don't have priority, keep "Normal"
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }
    }
}