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

        // =========================
        // ✅ DASHBOARD
        // =========================
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
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

        // =========================
        // ✅ MANAGE USERS
        // =========================
        [HttpGet]
        public async Task<IActionResult> ManageUsers()
        {
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

        // =========================
        // ✅ DELETE USER (SOFT DELETE FIX)  ✅ NO FK ERROR
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction(nameof(ManageUsers));

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return RedirectToAction(nameof(ManageUsers));

            // prevent deleting main seeded admin
            if ((user.Email ?? "").ToLower() == "admin@smartnagar.com")
            {
                TempData["Msg"] = "You cannot delete the main Admin account.";
                return RedirectToAction(nameof(ManageUsers));
            }

            // ✅ Soft delete: just disable
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

        // =========================
        // ✅ PUBLISH NOTICE
        // =========================
        [HttpGet]
        public IActionResult PublishNotice()
        {
            return View(new PublishNoticeVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishNotice(PublishNoticeVM vm)
        {
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

        // =========================
        // ✅ LIST NOTICES
        // =========================
        [HttpGet]
        public async Task<IActionResult> Notices()
        {
            var notices = await _db.Notices
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notices);
        }

        // =========================
        // ✅ DELETE NOTICE
        // =========================
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

        // =========================
        // ✅ EDIT NOTICE (GET)
        // =========================
        [HttpGet]
        public async Task<IActionResult> EditNotice(int id)
        {
            var notice = await _db.Notices.FirstOrDefaultAsync(n => n.Id == id);
            if (notice == null) return RedirectToAction(nameof(Notices));
            return View(notice);
        }

        // =========================
        // ✅ EDIT NOTICE (POST)
        // =========================
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

        // =========================
        // ✅ COMPLAINTS LIST
        // =========================
        [HttpGet]
        public async Task<IActionResult> Complaints()
        {
            var list = await _db.Complaints
                .Include(c => c.Citizen)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        // =========================
        // ✅ UPDATE COMPLAINT STATUS + NOTIFICATION
        // =========================
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
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Complaints));
        }

        // =========================
        // ✅ SYSTEM OVERVIEW
        // =========================
        [HttpGet]
        public async Task<IActionResult> SystemOverview(int days = 30)
        {
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
                .Select(x => new SystemOverviewVM.TopCategoryItem { Category = x.Category ?? "Other", Count = x.Count })
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

        // =========================
        // ✅ ADMIN NOTIFICATIONS (ACTIVITY LOGS)
        // =========================
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
                foreach (var a in unread) a.IsRead = true;
                await _db.SaveChangesAsync();
            }
            return Ok();
        }

        // =========================
        // ✅ PDF EXPORT (QuestPDF)
        // =========================
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