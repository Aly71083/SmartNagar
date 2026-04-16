namespace SmartNagar.ViewModels
{
    public class ComplaintListRowVM
    {
        public int Id { get; set; }
        public string ComplaintNo { get; set; } = "";
        public string Subject { get; set; } = "";
        public string CitizenName { get; set; } = "";
        public string Ward { get; set; } = "-";
        public string Priority { get; set; } = "Normal";
        public string Status { get; set; } = "";
        public string DateText { get; set; } = "";
    }
}