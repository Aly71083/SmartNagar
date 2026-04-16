using Microsoft.EntityFrameworkCore;
using SmartNagar.Data;

namespace SmartNagar.Services
{
    public class GarbageReminderBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GarbageReminderBackgroundService> _logger;

        public GarbageReminderBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<GarbageReminderBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("GarbageReminderBackgroundService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var reminderEmailService = scope.ServiceProvider.GetRequiredService<IReminderEmailService>();

                    var nowUtc = DateTime.UtcNow;

                    var dueReminders = await db.GarbageReminders
                        .Include(r => r.Citizen)
                        .Where(r =>
                            !r.IsEmailSent &&
                            r.ReminderDateTimeUtc <= nowUtc &&
                            r.Citizen != null &&
                            !string.IsNullOrWhiteSpace(r.Citizen.Email) &&
                            r.Citizen.IsActive &&
                            !r.Citizen.IsDeleted)
                        .ToListAsync(stoppingToken);

                    foreach (var reminder in dueReminders)
                    {
                        try
                        {
                            await reminderEmailService.SendGarbageReminderEmailAsync(
                                reminder.Citizen!.Email!,
                                reminder.Citizen.FullName,
                                reminder.WardNumber,
                                reminder.CollectionDays,
                                reminder.CollectionTime,
                                reminder.Notes
                            );

                            reminder.IsEmailSent = true;
                            reminder.EmailSentAtUtc = DateTime.UtcNow;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send reminder email for reminder ID {ReminderId}", reminder.Id);
                        }
                    }

                    if (dueReminders.Count > 0)
                    {
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while processing garbage reminder emails.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("GarbageReminderBackgroundService stopped.");
        }
    }
}