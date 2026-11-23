using StarTalkSessions.Data;
using StarTalkSessions.Models;
using StarTalkSessions.Services;
using StarTalkSessions.Utils;
using Microsoft.AspNetCore.Mvc;

namespace StarTalkSessions.Controllers
{
    public class CustomController : Controller
    {
        private readonly CustomQuoteDatabaseAccess _quoteDb;
        private readonly NotificationService _notifications;

        public CustomController(CustomQuoteDatabaseAccess quoteDb, NotificationService notifications)
        {
            _quoteDb = quoteDb;
            _notifications = notifications;
        }

        [HttpGet]
        [Route("custom-session")]
        public async Task<IActionResult> Index(CancellationToken ctl)
        {
            this.SetStore();
            ViewBag.Categories = new[]
            {
                "Sofa & Sectional", "Bed & Headboard", "Dining Table & Chairs", "Office Desk", "Wardrobe & Storage",
                "Media Console", "Outdoor Piece", "Entire Room", "Other"
            };
            ViewBag.Styles = new[]
            {
                "Mid-Century Modern", "Japanese Minimalist", "Industrial", "Luxury Contemporary", "Bespoke / Blend"
            };
            ViewBag.Woods = new[] { "Teak", "Black Walnut", "European Oak", "Red Oak", "Ash", "Maple", "Reclaimed Timber" };
            ViewBag.Fabrics = new[] { "Bouclé", "Italian Leather", "Performance Velvet", "Belgian Linen", "Weave / Wool", "Not sure" };
            ViewBag.Hardware = new[] { "Brushed Brass", "Matte Black", "Brushed Nickel", "Antique Bronze", "Stainless Steel", "Open to guidance" };
            ViewBag.CushionDensities = new[] { "Soft (HR foam + feather)", "Medium (standard HR foam)", "Firm (high-resilience)", "Bedroom / Lounge (soft)", "Not sure" };
            ViewBag.Budgets = new[]
            {
                "Under $1,000", "$1,000 – $2,500", "$2,500 – $5,000", "$5,000 – $10,000", "$10,000 – $25,000", "$25,000+"
            };
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Submit(CustomQuote model, CancellationToken ctl)
        {
            this.SetStore();
            if (string.IsNullOrWhiteSpace(model.Name) || !Functions.IsEmailValid(model.Email ?? ""))
            {
                TempData["Error"] = "Please provide a valid name and email address so we can reach you.";
                return RedirectToAction(nameof(Index));
            }

            var claimEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (!string.IsNullOrWhiteSpace(claimEmail))
            {
                var userDb = HttpContext.RequestServices.GetRequiredService<UserDatabaseAccess>();
                var user = await userDb.QueryOneAsync(u => u.Email == claimEmail, ctl);
                if (user != null) model.UserId = user.Id;
            }

            model.Email = model.Email!.Trim().ToLowerInvariant();
            model.RefNumber = Functions.GenerateQuoteRef();
            model.Status = "Submitted";
            model.CreatedAt = DateTime.Now;
            var saved = await _quoteDb.InsertAsync(model, ctl);

            if (!saved)
            {
                TempData["Error"] = "Something went wrong saving your request. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            _ = _notifications.SendEmailAsync(model.Email, $"Custom session brief received — {model.RefNumber}", _notifications.QuoteConfirmationHtml(model));
            _ = _notifications.NotifyWhatsAppAsync($"New custom brief {model.RefNumber} from {model.Name} ({model.Email}) — {model.Category} / {model.Style}.");

            return RedirectToAction("Confirmation", new { reference = model.RefNumber });
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(string reference, CancellationToken ctl)
        {
            this.SetStore();
            var quote = await _quoteDb.QueryOneAsync(x => x.RefNumber == reference, ctl);
            if (quote == null) return NotFound();
            return View(quote);
        }
    }
}