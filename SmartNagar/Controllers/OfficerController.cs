using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using SmartNagar.Data;
using SmartNagar.Models;
using SmartNagar.ViewModels;
using System.Text;
using ClosedXML.Excel;
using System.IO;

namespace SmartNagar.Controllers
{
    [Authorize(Roles = "MunicipalOfficer")]
    public class OfficerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;

        public OfficerController(ApplicationDbContext db, UserManager<User> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var me = await _userManager.GetUserAsync(User);
            ViewBag.FullName = me?.FullName ?? "Municipal Officer";

            var total = await _db.Complaints.CountAsync();
            var pending = await _db.Complaints.CountAsync(c => c.Status == "Pending");
            var inProgress = await _db.Complaints.CountAsync(c => c.Status == "In Progress");
            var resolved = await _db.Complaints.CountAsync(c => c.Status == "Resolved");
            var critical = await _db.Complaints.CountAsync(c => c.Status != "Resolved");

            var recent = await _db.Complaints
                .Include(c => c.Citizen)
                .OrderByDescending(c => c.CreatedAt)
                .Take(8)
                .Select(c => new ComplaintListRowVM
                {
                    Id = c.Id,
                    ComplaintNo = $"CN-{c.Id:0000}",
                    Subject = c.Title,
                    CitizenName = c.Citizen != null ? c.Citizen.FullName : "Citizen",
                    Ward = c.Ward ?? "-",
                    Priority = "Normal",
                    Status = c.Status,
                    DateText = c.CreatedAt.ToLocalTime().ToString("MMM dd, yyyy")
                })
                .ToListAsync();

            var catRaw = await _db.Complaints
                .GroupBy(c => c.Category ?? "Other Issues")
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(6)
                .ToListAsync();

            var categories = catRaw
                .Select(x => new KeyValuePair<string, int>(x.Category, x.Count))
                .ToList();

            var vm = new OfficerDashboardVM
            {
                TotalComplaints = total,
                PendingReview = pending,
                InProgress = inProgress,
                Resolved = resolved,
                CriticalIssues = critical,
                RecentComplaints = recent,
                TopCategories = categories
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ActiveUsersMap()
        {
            var me = await _userManager.GetUserAsync(User);
            ViewBag.FullName = me?.FullName ?? "Municipal Officer";

            var users = await _db.Users
                .Where(u => u.IsActive && u.LastLat != null && u.LastLng != null)
                .Select(u => new ActiveUserLocationVM
                {
                    UserId = u.Id,
                    FullName = u.FullName ?? u.UserName ?? "User",
                    Lat = u.LastLat!.Value,
                    Lng = u.LastLng!.Value,
                    LastAt = u.LastLocationAt
                })
                .OrderByDescending(x => x.LastAt)
                .ToListAsync();

            return View(users);
        }

        [HttpGet]
        public IActionResult Search(string? q)
        {
            return RedirectToAction("Complaints", new { q = q });
        }

        [HttpGet]
        public async Task<IActionResult> Complaints(string? q, string? status, string? category, string sort = "new", int page = 1, int pageSize = 10)
        {
            var me = await _userManager.GetUserAsync(User);
            ViewBag.FullName = me?.FullName ?? "Municipal Officer";

            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            var query = _db.Complaints
                .Include(c => c.Citizen)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(c =>
                    c.Title.Contains(term) ||
                    (c.Description != null && c.Description.Contains(term)) ||
                    (c.Citizen != null && c.Citizen.FullName.Contains(term)) ||
                    (c.Ward != null && c.Ward.Contains(term))
                );
            }

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(c => c.Status == status);

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(c => (c.Category ?? "Other Issues") == category);

            query = sort == "old"
                ? query.OrderBy(c => c.CreatedAt)
                : query.OrderByDescending(c => c.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var vm = new OfficerComplaintsVM
            {
                Q = q,
                Status = status,
                Category = category,
                Sort = sort,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Complaints = items,
                Categories = Complaint.Categories.ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignToMe(int id)
        {
            var me = await _userManager.GetUserAsync(User);
            if (me == null) return RedirectToAction("Login", "Account");

            var c = await _db.Complaints.FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return NotFound();

            if (string.IsNullOrWhiteSpace(c.AssignedOfficerId))
            {
                c.AssignedOfficerId = me.Id;
                c.AssignedAt = DateTime.UtcNow;
                c.UpdatedAt = DateTime.UtcNow;

                _db.CitizenNotifications.Add(new CitizenNotification
                {
                    CitizenId = c.CitizenId ?? "",
                    Title = "New Complaint Assigned",
                    Message = $"Complaint \"{c.Title}\" has been assigned to you.",
                    Type = "Assignment",
                    ComplaintId = c.Id,
                    TargetRole = "Officer",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();
            }

            return RedirectToAction("MyAssignments");
        }

        [HttpGet]
        public async Task<IActionResult> MyAssignments(string? q, string? status, string sort = "new", int page = 1, int pageSize = 10)
        {
            var me = await _userManager.GetUserAsync(User);
            ViewBag.FullName = me?.FullName ?? "Municipal Officer";
            if (me == null) return RedirectToAction("Login", "Account");

            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            var query = _db.Complaints
                .Include(c => c.Citizen)
                .Where(c => c.AssignedOfficerId == me.Id);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(c =>
                    c.Title.Contains(term) ||
                    (c.Description != null && c.Description.Contains(term)) ||
                    (c.Citizen != null && c.Citizen.FullName.Contains(term)) ||
                    (c.Ward != null && c.Ward.Contains(term))
                );
            }

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(c => c.Status == status);

            query = sort == "old"
                ? query.OrderBy(c => c.CreatedAt)
                : query.OrderByDescending(c => c.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var vm = new OfficerAssignmentsVM
            {
                Q = q,
                Status = status,
                Sort = sort,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string? officerRemarks)
        {
            var me = await _userManager.GetUserAsync(User);

            var c = await _db.Complaints
                .Include(x => x.Citizen)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (c == null) return NotFound();

            var allowed = new[] { "Pending", "In Progress", "Resolved", "Rejected" };
            if (!allowed.Contains(status)) return BadRequest("Invalid status");

            c.Status = status;
            c.OfficerRemarks = string.IsNullOrWhiteSpace(officerRemarks)
                ? null
                : officerRemarks.Trim();

            c.UpdatedAt = DateTime.UtcNow;

            if (status == "Resolved")
                c.ResolvedAt = DateTime.UtcNow;
            else
                c.ResolvedAt = null;

            await _db.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(c.CitizenId))
            {
                var officerName = me?.FullName ?? "Municipal Officer";

                var remarksText = string.IsNullOrWhiteSpace(c.OfficerRemarks)
                    ? ""
                    : $" Remarks: {c.OfficerRemarks}";

                _db.CitizenNotifications.Add(new CitizenNotification
                {
                    CitizenId = c.CitizenId,
                    Title = "Complaint Status Updated",
                    Message = $"Your complaint \"{c.Title}\" is now: {status} (Updated by {officerName}).{remarksText}",
                    Type = "ComplaintUpdate",
                    ComplaintId = c.Id,
                    TargetRole = "Citizen",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();
            }

            return RedirectToAction("ComplaintDetails", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatusFromAssignments(
            int id,
            string status,
            string? officerRemarks,
            string? q,
            string? filterStatus,
            string sort = "new",
            int page = 1,
            int pageSize = 10)
        {
            var me = await _userManager.GetUserAsync(User);

            var c = await _db.Complaints
                .Include(x => x.Citizen)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (c == null) return NotFound();

            var allowed = new[] { "Pending", "In Progress", "Resolved", "Rejected" };
            if (!allowed.Contains(status)) return BadRequest("Invalid status");

            c.Status = status;
            c.OfficerRemarks = string.IsNullOrWhiteSpace(officerRemarks)
                ? null
                : officerRemarks.Trim();

            c.UpdatedAt = DateTime.UtcNow;

            if (status == "Resolved")
                c.ResolvedAt = DateTime.UtcNow;
            else
                c.ResolvedAt = null;

            await _db.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(c.CitizenId))
            {
                var officerName = me?.FullName ?? "Municipal Officer";

                var remarksText = string.IsNullOrWhiteSpace(c.OfficerRemarks)
                    ? ""
                    : $" Remarks: {c.OfficerRemarks}";

                _db.CitizenNotifications.Add(new CitizenNotification
                {
                    CitizenId = c.CitizenId,
                    Title = "Complaint Status Updated",
                    Message = $"Your complaint \"{c.Title}\" is now: {status} (Updated by {officerName}).{remarksText}",
                    Type = "ComplaintUpdate",
                    ComplaintId = c.Id,
                    TargetRole = "Citizen",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();
            }

            return RedirectToAction("MyAssignments", new
            {
                q = q,
                status = filterStatus,
                sort = sort,
                page = page,
                pageSize = pageSize
            });
        }

        [HttpGet]
        public async Task<IActionResult> ComplaintDetails(int id)
        {
            var me = await _userManager.GetUserAsync(User);
            ViewBag.FullName = me?.FullName ?? "Municipal Officer";

            var c = await _db.Complaints
                .Include(x => x.Citizen)
                .Include(x => x.AssignedOfficer)
                .Include(x => x.Photos)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (c == null) return NotFound();
            return View(c);
        }

        [HttpGet]
        public async Task<IActionResult> Management()
        {
            var me = await _userManager.GetUserAsync(User);
            ViewBag.FullName = me?.FullName ?? "Municipal Officer";

            var officers = await _userManager.GetUsersInRoleAsync("MunicipalOfficer");
            var officerList = officers
                .Select(o => new OfficerOptionVM
                {
                    Id = o.Id,
                    Name = string.IsNullOrWhiteSpace(o.FullName) ? (o.Email ?? "Officer") : o.FullName,
                    Email = o.Email ?? ""
                })
                .OrderBy(x => x.Name)
                .ToList();

            var unassigned = await _db.Complaints
                .Include(c => c.Citizen)
                .Where(c => string.IsNullOrWhiteSpace(c.AssignedOfficerId))
                .OrderByDescending(c => c.CreatedAt)
                .Take(30)
                .ToListAsync();

            var assigned = await _db.Complaints
                .Include(c => c.Citizen)
                .Include(c => c.AssignedOfficer)
                .Where(c => !string.IsNullOrWhiteSpace(c.AssignedOfficerId))
                .OrderByDescending(c => c.AssignedAt ?? c.CreatedAt)
                .Take(30)
                .ToListAsync();

            var vm = new OfficerManagementVM
            {
                Officers = officerList,
                UnassignedComplaints = unassigned,
                AssignedComplaints = assigned
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignToOfficer(int complaintId, string officerId)
        {
            var complaint = await _db.Complaints.FirstOrDefaultAsync(c => c.Id == complaintId);
            if (complaint == null) return NotFound();

            var officer = await _userManager.FindByIdAsync(officerId);
            if (officer == null) return BadRequest("Officer not found");

            complaint.AssignedOfficerId = officer.Id;
            complaint.AssignedAt = DateTime.UtcNow;
            complaint.UpdatedAt = DateTime.UtcNow;

            _db.CitizenNotifications.Add(new CitizenNotification
            {
                CitizenId = complaint.CitizenId ?? "",
                Title = "Complaint Assigned",
                Message = $"Complaint \"{complaint.Title}\" has been assigned to an officer.",
                Type = "Assignment",
                ComplaintId = complaint.Id,
                TargetRole = "Officer",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return RedirectToAction("Management");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unassign(int complaintId)
        {
            var complaint = await _db.Complaints.FirstOrDefaultAsync(c => c.Id == complaintId);
            if (complaint == null) return NotFound();

            complaint.AssignedOfficerId = null;
            complaint.AssignedAt = null;
            complaint.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return RedirectToAction("Management");
        }

        [HttpGet]
        public async Task<IActionResult> Analytics(DateTime? from, DateTime? to)
        {
            var me = await _userManager.GetUserAsync(User);
            ViewBag.FullName = me?.FullName ?? "Municipal Officer";

            var toDate = (to ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);
            var fromDate = (from ?? DateTime.Today.AddDays(-29)).Date;

            if (fromDate > toDate.Date)
                fromDate = toDate.Date.AddDays(-29);

            int daysInRange = Math.Max(1, (toDate.Date - fromDate).Days + 1);

            var currentQ = _db.Complaints
                .Include(c => c.Citizen)
                .Include(c => c.AssignedOfficer)
                .Where(c => c.CreatedAt >= fromDate && c.CreatedAt <= toDate);

            var prevFrom = fromDate.AddDays(-daysInRange);
            var prevTo = fromDate.AddTicks(-1);

            var previousQ = _db.Complaints
                .Where(c => c.CreatedAt >= prevFrom && c.CreatedAt <= prevTo);

            // current period core counts
            var total = await currentQ.CountAsync();
            var pending = await currentQ.CountAsync(c => c.Status == "Pending");
            var inProgress = await currentQ.CountAsync(c => c.Status == "In Progress");
            var resolved = await currentQ.CountAsync(c => c.Status == "Resolved");
            var assigned = await currentQ.CountAsync(c => !string.IsNullOrWhiteSpace(c.AssignedOfficerId));
            var unassigned = await currentQ.CountAsync(c => string.IsNullOrWhiteSpace(c.AssignedOfficerId));

            // previous period core counts
            var totalPrev = await previousQ.CountAsync();
            var resolvedPrev = await previousQ.CountAsync(c => c.Status == "Resolved");

            // resolution rate
            double resolutionRate = total > 0 ? (resolved * 100.0 / total) : 0;
            double resolutionRatePrev = totalPrev > 0 ? (resolvedPrev * 100.0 / totalPrev) : 0;
            double resolutionRateDelta = resolutionRate - resolutionRatePrev;

            // average response/resolution hours
            var resolvedRows = await currentQ
                .Where(c => c.Status == "Resolved" && c.ResolvedAt != null)
                .Select(c => new
                {
                    c.CreatedAt,
                    ResolvedAt = c.ResolvedAt!.Value
                })
                .ToListAsync();

            var prevResolvedRows = await previousQ
                .Where(c => c.Status == "Resolved" && c.ResolvedAt != null)
                .Select(c => new
                {
                    c.CreatedAt,
                    ResolvedAt = c.ResolvedAt!.Value
                })
                .ToListAsync();

            double avgResponseHours = resolvedRows.Any()
                ? resolvedRows
                    .Select(x => (x.ResolvedAt - x.CreatedAt).TotalHours)
                    .Where(h => h >= 0)
                    .DefaultIfEmpty(0)
                    .Average()
                : 0;

            double avgResponseHoursPrev = prevResolvedRows.Any()
                ? prevResolvedRows
                    .Select(x => (x.ResolvedAt - x.CreatedAt).TotalHours)
                    .Where(h => h >= 0)
                    .DefaultIfEmpty(0)
                    .Average()
                : 0;

            double avgResponseDeltaPercent = 0;
            if (avgResponseHoursPrev > 0)
                avgResponseDeltaPercent = ((avgResponseHours - avgResponseHoursPrev) / avgResponseHoursPrev) * 100.0;

            bool avgResponseDeltaIsBad = avgResponseDeltaPercent > 0;

            // total complaints delta
            double totalComplaintsDelta = 0;
            if (totalPrev > 0)
                totalComplaintsDelta = ((total - totalPrev) * 100.0 / totalPrev);
            else if (total > 0)
                totalComplaintsDelta = 100;

            // resolved within 48 hours
            int resolvedWithin48 = resolvedRows.Count(x => (x.ResolvedAt - x.CreatedAt).TotalHours <= 48);
            int resolvedWithin48Percent = resolved > 0
                ? (int)Math.Round(resolvedWithin48 * 100.0 / resolved)
                : 0;

            // active users
            int activeUsers = await _db.Users.CountAsync(u => u.IsActive && !u.IsDeleted);
            int activeUsersBarPercent = Math.Min(100, activeUsers);

            // overall performance score
            var assignmentRate = (assigned + unassigned) > 0
                ? (assigned * 100.0 / (assigned + unassigned))
                : 0;

            var overallPerformance = Math.Min(100,
                (resolutionRate * 0.55) +
                (assignmentRate * 0.20) +
                (resolvedWithin48Percent * 0.25));

            int overallPerformanceBarPercent = (int)Math.Round(overallPerformance);

            // satisfaction score - derived safely from performance
            double satisfactionRaw = Math.Min(5.0, Math.Max(0.0,
                ((resolutionRate / 100.0) * 2.5) +
                ((resolvedWithin48Percent / 100.0) * 1.5) +
                ((assignmentRate / 100.0) * 1.0)
            ));

            // category stats
            var catRaw = await currentQ
                .GroupBy(c => c.Category ?? "Other Issues")
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            var categoryStats = catRaw
                .Select(x => new KeyValuePair<string, int>(x.Key, x.Count))
                .ToList();

            // monthly stats for current selected range grouped by month
            var monthlyRaw = await currentQ
                .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            var monthlyStats = monthlyRaw
                .Select(x => new KeyValuePair<string, int>(
                    new DateTime(x.Year, x.Month, 1).ToString("MMM yyyy"),
                    x.Count))
                .ToList();

            // officer load
            var officerLoadRaw = await currentQ
                .Where(c => !string.IsNullOrWhiteSpace(c.AssignedOfficerId))
                .GroupBy(c => c.AssignedOfficer != null
                    ? (c.AssignedOfficer.FullName ?? c.AssignedOfficer.Email ?? "Officer")
                    : "Officer")
                .Select(g => new
                {
                    Officer = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(8)
                .ToListAsync();

            var officerLoad = officerLoadRaw
                .Select(x => new KeyValuePair<string, int>(x.Officer, x.Count))
                .ToList();

            // daily trend points for chart
            var trendRaw = await currentQ
                .GroupBy(c => c.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var trendDict = trendRaw.ToDictionary(x => x.Date, x => x.Count);
            var trendPoints = new List<TrendPointVM>();

            for (var d = fromDate; d <= toDate.Date; d = d.AddDays(1))
            {
                trendPoints.Add(new TrendPointVM
                {
                    Date = d,
                    Count = trendDict.TryGetValue(d, out var cnt) ? cnt : 0
                });
            }

            // ward stats
            var wardRaw = await currentQ
                .Select(c => new
                {
                    WardKey = string.IsNullOrWhiteSpace(c.Ward) ? "Ward -" : c.Ward,
                    c.Status,
                    c.CreatedAt,
                    c.ResolvedAt
                })
                .ToListAsync();

            var wardStats = wardRaw
                .GroupBy(x => x.WardKey)
                .Select(g =>
                {
                    var wTotal = g.Count();
                    var wResolved = g.Count(x => x.Status == "Resolved");
                    var wPending = g.Count(x => x.Status == "Pending" || x.Status == "In Progress" || x.Status == "Rejected");

                    var avgResHours = g
                        .Where(x => x.Status == "Resolved" && x.ResolvedAt != null)
                        .Select(x => (x.ResolvedAt!.Value - x.CreatedAt).TotalHours)
                        .Where(h => h >= 0)
                        .DefaultIfEmpty(0)
                        .Average();

                    string tag = "Average";
                    double wardRate = wTotal > 0 ? (wResolved * 100.0 / wTotal) : 0;

                    if (wardRate >= 80 && avgResHours > 0 && avgResHours <= 72)
                        tag = "Excellent";
                    else if (wardRate >= 55)
                        tag = "Good";

                    return new WardPerfRowVM
                    {
                        WardName = g.Key,
                        Total = wTotal,
                        Resolved = wResolved,
                        Pending = wPending,
                        AvgResolutionHours = avgResHours,
                        PerformanceTag = tag
                    };
                })
                .OrderByDescending(x => x.Resolved)
                .ThenBy(x => x.WardName)
                .ToList();

            var vm = new OfficerAnalyticsVM
            {
                TotalComplaints = total,
                Pending = pending,
                InProgress = inProgress,
                Resolved = resolved,
                Assigned = assigned,
                Unassigned = unassigned,

                CategoryStats = categoryStats,
                MonthlyStats = monthlyStats,
                OfficerLoad = officerLoad,

                From = fromDate,
                To = toDate.Date,

                TotalComplaintsDeltaText = $"{Math.Abs(totalComplaintsDelta):0.#}%",
                TotalComplaintsDeltaNote = totalComplaintsDelta >= 0
                    ? "Compared with previous period"
                    : "Fewer complaints than previous period",

                AvgResponseTimeText = avgResponseHours <= 0 ? "0h" : $"{avgResponseHours:0.#}h",
                AvgResponseDeltaText = $"{Math.Abs(avgResponseDeltaPercent):0.#}%",
                AvgResponseDeltaIsBad = avgResponseDeltaIsBad,
                AvgResponseDeltaNote = avgResponseDeltaIsBad
                    ? "Response time increased from previous period"
                    : "Response time improved from previous period",

                ResolutionRateText = $"{resolutionRate:0.#}%",
                ResolutionRateDeltaText = $"{Math.Abs(resolutionRateDelta):0.#}%",
                ResolutionRateDeltaNote = resolutionRateDelta >= 0
                    ? "Resolution rate improved"
                    : "Resolution rate slightly dropped",

                SatisfactionScoreText = $"{satisfactionRaw:0.0}/5",
                SatisfactionDeltaText = $"{Math.Abs(resolutionRateDelta):0.#}%",
                SatisfactionDeltaNote = "Estimated from resolution speed and closure efficiency",

                ResolvedWithin48HoursPercent = resolvedWithin48Percent,
                ResolvedWithin48HoursText = $"{resolvedWithin48Percent}%",
                ResolvedWithin48HoursNote = $"{resolvedWithin48} complaints resolved within 48 hours",

                ActiveUsersText = activeUsers.ToString(),
                ActiveUsersNote = "Currently active users in the system",
                ActiveUsersBarPercent = activeUsersBarPercent,

                OverallPerformanceText = $"{overallPerformance:0.#}%",
                OverallPerformanceNote = "Calculated from resolution, assignment, and speed",
                OverallPerformanceBarPercent = overallPerformanceBarPercent,

                TrendPoints = trendPoints,
                WardStats = wardStats
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf()
        {
            var me = await _userManager.GetUserAsync(User);
            ViewBag.FullName = me?.FullName ?? "Municipal Officer";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> DownloadExport(DateTime? from, DateTime? to, string? ward, string? status, string format = "pdf")
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var query = _db.Complaints
                .Include(c => c.Citizen)
                .AsQueryable();

            if (from.HasValue) query = query.Where(c => c.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(c => c.CreatedAt <= to.Value);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(c => c.Status == status);

            if (!string.IsNullOrWhiteSpace(ward))
                query = query.Where(c => c.Ward == ward);

            var data = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();

            if (format == "pdf")
            {
                var doc = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(30);

                        page.Header()
                            .Text("Smart Nagar Complaint Report")
                            .FontSize(18).Bold();

                        page.Content().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.ConstantColumn(70);
                                columns.ConstantColumn(90);
                                columns.ConstantColumn(95);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Title").Bold();
                                header.Cell().Text("Citizen").Bold();
                                header.Cell().Text("Ward").Bold();
                                header.Cell().Text("Status").Bold();
                                header.Cell().Text("Date").Bold();
                            });

                            foreach (var c in data)
                            {
                                table.Cell().Text(c.Title);
                                table.Cell().Text(c.Citizen?.FullName ?? "-");
                                table.Cell().Text(c.Ward ?? "-");
                                table.Cell().Text(c.Status);
                                table.Cell().Text(c.CreatedAt.ToString("yyyy-MM-dd"));
                            }
                        });

                        page.Footer().AlignCenter().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
                    });
                });

                return File(doc.GeneratePdf(), "application/pdf", "ComplaintsReport.pdf");
            }

            if (format == "csv")
            {
                var csv = new StringBuilder();
                csv.AppendLine("Title,Citizen,Ward,Status,Date");

                foreach (var c in data)
                    csv.AppendLine($"{Csv(c.Title)},{Csv(c.Citizen?.FullName)},{Csv(c.Ward)},{Csv(c.Status)},{c.CreatedAt:yyyy-MM-dd}");

                return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "Complaints.csv");
            }

            if (format == "json")
                return Json(data);

            if (format == "excel")
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Complaints");

                ws.Cell(1, 1).Value = "Title";
                ws.Cell(1, 2).Value = "Citizen";
                ws.Cell(1, 3).Value = "Ward";
                ws.Cell(1, 4).Value = "Status";
                ws.Cell(1, 5).Value = "Created Date";

                int row = 2;
                foreach (var c in data)
                {
                    ws.Cell(row, 1).Value = c.Title;
                    ws.Cell(row, 2).Value = c.Citizen?.FullName ?? "-";
                    ws.Cell(row, 3).Value = c.Ward ?? "-";
                    ws.Cell(row, 4).Value = c.Status;
                    ws.Cell(row, 5).Value = c.CreatedAt.ToString("yyyy-MM-dd");
                    row++;
                }

                ws.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);

                return File(
                    stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "ComplaintsReport.xlsx");
            }

            return BadRequest("Invalid format");
        }

        private static string Csv(string? s)
        {
            s ??= "";
            s = s.Replace("\"", "\"\"");
            return $"\"{s}\"";
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var me = await _userManager.GetUserAsync(User);
            if (me == null) return RedirectToAction("Login", "Account");

            ViewBag.FullName = me.FullName ?? "Municipal Officer";

            var vm = new OfficerSettingsVM
            {
                FullName = me.FullName ?? "",
                Email = me.Email
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProfile(OfficerSettingsVM vm)
        {
            var me = await _userManager.GetUserAsync(User);
            if (me == null) return RedirectToAction("Login", "Account");

            ViewBag.FullName = me.FullName ?? "Municipal Officer";

            if (!ModelState.IsValid)
                return View("Settings", vm);

            me.FullName = (vm.FullName ?? "").Trim();
            await _userManager.UpdateAsync(me);

            TempData["ok"] = "Profile updated successfully ✅";
            return RedirectToAction("Settings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(OfficerSettingsVM vm)
        {
            var me = await _userManager.GetUserAsync(User);
            if (me == null) return RedirectToAction("Login", "Account");

            ViewBag.FullName = me.FullName ?? "Municipal Officer";

            if (string.IsNullOrWhiteSpace(vm.CurrentPassword) ||
                string.IsNullOrWhiteSpace(vm.NewPassword) ||
                string.IsNullOrWhiteSpace(vm.ConfirmNewPassword))
            {
                TempData["err"] = "Please fill all password fields.";
                return RedirectToAction("Settings");
            }

            if (vm.NewPassword != vm.ConfirmNewPassword)
            {
                TempData["err"] = "New password and confirmation do not match.";
                return RedirectToAction("Settings");
            }

            var result = await _userManager.ChangePasswordAsync(me, vm.CurrentPassword, vm.NewPassword);

            if (!result.Succeeded)
            {
                TempData["err"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                return RedirectToAction("Settings");
            }

            TempData["ok"] = "Password updated successfully ✅";
            return RedirectToAction("Settings");
        }

        [HttpGet]
        public async Task<IActionResult> Notifications()
        {
            var me = await _userManager.GetUserAsync(User);
            ViewBag.FullName = me?.FullName ?? "Municipal Officer";

            var list = await _db.CitizenNotifications
                .Include(n => n.Citizen)
                .Where(n => n.TargetRole == "Officer")
                .OrderByDescending(n => n.CreatedAt)
                .Take(60)
                .ToListAsync();

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> UnreadNotificationCount()
        {
            var count = await _db.CitizenNotifications
                .CountAsync(n => n.TargetRole == "Officer" && !n.IsRead);

            return Json(new { count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllNotificationsRead()
        {
            var list = await _db.CitizenNotifications
                .Where(n => n.TargetRole == "Officer" && !n.IsRead)
                .ToListAsync();

            foreach (var n in list) n.IsRead = true;

            await _db.SaveChangesAsync();
            return RedirectToAction("Notifications");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            var n = await _db.CitizenNotifications
                .FirstOrDefaultAsync(x => x.Id == id && x.TargetRole == "Officer");

            if (n != null)
            {
                n.IsRead = true;
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Notifications");
        }

        [HttpGet]
        public async Task<IActionResult> SendNotice()
        {
            var me = await _userManager.GetUserAsync(User);
            ViewBag.FullName = me?.FullName ?? "Municipal Officer";

            return View(new SendNoticeVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendNotice(SendNoticeVM vm)
        {
            var me = await _userManager.GetUserAsync(User);
            ViewBag.FullName = me?.FullName ?? "Municipal Officer";

            if (!ModelState.IsValid)
                return View(vm);

            var notice = new Notice
            {
                Title = vm.Title.Trim(),
                Description = vm.Description.Trim(),
                Priority = string.IsNullOrWhiteSpace(vm.Priority) ? "Normal" : vm.Priority.Trim(),
                CreatedByRole = "MunicipalOfficer",
                CreatedByName = me?.FullName ?? "Officer",
                CreatedAt = DateTime.UtcNow
            };

            _db.Notices.Add(notice);
            await _db.SaveChangesAsync();

            TempData["ok"] = "Notice sent successfully ✅";
            return RedirectToAction("SendNotice");
        }

        [HttpGet]
        public async Task<IActionResult> AddCitizen()
        {
            var me = await _userManager.GetUserAsync(User);
            ViewBag.FullName = me?.FullName ?? "Municipal Officer";

            return View(new AddCitizenVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCitizen(AddCitizenVM vm)
        {
            var me = await _userManager.GetUserAsync(User);
            ViewBag.FullName = me?.FullName ?? "Municipal Officer";

            if (!ModelState.IsValid)
                return View(vm);

            var fullName = (vm.FullName ?? "").Trim();
            var username = (vm.Username ?? "").Trim();
            var email = (vm.Email ?? "").Trim();
            var phoneNumber = (vm.PhoneNumber ?? "").Trim();
            var address = string.IsNullOrWhiteSpace(vm.Address) ? null : vm.Address.Trim();

            var existingEmail = await _userManager.FindByEmailAsync(email);
            if (existingEmail != null)
            {
                ModelState.AddModelError("Email", "An account with this email already exists.");
                return View(vm);
            }

            var existingUsername = await _userManager.FindByNameAsync(username);
            if (existingUsername != null)
            {
                ModelState.AddModelError("Username", "This username is already taken. Please choose another one.");
                return View(vm);
            }

            var citizen = new User
            {
                FullName = fullName,
                UserName = username,
                Email = email,
                PhoneNumber = phoneNumber,
                Address = address,
                Role = "Citizen",
                IsActive = true,
                IsDeleted = false,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(citizen, vm.Password);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err.Description);

                return View(vm);
            }

            var roleResult = await _userManager.AddToRoleAsync(citizen, "Citizen");
            if (!roleResult.Succeeded)
            {
                foreach (var err in roleResult.Errors)
                    ModelState.AddModelError("", err.Description);

                await _userManager.DeleteAsync(citizen);
                return View(vm);
            }

            TempData["ok"] = "Citizen account created successfully ✅";
            return RedirectToAction("AddCitizen");
        }
    }
}