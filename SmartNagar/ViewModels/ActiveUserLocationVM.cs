namespace SmartNagar.ViewModels
{
    public class ActiveUserLocationVM
    {
        public string UserId { get; set; } = "";
        public string FullName { get; set; } = "";
        public double Lat { get; set; }
        public double Lng { get; set; }
        public DateTime? LastAt { get; set; }
    }
}