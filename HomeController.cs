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

        public async Task<IActionResult> Index()
        {
            var totalUsers = await _db.Users.CountAsync();
            var activeUsers = await _db.Users.CountAsync(u => u.IsActive);

            var activeCitizens = await _db.Users.CountAsync(u => u.IsActive && u.Role == "Citizen");

            // ? NOT random:
            // - ResolvedComplaints stays 0 until you create Complaint module/table
            // - SatisfactionRate based on users only (active/total)
            int satisfaction = totalUsers == 0 ? 0 : (int)System.Math.Round((activeUsers * 100.0) / totalUsers);

            var vm = new HomeVM
            {
                ActiveCitizens = activeCitizens,
                ResolvedComplaints = 0,
                SatisfactionRate = satisfaction,
                ServiceAvailability = "24/7"
            };

            return View(vm);
        }
    }
}
