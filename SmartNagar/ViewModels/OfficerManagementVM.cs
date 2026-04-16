using SmartNagar.Models;

namespace SmartNagar.ViewModels
{
    public class OfficerManagementVM
    {
        public List<OfficerOptionVM> Officers { get; set; } = new();
        public List<Complaint> UnassignedComplaints { get; set; } = new();
        public List<Complaint> AssignedComplaints { get; set; } = new();
    }

    public class OfficerOptionVM
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
    }
}