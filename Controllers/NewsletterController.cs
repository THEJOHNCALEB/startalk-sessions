using StarTalkSessions.Data;
using StarTalkSessions.Services;
using StarTalkSessions.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StarTalkSessions.Controllers
{
    [AllowAnonymous]
    public class NewsletterController : Controller
    {
        private readonly NewsletterDatabaseAccess _newsletterDb;
        private readonly NotificationService _notifications;

        public NewsletterController(NewsletterDatabaseAccess newsletterDb, NotificationService notifications)
        {
            _newsletterDb = newsletterDb;
            _notifications = notifications;
        }

        [HttpPost]
        public async Task<IActionResult> Subscribe(string email, CancellationToken ctl)
        {
            if (!Functions.IsEmailValid(email))
                return Json(new { ok = false, message = "Please enter a valid email address." });

            var normalized = email!.Trim().ToLowerInvariant();
            var existing = await _newsletterDb.QueryOneAsync(n => n.Email == normalized, ctl);
            if (existing != null)
                return Json(new { ok = true, message = "You're already on the list — watch your inbox." });

            await _newsletterDb.InsertAsync(new Models.NewsletterSubscription
            {
                Email = normalized,
                IsActive = true,
                CreatedAt = DateTime.Now
            }, ctl);

            _ = _notifications.SendEmailAsync(normalized, $"Welcome to the {StarTalkSessions.Utils.Store.Name} Journal", _notifications.NewsletterWelcomeHtml(normalized));

            return Json(new { ok = true, message = "Welcome aboard. Expect design notes, not spam." });
        }
    }
}