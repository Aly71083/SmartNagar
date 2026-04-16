namespace SmartNagar.Services
{
    public interface IGeminiService
    {
        Task<string> AskAsync(string message);
    }
}