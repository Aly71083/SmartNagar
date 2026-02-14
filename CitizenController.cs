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
        private readonly IWebHostEnvironment _env; // ✅ for saving uploads to wwwroot

        public CitizenController(
            ApplicationDbContext db,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IWebHostEnvironment env // ✅ add this
        )
        {
            _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
            _env = env;
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
        // ✅ SUBMIT COMPLAINT (POST) + UPLOAD PHOTOS + NOTIFICATIONS
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitComplaint(ComplaintWizardVM vm)
        {
            var user = await CurrentUser();
            ViewBag.FullName = user.FullName ?? "Citizen";

            // Extra server-side check for category
            if (string.IsNullOrWhiteSpace(vm.Category))
                ModelState.AddModelError("Category", "Category is required.");

            // Validate photos: max 5, max 5MB, images only
            if (vm.Photos != null && vm.Photos.Count > 0)
            {
                if (vm.Photos.Count > 5)
                    ModelState.AddModelError("Photos", "Max 5 images allowed.");

                foreach (var f in vm.Photos)
                {
                    if (f == null || f.Length == 0) continue;

                    if (f.Length > 5 * 1024 * 1024)
                        ModelState.AddModelError("Photos", "Each image must be 5MB or less.");

                    if (string.IsNullOrWhiteSpace(f.ContentType) || !f.ContentType.StartsWith("image/"))
                        ModelState.AddModelError("Photos", "Only image files are allowed.");
                }
            }

            if (!ModelState.IsValid)
                return View(vm);

            // ✅ Map category values coming from your UI to your real list
            var category = MapCategory(vm.Category);

            // ✅ Save complaint first to get ID
            var complaint = new Complaint
            {
                Category = category,
                Title = vm.Title ?? "",
                Description = vm.Description ?? "",
                Status = "Pending",
                CitizenId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ResolvedAt = null,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Complaints.Add(complaint);
            await _db.SaveChangesAsync(); // complaint.Id available now

            // ✅ Save uploaded photos into wwwroot/uploads/complaints/{id}/ + DB records
            if (vm.Photos != null && vm.Photos.Count > 0)
            {
                var webRoot = _env.WebRootPath;
                var absFolder = Path.Combine(webRoot, "uploads", "complaints", complaint.Id.ToString());
                if (!Directory.Exists(absFolder))
                    Directory.CreateDirectory(absFolder);

                foreach (var file in vm.Photos)
                {
                    if (file == null || file.Length == 0) continue;

                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                    if (!allowedExt.Contains(ext)) continue;

                    var safeName = $"{Guid.NewGuid():N}{ext}";
                    var absPath = Path.Combine(absFolder, safeName);

                    using (var stream = new FileStream(absPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // This is what <img src="..."> will use
                    var dbPath = $"/uploads/complaints/{complaint.Id}/{safeName}";

                    _db.ComplaintPhotos.Add(new ComplaintPhoto
                    {
                        ComplaintId = complaint.Id,
                        FilePath = dbPath,
                        OriginalName = Path.GetFileName(file.FileName),
                        ContentType = file.ContentType,
                        UploadedAt = DateTime.UtcNow
                    });
                }

                await _db.SaveChangesAsync();
            }

            // ✅ Notify officer + admin (bell updates)
            _db.CitizenNotifications.Add(new CitizenNotification
            {
                CitizenId = user.Id,
                Title = "New Complaint Submitted",
                Message = $"New complaint \"{complaint.Title}\" submitted by {user.FullName}.",
                Type = "ComplaintUpdate",
                ComplaintId = complaint.Id,
                TargetRole = "Officer",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            _db.CitizenNotifications.Add(new CitizenNotification
            {
                CitizenId = user.Id,
                Title = "New Complaint Submitted",
                Message = $"New complaint \"{complaint.Title}\" submitted by {user.FullName}.",
                Type = "ComplaintUpdate",
                ComplaintId = complaint.Id,
                TargetRole = "Admin",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            TempData["Msg"] = "✅ Complaint submitted successfully!";
            return RedirectToAction(nameof(MyComplaints));
        }

        // ✅ maps your step-1 card values to your official list in Complaint.Categories
        private static string MapCategory(string raw)
        {
            raw = (raw ?? "").Trim().ToLowerInvariant();

            return raw switch
            {
                "road" => "Roads & Infastructure",
                "water" => "Water Supply",
                "garbage" => "Garbage Collection",
                "street light" => "Street Lights",
                "drainage" => "Drainage and Sewage",
                "parks" => "Parks & Gradens",
                "illegal construction" => "Illegal Construction",
                "noise" => "Noise Pollution",
                "stray animals" => "Stray Animals",
                "electricity" => "Electricity ",
                "air pollution" => "Air Pollution",
                "other" => "Other Issues",
                _ => "Other Issues"
            };
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
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeactivateAccount()
        {
            TempData["Msg"] = "Deactivate feature is disabled for now.";
            return RedirectToAction(nameof(MyProfile));
        }

        // =========================
        // ✅ DELETE ACCOUNT
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

        // =========================
        // ✅ QUICK RESET (keep if you want)
        // =========================
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> QuickReset()
        {
            var email = "walnutbrownie991@gmail.com";
            var newPassword = "NewPass@123";

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return Content("User not found");

            user.IsActive = true;
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            user.LockoutEnabled = false;
            await _userManager.UpdateAsync(user);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
                return Content("Reset failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));

            return Content("DONE ✅ Password reset to NewPass@123. Now login.");
        }
    }
}