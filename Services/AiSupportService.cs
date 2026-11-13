using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using StarTalkSessions.Utils;

namespace StarTalkSessions.Services
{
    public class AiSupportService
    {
        private readonly ILogger<AiSupportService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private static string GroqKey => Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? "";
        private static string OpenRouterKey => Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? "";
        private static string GeminiKey => Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";

        public AiSupportService(IHttpClientFactory httpClientFactory, ILogger<AiSupportService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public bool RemoteEnabled =>
            !string.IsNullOrWhiteSpace(GroqKey)
            || !string.IsNullOrWhiteSpace(OpenRouterKey)
            || !string.IsNullOrWhiteSpace(GeminiKey);

        public async Task<string> ReplyAsync(Models.SupportTicket ticket, CancellationToken ctl)
        {
            var history = new List<ChatMessage>
            {
                new("system", BuildSystemPrompt(ticket))
            };
            foreach (var m in ticket.Messages)
            {
                var role = m.Role switch
                {
                    "user" => "user",
                    "ai" or "admin" => "assistant",
                    _ => "user"
                };
                history.Add(new ChatMessage(role, m.Body));
            }

            string? reply = null;
            if (!string.IsNullOrWhiteSpace(GroqKey))
                reply = await PostChatCompletionsAsync("https://api.groq.com/openai/v1", GroqKey.Trim(), "llama-3.3-70b-versatile", history, ctl);
            if (reply == null && !string.IsNullOrWhiteSpace(OpenRouterKey))
                reply = await PostChatCompletionsAsync("https://openrouter.ai/api/v1", OpenRouterKey.Trim(), "meta-llama/llama-3.3-70b-instruct:free", history, ctl);
            if (reply == null && !string.IsNullOrWhiteSpace(GeminiKey))
                reply = await PostChatCompletionsAsync("https://generativelanguage.googleapis.com/v1beta/openai", GeminiKey.Trim(), "gemini-2.0-flash", history, ctl);

            return reply ?? LocalFallback(ticket);
        }

        private async Task<string?> PostChatCompletionsAsync(string baseUrl, string apiKey, string model, List<ChatMessage> messages, CancellationToken ctl)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient("ai");
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (baseUrl.Contains("openrouter"))
                    client.DefaultRequestHeaders.TryAddWithoutValidation("HTTP-Referer", StarTalkSessions.Utils.Store.SiteUrl);
                if (baseUrl.Contains("openrouter"))
                    client.DefaultRequestHeaders.TryAddWithoutValidation("X-Title", "Star Talk Support");

                var payload = new
                {
                    model,
                    messages,
                    temperature = 0.6,
                    max_tokens = 320
                };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var url = $"{baseUrl.TrimEnd('/')}/chat/completions";
                using var cts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(ctl);
                cts.CancelAfter(TimeSpan.FromSeconds(14));
                var resp = await client.PostAsync(url, content, cts.Token);
                if (!resp.IsSuccessStatusCode) return null;
                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(cts.Token));
                var text = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
                return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI provider {BaseUrl} failed.", baseUrl);
                return null;
            }
        }

        private string BuildSystemPrompt(Models.SupportTicket ticket)
        {
            var store = Store.Name;
            var tagline = Store.Tagline;
            return $"You are a friendly, concise customer-support assistant for {store}, a media platform ('{tagline}'). " +
                   "Help the customer with questions about products, materials, sizing, delivery, lead times, warranty, returns, custom media and studios. " +
                   "Keep replies under 160 words, warm in tone, never invent specific prices, discounts or delivery dates. " +
                   "For order-status or billing specifics, say a specialist from the team will follow up personally. " +
                   $"Customer: {ticket.Name} <{ticket.Email}>. Subject: {ticket.Subject}.";
        }

        private string LocalFallback(Models.SupportTicket ticket)
        {
            var q = (ticket.LastBody ?? "").ToLowerInvariant();
            var store = Store.Name;
            if (q.Contains("order") || q.Contains("track") || q.Contains("ship") || q.Contains("delivery"))
                return "Thanks! To check the status of an order, your tracked delivery is linked from your account or order confirmation email. If you tell us your order reference, our team will confirm the latest status personally.";
            if (q.Contains("custom") || q.Contains("bespoke") || q.Contains("commission"))
                return "Wonderful — bespoke pieces are our specialty. Share a rough size, material and budget on the Custom Star Talk Sessions page and our studio will come back with a concept and quote.";
            if (q.Contains("warranty") || q.Contains("return") || q.Contains("refund"))
                return "Every piece carries our lifetime guarantee against joinery and community defects. Return requests are handled within 14 days of delivery — our team will guide you through it.";
            if (q.Contains("price") || q.Contains("cost") || q.Contains("much") || q.Contains("discount"))
                return "Our current catalogue pricing is on the product pages. For bulk or trade pricing, drop us a note and a specialist will share the best offer for your project.";
            if (q.Contains("studio") || q.Contains("visit") || q.Contains("location") || q.Contains("abuja") || q.Contains("where are you"))
                return $"Our flagship studio is at {Store.AddressFormatted}, open Mon–Sat 09:00–18:00 WAT. You're welcome to come and sit in everything.";
            if (q.Contains("media kit") || q.Contains("material") || q.Contains("fabric") || q.Contains("oak") || q.Contains("walnut"))
                return "We build in kiln-dried hardmedia kits like walnut and oak, with fabrics from bouclé to velvet. Each product page lists materials and swatches — happy to help you compare.";
            return $"Thanks for reaching out to {store}. A member of our team has been notified and will follow up personally — usually within one business day. In the meantime, is there anything more we can help with?";
        }

        private sealed record ChatMessage(string role, string content);
    }
}