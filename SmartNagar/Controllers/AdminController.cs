using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SmartNagar.Data;
using SmartNagar.Models;
using SmartNagar.ViewModels;

namespace SmartNagar.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;

        public AdminController(ApplicationDbContext db, UserManager<User> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // DASHBOARD
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.Active = "dashboard";
            ViewBag.PageTitle = "Admin Dashboard";

            var vm = new AdminDashboardVM
            {
                TotalUsers = await _db.Users.CountAsync(),
                TotalComplaints = await _db.Complaints.CountAsync(),
                Resolved = await _db.Complaints.CountAsync(c => c.Status == "Resolved"),
                Pending = await _db.Complaints.CountAsync(c => c.Status == "Pending"),
                RecentActivities = await _db.ActivityLogs
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(15)
                    .ToListAsync()
            };

            return View(vm);
        }

        // FULL ADMIN NOTIFICATIONS PAGE
        [HttpGet]
        public async Task<IActionResult> Notifications()
        {
            ViewBag.PageTitle = "Admin Notifications";

            var items = await _db.ActivityLogs
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(items);
        }

        // ADMIN NOTIFICATION COUNT
        [HttpGet]
        public async Task<IActionResult> UnreadActivityCount()
        {
            var count = await _db.ActivityLogs.CountAsync(x => !x.IsRead);
            return Json(new { count });
        }

        // ADMIN NOTIFICATION PREVIEW
        [HttpGet]
        public async Task<IActionResult> RecentActivitiesPreview()
        {
            var items = await _db.ActivityLogs
                .OrderByDescending(x => x.CreatedAt)
                .Take(8)
                .Select(x => new
                {
                    x.Id,
                    x.Type,
                    x.Title,
                    x.Detail,
                    x.IsRead,
                    createdAtText = x.CreatedAt.ToLocalTime().ToString("dd MMM yyyy hh:mm tt")
                })
                .ToListAsync();

            return Json(items);
        }

        // MANAGE USERS
        [HttpGet]
        public async Task<IActionResult> ManageUsers()
        {
            ViewBag.Active = "users";
            ViewBag.PageTitle = "Manage Users";

            var users = await _db.Users
                .OrderByDescending(u => u.Email)
                .Select(u => new ManageUsersVM.UserRow
                {
                    Id = u.Id,
                    Name = u.FullName,
                    Email = u.Email ?? "",
                    Role = u.Role,
                    IsActive = u.IsActive
                })
                .ToListAsync();

            return View(new ManageUsersVM { Users = users });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction(nameof(ManageUsers));

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return RedirectToAction(nameof(ManageUsers));

            user.IsActive = !user.IsActive;
            _db.Users.Update(user);

            _db.ActivityLogs.Add(new ActivityLog
            {
                Type = "User",
                Title = user.IsActive ? "User activated" : "User deactivated",
                Detail = $"{user.FullName} ({user.Email})",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(ManageUsers));
        }

        // DELETE USER
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction(nameof(ManageUsers));

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return RedirectToAction(nameof(ManageUsers));

            if ((user.Email ?? "").ToLower() == "admin@smartnagar.com")
            {
                TempData["Msg"] = "You cannot delete the main Admin account.";
                return RedirectToAction(nameof(ManageUsers));
            }

            user.IsActive = false;
            _db.Users.Update(user);

            _db.ActivityLogs.Add(new ActivityLog
            {
                Type = "User",
                Title = "User disabled (soft delete)",
                Detail = $"{user.FullName} ({user.Email})",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            TempData["Msg"] = "✅ User disabled successfully (complaints kept).";
            return RedirectToAction(nameof(ManageUsers));
        }

        // PUBLISH NOTICE
        [HttpGet]
        public IActionResult PublishNotice()
        {
            ViewBag.Active = "publish";
            ViewBag.PageTitle = "Publish Notice";
            return View(new PublishNoticeVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishNotice(PublishNoticeVM vm)
        {
            ViewBag.Active = "publish";
            ViewBag.PageTitle = "Publish Notice";

            if (!ModelState.IsValid)
                return View(vm);

            var notice = new Notice
            {
                Title = vm.Title.Trim(),
                Description = vm.Description.Trim(),
                Priority = vm.Priority,
                CreatedAt = DateTime.UtcNow
            };

            _db.Notices.Add(notice);

            _db.ActivityLogs.Add(new ActivityLog
            {
                Type = "Notice",
                Title = "New notice published",
                Detail = $"{notice.Title} ({notice.Priority})",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            TempData["Msg"] = "✅ Notice published successfully!";
            return RedirectToAction(nameof(Notices));
        }

        // LIST NOTICES
        [HttpGet]
        public async Task<IActionResult> Notices()
        {
            ViewBag.Active = "notices";
            ViewBag.PageTitle = "Notices";

            var notices = await _db.Notices
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notices);
        }

        // DELETE NOTICE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNotice(int id)
        {
            var notice = await _db.Notices.FirstOrDefaultAsync(n => n.Id == id);
            if (notice == null)
                return RedirectToAction(nameof(Notices));

            _db.Notices.Remove(notice);

            _db.ActivityLogs.Add(new ActivityLog
            {
                Type = "Notice",
                Title = "Notice deleted",
                Detail = notice.Title,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Notices));
        }

        // EDIT NOTICE (GET)
        [HttpGet]
        public async Task<IActionResult> EditNotice(int id)
        {
            ViewBag.Active = "notices";
            ViewBag.PageTitle = "Edit Notice";

            var notice = await _db.Notices.FirstOrDefaultAsync(n => n.Id == id);
            if (notice == null) return RedirectToAction(nameof(Notices));
            return View(notice);
        }

        // EDIT NOTICE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditNotice(Notice model)
        {
            var notice = await _db.Notices.FirstOrDefaultAsync(n => n.Id == model.Id);
            if (notice == null) return RedirectToAction(nameof(Notices));

            notice.Title = (model.Title ?? "").Trim();
            notice.Description = (model.Description ?? "").Trim();
            notice.Priority = (model.Priority ?? "Normal").Trim();

            _db.ActivityLogs.Add(new ActivityLog
            {
                Type = "Notice",
                Title = "Notice updated",
                Detail = $"{notice.Title} ({notice.Priority})",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            TempData["Msg"] = "✅ Notice updated successfully!";
            return RedirectToAction(nameof(Notices));
        }

        // COMPLAINTS LIST
        [HttpGet]
        public async Task<IActionResult> Complaints()
        {
            ViewBag.PageTitle = "Complaints";

            var list = await _db.Complaints
                .Include(c => c.Citizen)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        // UPDATE COMPLAINT STATUS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateComplaintStatus(int id, string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return RedirectToAction(nameof(Complaints));

            status = status.Trim();

            var allowed = new[] { "Pending", "In Progress", "Resolved" };
            if (!allowed.Contains(status))
                return RedirectToAction(nameof(Complaints));

            var complaint = await _db.Complaints.FirstOrDefaultAsync(c => c.Id == id);
            if (complaint == null)
                return RedirectToAction(nameof(Complaints));

            complaint.Status = status;

            if (status == "Resolved")
                complaint.ResolvedAt = DateTime.UtcNow;
            else
                complaint.ResolvedAt = null;

            _db.ActivityLogs.Add(new ActivityLog
            {
                Type = "Complaint",
                Title = "Complaint status updated",
                Detail = $"Complaint #{complaint.Id} -> {complaint.Status}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            if (!string.IsNullOrEmpty(complaint.CitizenId))
            {
                _db.CitizenNotifications.Add(new CitizenNotification
                {
                    CitizenId = complaint.CitizenId,
                    Title = "Complaint Status Updated",
                    Message = $"Your complaint #{complaint.Id} is now '{complaint.Status}'.",
                    Type = "ComplaintUpdate",
                    ComplaintId = complaint.Id,
                    TargetRole = "Citizen",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Complaints));
        }

        // SYSTEM OVERVIEW
        [HttpGet]
        public async Task<IActionResult> SystemOverview(int days = 30)
        {
            ViewBag.Active = "overview";
            ViewBag.PageTitle = "System Overview";

            if (days <= 0) days = 30;

            var from = DateTime.UtcNow.Date.AddDays(-days + 1);

            var totalUsers = await _db.Users.CountAsync();
            var totalComplaints = await _db.Complaints.CountAsync();
            var resolved = await _db.Complaints.CountAsync(c => c.Status == "Resolved");
            var pending = await _db.Complaints.CountAsync(c => c.Status == "Pending");

            double avgResolutionDays = 0;

            var complaintGroups = await _db.Complaints
                .Where(c => c.CreatedAt >= from)
                .GroupBy(c => c.CreatedAt.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync();

            var trendLabels = new List<string>();
            var trendValues = new List<int>();
            for (int i = 0; i < days; i++)
            {
                var d = from.AddDays(i).Date;
                trendLabels.Add(d.ToString("dd MMM"));
                trendValues.Add(complaintGroups.FirstOrDefault(x => x.Day == d)?.Count ?? 0);
            }

            var cats = await _db.Complaints
                .GroupBy(c => c.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            var categoryLabels = cats.Select(x => x.Category ?? "Other").ToList();
            var categoryValues = cats.Select(x => x.Count).ToList();

            var statusLabels = new List<string> { "Pending", "Resolved" };
            var statusValues = new List<int> { pending, resolved };

            var topCats = cats.Take(6)
                .Select(x => new SystemOverviewVM.TopCategoryItem
                {
                    Category = x.Category ?? "Other",
                    Count = x.Count
                })
                .ToList();

            var vm = new SystemOverviewVM
            {
                Days = days,
                TotalUsers = totalUsers,
                TotalComplaints = totalComplaints,
                Resolved = resolved,
                Pending = pending,
                AvgResolutionDays = avgResolutionDays,
                TrendLabels = trendLabels,
                TrendValues = trendValues,
                CategoryLabels = categoryLabels,
                CategoryValues = categoryValues,
                StatusLabels = statusLabels,
                StatusValues = statusValues,
                TopCategories = topCats
            };

            return View(vm);
        }

        // ADMIN NOTIFICATIONS
        [HttpPost]
        public async Task<IActionResult> MarkActivityRead(int id)
        {
            var log = await _db.ActivityLogs.FirstOrDefaultAsync(x => x.Id == id);
            if (log != null)
            {
                log.IsRead = true;
                await _db.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            var unread = await _db.ActivityLogs.Where(x => !x.IsRead).ToListAsync();
            if (unread.Count > 0)
            {
                foreach (var a in unread)
                    a.IsRead = true;

                await _db.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllReadFromPage()
        {
            var unread = await _db.ActivityLogs.Where(x => !x.IsRead).ToListAsync();

            if (unread.Count > 0)
            {
                foreach (var item in unread)
                    item.IsRead = true;

                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Notifications));
        }

        // GARBAGE SCHEDULES
        [HttpGet]
        public async Task<IActionResult> GarbageSchedules(int? editId = null)
        {
            ViewBag.Active = "garbage";
            ViewBag.PageTitle = "Garbage Schedules";

            var vm = new GarbageSchedulePageVM
            {
                Schedules = await _db.GarbageSchedules
                    .OrderBy(x => x.WardNumber)
                    .ToListAsync(),
                Form = new GarbageSchedule()
            };

            if (editId.HasValue)
            {
                var item = await _db.GarbageSchedules.FirstOrDefaultAsync(x => x.Id == editId.Value);
                if (item != null)
                {
                    vm.Form = item;
                }
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveGarbageSchedule(GarbageSchedulePageVM vm)
        {
            ViewBag.Active = "garbage";
            ViewBag.PageTitle = "Garbage Schedules";

            if (!ModelState.IsValid)
            {
                vm.Schedules = await _db.GarbageSchedules
                    .OrderBy(x => x.WardNumber)
                    .ToListAsync();

                return View("GarbageSchedules", vm);
            }

            if (vm.Form.Id == 0)
            {
                var schedule = new GarbageSchedule
                {
                    WardNumber = vm.Form.WardNumber,
                    CollectionDays = (vm.Form.CollectionDays ?? "").Trim(),
                    CollectionTime = (vm.Form.CollectionTime ?? "").Trim(),
                    Notes = string.IsNullOrWhiteSpace(vm.Form.Notes) ? null : vm.Form.Notes.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                _db.GarbageSchedules.Add(schedule);

                _db.ActivityLogs.Add(new ActivityLog
                {
                    Type = "Garbage",
                    Title = "Garbage schedule added",
                    Detail = $"Ward {schedule.WardNumber} | {schedule.CollectionDays} | {schedule.CollectionTime}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();

                await NotifyCitizensGarbage(
                    "Garbage Schedule Added",
                    $"New garbage schedule added for Ward {schedule.WardNumber}. Collection Days: {schedule.CollectionDays}, Time: {schedule.CollectionTime}."
                    + (string.IsNullOrWhiteSpace(schedule.Notes) ? "" : $" Notes: {schedule.Notes}")
                );

                TempData["Msg"] = "✅ Garbage schedule added successfully!";
            }
            else
            {
                var schedule = await _db.GarbageSchedules.FirstOrDefaultAsync(x => x.Id == vm.Form.Id);
                if (schedule == null)
                    return RedirectToAction(nameof(GarbageSchedules));

                schedule.WardNumber = vm.Form.WardNumber;
                schedule.CollectionDays = (vm.Form.CollectionDays ?? "").Trim();
                schedule.CollectionTime = (vm.Form.CollectionTime ?? "").Trim();
                schedule.Notes = string.IsNullOrWhiteSpace(vm.Form.Notes) ? null : vm.Form.Notes.Trim();

                _db.ActivityLogs.Add(new ActivityLog
                {
                    Type = "Garbage",
                    Title = "Garbage schedule updated",
                    Detail = $"Ward {schedule.WardNumber} | {schedule.CollectionDays} | {schedule.CollectionTime}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();

                await NotifyCitizensGarbage(
                    "Garbage Schedule Updated",
                    $"Garbage schedule updated for Ward {schedule.WardNumber}. Collection Days: {schedule.CollectionDays}, Time: {schedule.CollectionTime}."
                    + (string.IsNullOrWhiteSpace(schedule.Notes) ? "" : $" Notes: {schedule.Notes}")
                );

                TempData["Msg"] = "✅ Garbage schedule updated successfully!";
            }

            return RedirectToAction(nameof(GarbageSchedules));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGarbageSchedule(int id)
        {
            var item = await _db.GarbageSchedules.FirstOrDefaultAsync(x => x.Id == id);
            if (item == null)
                return RedirectToAction(nameof(GarbageSchedules));

            var wardNumber = item.WardNumber;
            var collectionDays = item.CollectionDays;
            var collectionTime = item.CollectionTime;

            _db.GarbageSchedules.Remove(item);

            _db.ActivityLogs.Add(new ActivityLog
            {
                Type = "Garbage",
                Title = "Garbage schedule deleted",
                Detail = $"Ward {item.WardNumber} | {item.CollectionDays} | {item.CollectionTime}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            await NotifyCitizensGarbage(
                "Garbage Schedule Removed",
                $"Garbage schedule removed for Ward {wardNumber}. Previous schedule: {collectionDays}, {collectionTime}."
            );

            TempData["Msg"] = "✅ Garbage schedule deleted successfully!";
            return RedirectToAction(nameof(GarbageSchedules));
        }

        [HttpGet]
        public IActionResult CancelGarbageEdit()
        {
            return RedirectToAction(nameof(GarbageSchedules));
        }

        // HELPER: CITIZEN GARBAGE NOTIFICATIONS
        private async Task NotifyCitizensGarbage(string title, string message)
        {
            var citizens = await _db.Users
                .Where(u => u.IsActive && !u.IsDeleted && u.Role == "Citizen")
                .ToListAsync();

            if (citizens.Count == 0)
                return;

            var list = new List<CitizenNotification>();

            foreach (var citizen in citizens)
            {
                list.Add(new CitizenNotification
                {
                    CitizenId = citizen.Id,
                    Title = title,
                    Message = message,
                    Type = "Garbage",
                    ComplaintId = null,
                    TargetRole = "Citizen",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _db.CitizenNotifications.AddRange(list);
            await _db.SaveChangesAsync();
        }

        // PDF EXPORT
        [HttpGet]
        public async Task<IActionResult> GenerateReportPdf()
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var totalUsers = await _db.Users.CountAsync();
            var totalComplaints = await _db.Complaints.CountAsync();
            var resolved = await _db.Complaints.CountAsync(c => c.Status == "Resolved");
            var pending = await _db.Complaints.CountAsync(c => c.Status == "Pending");

            var bytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Smart Nagar System Report").FontSize(20).Bold();
                            col.Item().Text("Municipal Digital Services — Analytics Summary")
                                .FontSize(11).FontColor(Colors.Grey.Darken1);
                            col.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd hh:mm tt}")
                                .FontSize(10).FontColor(Colors.Grey.Darken1);
                        });

                        row.ConstantItem(140).AlignRight().Column(col =>
                        {
                            col.Item().Text("Admin Report").Bold().AlignRight();
                            col.Item().Text("SmartNagar").AlignRight().FontColor(Colors.Blue.Darken2);
                        });
                    });

                    page.Content().PaddingTop(18).Column(col =>
                    {
                        col.Item().Text("Key Metrics").FontSize(14).Bold();
                        col.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Element(c => StatCard(c, "Total Users", totalUsers.ToString(), Colors.Indigo.Medium));
                            row.Spacing(10);
                            row.RelativeItem().Element(c => StatCard(c, "Total Complaints", totalComplaints.ToString(), Colors.Blue.Medium));
                            row.Spacing(10);
                            row.RelativeItem().Element(c => StatCard(c, "Resolved", resolved.ToString(), Colors.Green.Medium));
                            row.Spacing(10);
                            row.RelativeItem().Element(c => StatCard(c, "Pending", pending.ToString(), Colors.Red.Medium));
                        });

                        col.Item().PaddingTop(18).Text("Summary Table").FontSize(14).Bold();
                        col.Item().PaddingTop(8).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCellStyle).Text("Metric").FontColor(Colors.White).Bold();
                                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Value").FontColor(Colors.White).Bold();
                            });

                            Row(table, "Total Users", totalUsers.ToString());
                            Row(table, "Total Complaints", totalComplaints.ToString());
                            Row(table, "Resolved Complaints", resolved.ToString());
                            Row(table, "Pending Complaints", pending.ToString());
                        });

                        col.Item().PaddingTop(18).Text("Notes").FontSize(14).Bold();
                        col.Item().PaddingTop(6).Text("• This report is generated from live database counts.").FontColor(Colors.Grey.Darken2);
                        col.Item().Text("• Resolved/Pending depend on Complaint.Status values (Resolved/Pending).").FontColor(Colors.Grey.Darken2);
                    });

                    page.Footer()
                        .AlignCenter()
                        .DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken1))
                        .Text(txt =>
                        {
                            txt.Span("© Smart Nagar — ");
                            txt.Span("System Generated Report").Bold();
                        });
                });
            }).GeneratePdf();

            return File(bytes, "application/pdf", "SmartNagar_System_Report.pdf");

            static IContainer HeaderCellStyle(IContainer container) =>
                container.Background(Colors.Blue.Darken2).PaddingVertical(8).PaddingHorizontal(10);

            static void Row(TableDescriptor table, string metric, string value)
            {
                table.Cell().Element(BodyCellStyle).Text(metric);
                table.Cell().Element(BodyCellStyle).AlignRight().Text(value).Bold();
            }

            static IContainer BodyCellStyle(IContainer container) =>
                container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(8).PaddingHorizontal(10);

            static void StatCard(IContainer container, string label, string number, string accent)
            {
                container
                    .Border(1).BorderColor(Colors.Grey.Lighten2)
                    .Background(Colors.White)
                    .Padding(12)
                    .CornerRadius(10)
                    .Column(col =>
                    {
                        col.Item().Text(label).FontSize(10).FontColor(Colors.Grey.Darken1).Bold();
                        col.Item().PaddingTop(8).Row(r =>
                        {
                            r.ConstantItem(6).Height(28).Background(accent).CornerRadius(3);
                            r.Spacing(10);
                            r.RelativeItem().Text(number).FontSize(20).Bold();
                        });
                    });
            }
        }
    }
}