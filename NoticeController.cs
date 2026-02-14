using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartNagar.Data;
using SmartNagar.Models;
using SmartNagar.ViewModels;

namespace SmartNagar.Controllers
{
    [Authorize(Roles = "Admin,MunicipalOfficer")]
    public class NoticeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;

        public NoticeController(ApplicationDbContext db, UserManager<User> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Publish()
        {
            return View(new PublishNoticeVM()); // Views/Notice/Publish.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(PublishNoticeVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _userManager.GetUserAsync(User);

            var notice = new Notice
            {
                Title = vm.Title.Trim(),
                Description = vm.Description.Trim(),
                Priority = vm.Priority,
                CreatedAt = DateTime.UtcNow,

                // ✅ store creator
                CreatedByRole = User.IsInRole("Admin") ? "Admin" : "MunicipalOfficer",
                CreatedByName = user?.FullName ?? "Unknown"
            };

            _db.Notices.Add(notice);

            // optional: log activity if you want
            _db.ActivityLogs.Add(new ActivityLog
            {
                Type = "Notice",
                Title = "New notice published",
                Detail = $"{notice.Title} ({notice.Priority}) by {notice.CreatedByRole}",
                IsRead = false
            });

            await _db.SaveChangesAsync();

            TempData["Msg"] = "✅ Notice published!";
            return RedirectToAction("List");
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var notices = await _db.Notices
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notices); // Views/Notice/List.cshtml
        }
    }
}
