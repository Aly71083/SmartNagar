using System.Collections.Generic;

namespace SmartNagar.ViewModels
{
    public class OfficerDashboardVM
    {
        public int TotalComplaints { get; set; }
        public int PendingReview { get; set; }
        public int InProgress { get; set; }
        public int Resolved { get; set; }
        public int CriticalIssues { get; set; }

        public List<ComplaintListRowVM> RecentComplaints { get; set; } = new();
        public List<KeyValuePair<string, int>> TopCategories { get; set; } = new();
    }
}