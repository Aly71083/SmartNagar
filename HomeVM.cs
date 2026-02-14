namespace SmartNagar.ViewModels
{
    public class HomeVM
    {
        public int ActiveCitizens { get; set; }      // from Users table
        public int ResolvedComplaints { get; set; }  // placeholder until Complaint module (0, not random)
        public int SatisfactionRate { get; set; }    // based on users: ActiveUsers/TotalUsers * 100
        public string ServiceAvailability { get; set; } = "24/7";
    }
}
