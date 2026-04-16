using System.Collections.Generic;

namespace SmartNagar.ViewModels
{
    public class SystemOverviewVM
    {
        public int Days { get; set; }

        public int TotalUsers { get; set; }
        public int TotalComplaints { get; set; }
        public int Resolved { get; set; }
        public int Pending { get; set; }
        public double AvgResolutionDays { get; set; }

        public List<string> TrendLabels { get; set; } = new();
        public List<int> TrendValues { get; set; } = new();

        public List<string> CategoryLabels { get; set; } = new();
        public List<int> CategoryValues { get; set; } = new();

        public List<string> StatusLabels { get; set; } = new();
        public List<int> StatusValues { get; set; } = new();

        public List<TopCategoryItem> TopCategories { get; set; } = new();

        public class TopCategoryItem
        {
            public string Category { get; set; } = "";
            public int Count { get; set; }
        }
    }
}
