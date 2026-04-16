using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartNagar.Data;
using SmartNagar.ViewModels;

namespace SmartNagar.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        public HomeController(ApplicationDbContext db) => _db = db;

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> Index()
        {
            var totalUsers = await _db.Users.CountAsync();
            var activeUsers = await _db.Users.CountAsync(u => u.IsActive);
            var activeCitizens = await _db.Users.CountAsync(u => u.IsActive && u.Role == "Citizen");

            var resolvedComplaints = await _db.Complaints.CountAsync(c => c.Status == "Resolved");

            int satisfaction = totalUsers == 0
                ? 0
                : (int)Math.Round((activeUsers * 100.0) / totalUsers);

            var reviews = await _db.Reviews
                .Where(r => r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .Take(6)
                .Select(r => new HomeReviewVM
                {
                    CitizenName = r.CitizenName,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            var vm = new HomeVM
            {
                ActiveCitizens = activeCitizens,
                ResolvedComplaints = resolvedComplaints,
                SatisfactionRate = satisfaction,
                ServiceAvailability = "24/7",
                Reviews = reviews
            };

            return View(vm);


        }
    }
}