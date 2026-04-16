namespace SmartNagar.Services
{
    public interface IReminderEmailService
    {
        Task SendGarbageReminderEmailAsync(string toEmail, string fullName, int wardNumber, string collectionDays, string collectionTime, string? notes);
    }
}