using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartNagar.Data;
using SmartNagar.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SmartNagar.Controllers
{
    [Authorize]
    public class IssueController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public IssueController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================
        // 1. ALL ISSUES (Admin View)
        // =========================
        public async Task<IActionResult> Index()
        {
            var issues = await _context.Issues
                .OrderByDescending(i => i.CreatedDate)
                .ToListAsync();

            return View(issues);
        }

        // =========================
        // 2. MY ISSUES (Citizen)
        // =========================
        public async Task<IActionResult> MyIssues()
        {
            var user = await _userManager.GetUserAsync(User);

            var issues = await _context.Issues
                .Where(i => i.UserId == user.Id)
                .OrderByDescending(i => i.CreatedDate)
                .ToListAsync();

            return View(issues);
        }

        // =========================
        // 3. CREATE ISSUE (GET)
        // =========================
        public IActionResult Create()
        {
            return View();
        }

        // =========================
        // 4. CREATE ISSUE (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Issue issue, IFormFile ImageFile)
        {
            if (!ModelState.IsValid)
                return View(issue);

            var user = await _userManager.GetUserAsync(User);

            // Image upload
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/issues");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                issue.ImagePath = "/uploads/issues/" + fileName;
            }

            issue.UserId = user.Id;
            issue.Status = "Pending";
            issue.CreatedDate = DateTime.Now;

            _context.Issues.Add(issue);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyIssues));
        }

        // =========================
        // 5. ISSUE DETAILS
        // =========================
        public async Task<IActionResult> Details(int id)
        {
            var issue = await _context.Issues.FindAsync(id);

            if (issue == null)
                return NotFound();

            return View(issue);
        }

        // =========================
        // 6. UPDATE STATUS (Admin)
        // =========================
        public async Task<IActionResult> UpdateStatus(int id)
        {
            var issue = await _context.Issues.FindAsync(id);
            if (issue == null)
                return NotFound();

            return View(issue);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var issue = await _context.Issues.FindAsync(id);

            if (issue == null)
                return NotFound();

            issue.Status = status;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
