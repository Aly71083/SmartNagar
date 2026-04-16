using Google.GenAI;
using Microsoft.Extensions.Options;

namespace SmartNagar.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly GeminiSettings _settings;

        public GeminiService(IOptions<GeminiSettings> options)
        {
            _settings = options.Value;
        }

        public async Task<string> AskAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "Please enter a message.";

            try
            {
                var client = new Client(apiKey: _settings.ApiKey);

                var prompt = $@"
You are Smart Nagar Assistant for a Nepal municipal web app.
Answer only about Smart Nagar features and common citizen help.

Main features:
- Submit complaint
- Track complaint status
- Garbage schedule and reminders
- Notices
- Emergency alert
- Profile help
- Login/Register/Forgot password help

Rules:
- Keep answers short, clear, and friendly.
- If asked something unrelated to Smart Nagar, say:
  'I can help with Smart Nagar services like complaints, garbage reminders, notices, emergency help, and account support.'
- Do not invent ward schedules or complaint statuses.
- For emergency, tell users to use the emergency feature in the system.
- Answer in simple English.

User question:
{message}";

                var response = await client.Models.GenerateContentAsync(
                    model: _settings.Model,
                    contents: prompt);

                var text = response?.Text;

                if (string.IsNullOrWhiteSpace(text))
                    return "Sorry, I could not generate a response right now.";

                return text.Trim();
            }
            catch
            {
                return "Sorry, the chatbot is unavailable right now. Please try again.";
            }
        }
    }
}