namespace SmartNagar.Services
{
    public class ReminderEmailService : IReminderEmailService
    {
        private readonly IEmailService _emailService;

        public ReminderEmailService(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task SendGarbageReminderEmailAsync(
            string toEmail,
            string fullName,
            int wardNumber,
            string collectionDays,
            string collectionTime,
            string? notes)
        {
            var subject = "Smart Nagar Garbage Collection Reminder";

            var body = $@"
<div style='font-family:Segoe UI,Arial,sans-serif;line-height:1.7;color:#1f2937'>
    <h2 style='color:#0f172a'>Garbage Collection Reminder</h2>
    <p>Hello {fullName},</p>
    <p>This is a reminder for your garbage collection schedule.</p>

    <table style='border-collapse:collapse;margin-top:12px'>
        <tr>
            <td style='padding:8px 12px;border:1px solid #e5e7eb;font-weight:700;'>Ward</td>
            <td style='padding:8px 12px;border:1px solid #e5e7eb;'>{wardNumber}</td>
        </tr>
        <tr>
            <td style='padding:8px 12px;border:1px solid #e5e7eb;font-weight:700;'>Collection Days</td>
            <td style='padding:8px 12px;border:1px solid #e5e7eb;'>{collectionDays}</td>
        </tr>
        <tr>
            <td style='padding:8px 12px;border:1px solid #e5e7eb;font-weight:700;'>Collection Time</td>
            <td style='padding:8px 12px;border:1px solid #e5e7eb;'>{collectionTime}</td>
        </tr>
        <tr>
            <td style='padding:8px 12px;border:1px solid #e5e7eb;font-weight:700;'>Notes</td>
            <td style='padding:8px 12px;border:1px solid #e5e7eb;'>{notes ?? "N/A"}</td>
        </tr>
    </table>

    <p style='margin-top:16px;'>Please keep your waste ready on time.</p>
    <p>Thank you,<br/>Smart Nagar</p>
</div>";

            await _emailService.SendEmailAsync(toEmail, subject, body);
        }
    }
}