using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartNagar.Models;

namespace SmartNagar.Data
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
        public DbSet<Issue> Issues { get; set; }
        // Future Smart Nagar tables will go here
        // Example:
        // public DbSet<Complaint> Complaints { get; set; }
        // public DbSet<Notice> Notices { get; set; }
    }
}
