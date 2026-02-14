using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartNagar.Data;
using SmartNagar.Models;
using SmartNagar.ViewModels;

namespace SmartNagar.Controllers
{
    [Authorize(Roles = "Citizen")]
    public class CitizenController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public CitizenController(
            ApplicationDbContext db,
            UserManager<User> userManager,
            SignInManager<User> signInManager
        )
        {
            _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        private async Task<User> CurrentUser()
        {
            var user = await _userManager.GetUserAsync(User);
            return user!;
        }

        // =========================
        // ✅ DASHBOARD
        // =========================
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var user = await CurrentUser();

            var myComplaints = _db.Complaints.Where(c => c.CitizenId == user.Id);

            var vm = new CitizenDashboardVM
            {
                FullName = user.FullName ?? "Citizen",
                TotalComplaints = await myComplaints.CountAsync(),
                Pending = await myComplaints.CountAsync(x => x.Status == "Pending"),
                InProgress = await myComplaints.CountAsync(x => x.Status == "In Progress"),
                Resolved = await myComplaints.CountAsync(x => x.Status == "Resolved"),
            };

            ViewBag.FullName = vm.FullName;
            return View(vm);
        }

        // =========================
        // ✅ MY COMPLAINTS
        // =========================
        [HttpGet]
        public async Task<IActionResult> MyComplaints()
        {
            var user = await CurrentUser();

            var list = await _db.Complaints
                .Where(c => c.CitizenId == user.Id)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.FullName = user.FullName ?? "Citizen";
            return View(list);
        }

        // =========================
        // ✅ SUBMIT COMPLAINT (GET)
        // =========================
        [HttpGet]
        public async Task<IActionResult> SubmitComplaint()
        {
            var user = await CurrentUser();
            ViewBag.FullName = user.FullName ?? "Citizen";

            var vm = new ComplaintWizardVM
            {
                Email = user.Email
            };

            return View(vm);
        }

        // =========================
        // ✅ SUBMIT COMPLAINT (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitComplaint(ComplaintWizardVM vm)
        {
            var user = await CurrentUser();
            ViewBag.FullName = user.FullName ?? "Citizen";

            if (!ModelState.IsValid)
                return View(vm);

            // Save only columns that exist in your Complaint table currently
            var complaint = new Complaint
            {
                Category = vm.Category ?? "Other",
                Title = vm.Title ?? "",
                Description = vm.Description ?? "",
                Status = "Pending",
                CitizenId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ResolvedAt = null
            };

            _db.Complaints.Add(complaint);
            await _db.SaveChangesAsync();

            TempData["Msg"] = "✅ Complaint submitted successfully!";
            return RedirectToAction(nameof(MyComplaints));
        }

        // =========================
        // ✅ TRACK STATUS
        // =========================
        [HttpGet]
        public async Task<IActionResult> TrackStatus(int? id)
        {
            var user = await CurrentUser();
            ViewBag.FullName = user.FullName ?? "Citizen";

            if (id == null) return View(null);

            var complaint = await _db.Complaints
                .FirstOrDefaultAsync(c => c.Id == id && c.CitizenId == user.Id);

            if (complaint == null)
            {
                ViewBag.Error = "Complaint not found for this ID.";
                return View(null);
            }

            return View(complaint);
        }

        // =========================
        // ✅ EMERGENCY
        // =========================
        [HttpGet]
        public async Task<IActionResult> Emergency()
        {
            var user = await CurrentUser();
            ViewBag.FullName = user.FullName ?? "Citizen";
            return View();
        }

        // =========================
        // ✅ GARBAGE SCHEDULE (GET)
        // =========================
        [HttpGet]
        public async Task<IActionResult> GarbageSchedule()
        {
            var user = await CurrentUser();
            ViewBag.FullName = user.FullName ?? "Citizen";
            return View();
        }

        // =========================
        // ✅ SAVE GARBAGE REMINDER (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveGarbageReminder(int wardNumber, string days, string time, string? notes)
        {
            var user = await CurrentUser();

            if (wardNumber <= 0)
            {
                TempData["Msg"] = "Please select a valid ward.";
                return RedirectToAction(nameof(GarbageSchedule));
            }

            var already = await _db.GarbageReminders
                .AnyAsync(r => r.CitizenId == user.Id && r.WardNumber == wardNumber);

            if (already)
            {
                TempData["Msg"] = "Reminder already saved for this ward.";
                return RedirectToAction(nameof(MyGarbageReminders));
            }

            var reminder = new GarbageReminder
            {
                CitizenId = user.Id,
                WardNumber = wardNumber,
                CollectionDays = string.IsNullOrWhiteSpace(days) ? "Sunday – Friday" : days,
                CollectionTime = string.IsNullOrWhiteSpace(time) ? "6:00 AM – 10:00 AM" : time,
                Notes = string.IsNullOrWhiteSpace(notes) ? "All Types of Waste" : notes,
                CreatedAt = DateTime.UtcNow
            };

            _db.GarbageReminders.Add(reminder);
            await _db.SaveChangesAsync();

            TempData["Msg"] = "✅ Garbage reminder saved!";
            return RedirectToAction(nameof(MyGarbageReminders));
        }

        // =========================
        // ✅ LIST MY REMINDERS
        // =========================
        [HttpGet]
        public async Task<IActionResult> MyGarbageReminders()
        {
            var user = await CurrentUser();

            var list = await _db.GarbageReminders
                .Where(r => r.CitizenId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.FullName = user.FullName ?? "Citizen";
            return View(list);
        }

        // =========================
        // ✅ DELETE REMINDER
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGarbageReminder(int id)
        {
            var user = await CurrentUser();

            var reminder = await _db.GarbageReminders
                .FirstOrDefaultAsync(r => r.Id == id && r.CitizenId == user.Id);

            if (reminder != null)
            {
                _db.GarbageReminders.Remove(reminder);
                await _db.SaveChangesAsync();
                TempData["Msg"] = "✅ Reminder deleted!";
            }

            return RedirectToAction(nameof(MyGarbageReminders));
        }

        // =========================
        // ✅ NOTICES
        // =========================
        [HttpGet]
        public async Task<IActionResult> Notices()
        {
            var user = await CurrentUser();
            ViewBag.FullName = user.FullName ?? "Citizen";

            var notices = await _db.Notices
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notices);
        }

        // =========================
        // ✅ MY PROFILE (GET)
        // =========================
        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            ViewBag.FullName = user.FullName ?? "Citizen";

            var vm = new ProfileViewModel
            {
                FullName = user.FullName ?? "",
                Address = user.Address ?? "",
                Email = user.Email ?? ""
            };

            return View(vm);
        }

        // =========================
        // ✅ MY PROFILE (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MyProfile(ProfileViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            user.FullName = vm.FullName;
            user.Address = vm.Address;

            await _userManager.UpdateAsync(user);

            TempData["Msg"] = "✅ Profile updated successfully!";
            return RedirectToAction(nameof(MyProfile));
        }

        // =========================
        // 🚫 DEACTIVATE ACCOUNT (DISABLED FOR NOW)
        // URL: POST /Citizen/DeactivateAccount
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeactivateAccount()
        {
            TempData["Msg"] = "Deactivate feature is disabled for now.";
            return RedirectToAction(nameof(MyProfile));
        }

        // =========================
        // ✅ DELETE ACCOUNT (KEEP ONLY IF YOU REALLY WANT)
        // URL: POST /Citizen/DeleteAccount
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            await _signInManager.SignOutAsync();
            await _userManager.DeleteAsync(user);

            TempData["Msg"] = "✅ Your account has been deleted.";
            return RedirectToAction("Register", "Account");
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> QuickReset()
        {
            var email = "walnutbrownie991@gmail.com";
            var newPassword = "NewPass@123";

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return Content("User not found");

            // unlock + activate
            user.IsActive = true;
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            user.LockoutEnabled = false;
            await _userManager.UpdateAsync(user);

            // reset password
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
                return Content("Reset failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));

            return Content("DONE ✅ Password reset to NewPass@123. Now login.");
        }
    }
}