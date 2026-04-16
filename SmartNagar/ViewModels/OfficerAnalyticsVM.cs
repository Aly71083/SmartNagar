namespace SmartNagar.ViewModels
{
    public class OfficerAnalyticsVM
    {
        // existing
        public int TotalComplaints { get; set; }
        public int Pending { get; set; }
        public int InProgress { get; set; }
        public int Resolved { get; set; }
        public int Assigned { get; set; }
        public int Unassigned { get; set; }

        public List<KeyValuePair<string, int>> CategoryStats { get; set; } = new();
        public List<KeyValuePair<string, int>> MonthlyStats { get; set; } = new();
        public List<KeyValuePair<string, int>> OfficerLoad { get; set; } = new();

        // NEW: date range
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        // NEW: KPI display helpers (so UI matches screenshot)
        public string TotalComplaintsDeltaText { get; set; } = "0%";
        public string TotalComplaintsDeltaNote { get; set; } = "—";

        public string AvgResponseTimeText { get; set; } = "0h";
        public string AvgResponseDeltaText { get; set; } = "0%";
        public bool AvgResponseDeltaIsBad { get; set; } = false;
        public string AvgResponseDeltaNote { get; set; } = "—";

        public string ResolutionRateText { get; set; } = "0%";
        public string ResolutionRateDeltaText { get; set; } = "0%";
        public string ResolutionRateDeltaNote { get; set; } = "—";

        public string SatisfactionScoreText { get; set; } = "0/5";
        public string SatisfactionDeltaText { get; set; } = "0%";
        public string SatisfactionDeltaNote { get; set; } = "Based on responses";

        // NEW: strip cards
        public int ResolvedWithin48HoursPercent { get; set; } = 0;
        public string ResolvedWithin48HoursText { get; set; } = "0%";
        public string ResolvedWithin48HoursNote { get; set; } = "—";

        public string ActiveUsersText { get; set; } = "0";
        public string ActiveUsersNote { get; set; } = "—";
        public int ActiveUsersBarPercent { get; set; } = 50;

        public string OverallPerformanceText { get; set; } = "0%";
        public string OverallPerformanceNote { get; set; } = "—";
        public int OverallPerformanceBarPercent { get; set; } = 0;

        // NEW: trend points for the chart
        public List<TrendPointVM> TrendPoints { get; set; } = new();

        // NEW: ward table
        public List<WardPerfRowVM> WardStats { get; set; } = new();
    }

    public class TrendPointVM
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class WardPerfRowVM
    {
        public string WardName { get; set; } = "Ward";
        public int Total { get; set; }
        public int Resolved { get; set; }
        public int Pending { get; set; }
        public double AvgResolutionHours { get; set; }
        public string AvgResolutionTimeText => AvgResolutionHours <= 0 ? "-" : $"{AvgResolutionHours:0.0} hours";
        public string PerformanceTag { get; set; } = "Good";
    }
}