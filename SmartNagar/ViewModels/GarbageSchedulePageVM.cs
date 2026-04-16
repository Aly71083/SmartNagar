using SmartNagar.Models;

namespace SmartNagar.ViewModels
{
    public class GarbageSchedulePageVM
    {
        public GarbageSchedule Form { get; set; } = new GarbageSchedule();
        public List<GarbageSchedule> Schedules { get; set; } = new();
        public bool IsEdit => Form != null && Form.Id > 0;
    }
}