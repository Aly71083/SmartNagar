namespace SmartNagar.ViewModels
{
    public class ExportDataVM
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        public string? Ward { get; set; }
        public string? Status { get; set; }

        public string Format { get; set; } = "pdf";

        public bool Complaints { get; set; }
        public bool Citizens { get; set; }
        public bool Analytics { get; set; }
    }
}