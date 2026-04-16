using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartNagar.Models;

namespace SmartNagar.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Notice> Notices { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<CitizenNotification> CitizenNotifications { get; set; }
        public DbSet<GarbageReminder> GarbageReminders { get; set; }
        public DbSet<ComplaintPhoto> ComplaintPhotos { get; set; }
        public DbSet<Review> Reviews { get; set; }

       

        public DbSet<EmergencyAlert> EmergencyAlerts { get; set; }
        public DbSet<GarbageSchedule> GarbageSchedules { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<GarbageReminder>()
                .HasOne(gr => gr.Citizen)
                .WithMany()
                .HasForeignKey(gr => gr.CitizenId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}