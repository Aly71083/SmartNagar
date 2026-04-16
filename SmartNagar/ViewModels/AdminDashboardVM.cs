using System.Collections.Generic;
using SmartNagar.Models;

namespace SmartNagar.ViewModels
{
    public class AdminDashboardVM
    {
        public int TotalUsers { get; set; }

       
        public int TotalComplaints { get; set; }
        public int Resolved { get; set; }
        public int Pending { get; set; }

        public List<ActivityLog> RecentActivities { get; set; } = new();
    }
}
