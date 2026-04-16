using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartNagar.Data;
using SmartNagar.Models;
using SmartNagar.ViewModels;
using SmartNagar.Services;

namespace SmartNagar.Controllers
{
    [Authorize(Roles = "Citizen")]
    public class CitizenController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailService _emailService;

        public CitizenController(
            ApplicationDbContext db,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IWebHostEnvironment env,
            IEmailService emailService
        )
        {
            _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
            _env = env;
            _emailService = emailService;
        }

        public record LocationDto(double Lat, double Lng);

        private async Task<User> CurrentUser()
        {
            var user = await _userManager.GetUserAsync(User);
            return user!;
        }

        // DASHBOARD
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
            ViewBag.LastLat = user.LastLat;
            ViewBag.LastLng = user.LastLng;
            ViewBag.LastLocationAt = user.LastLocationAt;

            return View(vm);
        }

        // SAVE MY LOCATION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveMyLocation([FromBody] LocationDto dto)
        {
            var user = await CurrentUser();

            if (!user.IsActive)
                return Forbid();

            user.LastLat = dto.Lat;
            user.LastLng = dto.Lng;
            user.LastLocationAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);
            return Ok(new { ok = true });
        }

        // MY COMPLAINTS
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

        // SUBMIT COMPLAINT (GET)
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

        // SUBMIT COMPLAINT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitComplaint(ComplaintWizardVM vm)
        {
            var user = await CurrentUser();
            ViewBag.FullName = user.FullName ?? "Citizen";

            if (string.IsNullOrWhiteSpace(vm.Category))
                ModelState.AddModelError("Category", "Category is required.");

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

            var category = MapCategory(vm.Category);

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
            await _db.SaveChangesAsync();

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

        // TRACK STATUS
        [HttpGet]
        public async Task<IActionResult> TrackStatus(int? id)
        {
            var user = await CurrentUser();
            ViewBag.FullName = user.FullName ?? "Citizen";
            ViewBag.Active = "trackstatus";

            if (id == null)
                return View(null);

            var complaint = await _db.Complaints
                .Include(c => c.Photos)
                .Include(c => c.Citizen)
                .Include(c => c.AssignedOfficer)
                .FirstOrDefaultAsync(c => c.Id == id && c.CitizenId == user.Id);

            if (complaint == null)
            {
                ViewBag.Error = "Complaint not found for this ID.";
                return View(null);
            }

            return View(complaint);
        }

        // EMERGENCY PAGE
        [HttpGet]
        public async Task<IActionResult> Emergency()
        {
            var user = await CurrentUser();
            ViewBag.FullName = user.FullName ?? "Citizen";
            ViewBag.Active = "Emergency";
            return View();
        }

        // SEND DIFFERENT EMERGENCY ALERTS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendEmergencyAlert(string alertType)
        {
            var user = await CurrentUser();

            if (user == null)
                return Unauthorized();

            alertType = (alertType ?? "").Trim();

            var allowedAlerts = new[]
            {
                "Police",
                "Ambulance",
                "Fire",
                "ArmedPolice",
                "WomenChildrenSenior",
                "Traffic",
                "TouristPolice"
            };

            if (!allowedAlerts.Contains(alertType))
                return BadRequest(new { ok = false, message = "Invalid alert type." });

            var citizenName = string.IsNullOrWhiteSpace(user.FullName) ? "Citizen" : user.FullName;

            var locationText = (user.LastLat.HasValue && user.LastLng.HasValue)
                ? $" Location: Lat {user.LastLat.Value:F6}, Lng {user.LastLng.Value:F6}."
                : " Location not shared yet.";

            string title;
            string message;
            string activityTitle;
            string activityDetail;

            switch (alertType)
            {
                case "Police":
                    title = "🚓 Police Emergency Alert";
                    message = $"Police emergency alert sent by {citizenName}.{locationText}";
                    activityTitle = "🚓 Police Emergency Alert";
                    activityDetail = $"{citizenName} triggered a police emergency alert.{locationText}";
                    break;

                case "Ambulance":
                    title = "🚑 Ambulance Emergency Alert";
                    message = $"Ambulance emergency alert sent by {citizenName}.{locationText}";
                    activityTitle = "🚑 Ambulance Emergency Alert";
                    activityDetail = $"{citizenName} triggered an ambulance emergency alert.{locationText}";
                    break;

                case "Fire":
                    title = "🔥 Fire Emergency Alert";
                    message = $"Fire emergency alert sent by {citizenName}.{locationText}";
                    activityTitle = "🔥 Fire Emergency Alert";
                    activityDetail = $"{citizenName} triggered a fire emergency alert.{locationText}";
                    break;

                case "ArmedPolice":
                    title = "🚨 Armed Police Emergency Alert";
                    message = $"Armed police emergency alert sent by {citizenName}.{locationText}";
                    activityTitle = "🚨 Armed Police Emergency Alert";
                    activityDetail = $"{citizenName} triggered an armed police emergency alert.{locationText}";
                    break;

                case "WomenChildrenSenior":
                    title = "👩‍🦰 Women / Child / Senior Emergency Alert";
                    message = $"Women / child / senior emergency alert sent by {citizenName}.{locationText}";
                    activityTitle = "👩‍🦰 Women / Child / Senior Emergency Alert";
                    activityDetail = $"{citizenName} triggered a women / child / senior emergency alert.{locationText}";
                    break;

                case "Traffic":
                    title = "🚦 Traffic Emergency Alert";
                    message = $"Traffic emergency alert sent by {citizenName}.{locationText}";
                    activityTitle = "🚦 Traffic Emergency Alert";
                    activityDetail = $"{citizenName} triggered a traffic emergency alert.{locationText}";
                    break;

                case "TouristPolice":
                    title = "🛫 Tourist Police Emergency Alert";
                    message = $"Tourist police emergency alert sent by {citizenName}.{locationText}";
                    activityTitle = "🛫 Tourist Police Emergency Alert";
                    activityDetail = $"{citizenName} triggered a tourist police emergency alert.{locationText}";
                    break;

                default:
                    return BadRequest(new { ok = false, message = "Invalid alert type." });
            }

            _db.CitizenNotifications.Add(new CitizenNotification
            {
                CitizenId = user.Id,
                Title = title,
                Message = message,
                Type = "Emergency",
                ComplaintId = null,
                TargetRole = "Officer",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            _db.CitizenNotifications.Add(new CitizenNotification
            {
                CitizenId = user.Id,
                Title = title,
                Message = message,
                Type = "Emergency",
                ComplaintId = null,
                TargetRole = "Admin",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            _db.ActivityLogs.Add(new ActivityLog
            {
                Type = "Emergency",
                Title = activityTitle,
                Detail = activityDetail,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return Ok(new
            {
                ok = true,
                message = $"{title} sent successfully."
            });
        }

        // GARBAGE SCHEDULE (ADMIN-MANAGED)
        [HttpGet]
        public async Task<IActionResult> GarbageSchedule()
        {
            var user = await CurrentUser();

            var schedules = await _db.GarbageSchedules
                .OrderBy(x => x.WardNumber)
                .ToListAsync();

            ViewBag.FullName = user.FullName ?? "Citizen";
            ViewBag.Active = "garbageschedule";
            return View(schedules);
        }

        // SAVE GARBAGE REMINDER
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveGarbageReminder(int wardNumber, DateTime reminderDateTime)
        {
            var user = await CurrentUser();

            if (wardNumber <= 0)
            {
                TempData["Msg"] = "Please select a valid ward.";
                return RedirectToAction(nameof(GarbageSchedule));
            }

            if (reminderDateTime == default)
            {
                TempData["Msg"] = "Please choose a valid reminder date and time.";
                return RedirectToAction(nameof(GarbageSchedule));
            }

            if (reminderDateTime <= DateTime.Now)
            {
                TempData["Msg"] = "Reminder date and time must be in the future.";
                return RedirectToAction(nameof(GarbageSchedule));
            }

            var schedule = await _db.GarbageSchedules
                .FirstOrDefaultAsync(x => x.WardNumber == wardNumber);

            if (schedule == null)
            {
                TempData["Msg"] = "No schedule currently available for this ward.";
                return RedirectToAction(nameof(GarbageSchedule));
            }

            var already = await _db.GarbageReminders
                .AnyAsync(r =>
                    r.CitizenId == user.Id &&
                    r.WardNumber == wardNumber &&
                    !r.IsEmailSent);

            if (already)
            {
                TempData["Msg"] = "Reminder already saved for this ward.";
                return RedirectToAction(nameof(MyGarbageReminders));
            }

            var reminder = new GarbageReminder
            {
                CitizenId = user.Id,
                WardNumber = wardNumber,
                CollectionDays = schedule.CollectionDays,
                CollectionTime = schedule.CollectionTime,
                Notes = string.IsNullOrWhiteSpace(schedule.Notes) ? "No additional notes" : schedule.Notes,
                ReminderDateTimeUtc = reminderDateTime.ToUniversalTime(),
                IsEmailSent = false,
                EmailSentAtUtc = null,
                CreatedAt = DateTime.UtcNow
            };

            _db.GarbageReminders.Add(reminder);
            await _db.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var subject = "Smart Nagar Garbage Reminder Scheduled";
                var body = $@"
                    <h2>Garbage Reminder Scheduled</h2>
                    <p>Hello {user.FullName},</p>
                    <p>Your garbage reminder has been scheduled successfully.</p>
                    <p><strong>Ward:</strong> {reminder.WardNumber}</p>
                    <p><strong>Collection Days:</strong> {reminder.CollectionDays}</p>
                    <p><strong>Collection Time:</strong> {reminder.CollectionTime}</p>
                    <p><strong>Notes:</strong> {reminder.Notes}</p>
                    <p><strong>Reminder Time:</strong> {reminderDateTime:yyyy-MM-dd hh:mm tt}</p>
                    <p>You will receive an automatic email reminder at the scheduled time.</p>
                    <p>Thank you,<br/>Smart Nagar</p>";

                try
                {
                    await _emailService.SendEmailAsync(user.Email!, subject, body);
                    TempData["Msg"] = "✅ Reminder scheduled and confirmation email sent!";
                }
                catch
                {
                    TempData["Msg"] = "✅ Reminder scheduled, but confirmation email could not be sent.";
                }
            }
            else
            {
                TempData["Msg"] = "✅ Reminder scheduled successfully!";
            }

            return RedirectToAction(nameof(MyGarbageReminders));
        }

        // MY GARBAGE REMINDERS
        [HttpGet]
        public async Task<IActionResult> MyGarbageReminders()
        {
            var user = await CurrentUser();

            var list = await _db.GarbageReminders
                .Where(r => r.CitizenId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.FullName = user.FullName ?? "Citizen";
            ViewBag.Active = "garbageschedule";
            return View(list);
        }

        // DELETE GARBAGE REMINDER
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

        // NOTICES
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

        // MY PROFILE (GET)
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

        // MY PROFILE (POST)
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

        // ADD REVIEW
        [HttpGet]
        public async Task<IActionResult> AddReview()
        {
            var user = await CurrentUser();
            ViewBag.FullName = user.FullName ?? "Citizen";
            ViewBag.Active = "addreview";
            return View(new AddReviewVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(AddReviewVM vm)
        {
            var user = await CurrentUser();
            ViewBag.FullName = user.FullName ?? "Citizen";
            ViewBag.Active = "addreview";

            if (!ModelState.IsValid)
                return View(vm);

            var alreadyReviewed = await _db.Reviews
                .AnyAsync(r => r.CitizenId == user.Id);

            if (alreadyReviewed)
            {
                ModelState.AddModelError("", "You have already submitted a review.");
                return View(vm);
            }

            var review = new Review
            {
                CitizenId = user.Id,
                CitizenName = string.IsNullOrWhiteSpace(user.FullName) ? "Citizen" : user.FullName,
                Rating = vm.Rating,
                Comment = vm.Comment,
                IsApproved = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();

            TempData["Msg"] = "✅ Review submitted successfully!";
            return RedirectToAction(nameof(Dashboard));
        }

        // DEACTIVATE ACCOUNT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeactivateAccount()
        {
            TempData["Msg"] = "Deactivate feature is disabled for now.";
            return RedirectToAction(nameof(MyProfile));
        }

        // DELETE ACCOUNT
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

        // QUICK RESET
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