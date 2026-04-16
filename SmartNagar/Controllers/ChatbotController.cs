using Microsoft.AspNetCore.Mvc;
using SmartNagar.Services;

namespace SmartNagar.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly IGeminiService _geminiService;

        public ChatbotController(IGeminiService geminiService)
        {
            _geminiService = geminiService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { reply = "Please enter a message." });
            }

            var reply = await _geminiService.AskAsync(request.Message);
            return Json(new { reply });
        }

        public class ChatRequest
        {
            public string Message { get; set; } = "";
        }
    }
}