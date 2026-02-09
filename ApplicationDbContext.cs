using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartNagar.Models;

namespace SmartNagar.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Complaint> Complaints => Set<Complaint>();
        public DbSet<Notice> Notices => Set<Notice>();
    }
}
