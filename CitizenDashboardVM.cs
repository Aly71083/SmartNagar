namespace SmartNagar.ViewModels
{
    public class CitizenDashboardVM
    {
        public string FullName { get; set; } = "Citizen";
        public int TotalComplaints { get; set; }
        public int InProgress { get; set; }
        public int Pending { get; set; }
        public int Resolved { get; set; }
    }
}
