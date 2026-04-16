namespace SmartNagar.ViewModels
{
    public class HomeVM
    {
        public int ActiveCitizens { get; set; }
        public int ResolvedComplaints { get; set; }
        public int SatisfactionRate { get; set; }
        public string ServiceAvailability { get; set; } = "24/7";

        public List<HomeReviewVM> Reviews { get; set; } = new();
    }

    public class HomeReviewVM
    {
        public string CitizenName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}