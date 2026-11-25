using StarTalkSessions.Data;
using StarTalkSessions.Models;
using StarTalkSessions.Services;
using StarTalkSessions.Utils;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StarTalkSessions.Controllers
{
    [AllowAnonymous]
    public class SupportController : Controller
    {
        private const string TicketCookieName = "sts_support";

        private readonly SupportTicketDatabaseAccess _ticketDb;
        private readonly SiteSettingDatabaseAccess _settingsDb;
        private readonly AiSupportService _ai;
        private readonly NotificationService _notifications;

        public SupportController(SupportTicketDatabaseAccess ticketDb, SiteSettingDatabaseAccess settingsDb,
            AiSupportService ai, NotificationService notifications)
        {
            _ticketDb = ticketDb;
            _settingsDb = settingsDb;
            _ai = ai;
            _notifications = notifications;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? t, CancellationToken ctl)
        {
            this.SetStore();
            ViewBag.AiRemoteEnabled = _ai.RemoteEnabled;

            var settings = await _settingsDb.QueryAllAsync(ctl);
            ViewBag.SupportAccountNumber = SettingOrFallback(settings, "supportAccountNumber", Store.SupportAccountNumber);
            ViewBag.SupportAccountName = SettingOrFallback(settings, "supportAccountName", Store.SupportAccountName);
            ViewBag.SupportBankName = SettingOrFallback(settings, "supportBankName", Store.SupportBankName);

            var ticket = await ResolveTicketAsync(t, ctl);
            if (ticket != null)
            {
                ViewBag.Ticket = ticket;
                SetTicketCookie(ticket.Id);
            }
            else
            {
                ViewBag.Ticket = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    ViewBag.PrefillName = User.FindFirst("Name")?.Value ?? "";
                    ViewBag.PrefillEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity.Name ?? "";
                    ViewBag.CustomerId = User.FindFirst("UserId")?.Value ?? "";
                }
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Start(string name, string email, string subject, string message, CancellationToken ctl)
        {
            this.SetStore();
            if (string.IsNullOrWhiteSpace(name) || !Functions.IsEmailValid(email ?? "") || string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Please tell us your name, a valid email and your message.";
                return RedirectToAction(nameof(Index));
            }

            var ticket = new SupportTicket
            {
                Name = name.Trim(),
                Email = email!.Trim().ToLowerInvariant(),
                Subject = string.IsNullOrWhiteSpace(subject) ? "General support request" : subject.Trim(),
                Status = "Open",
                AiAssigned = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CustomerId = User.Identity?.IsAuthenticated == true && Guid.TryParse(User.FindFirst("UserId")?.Value, out var uid) ? uid : null
            };
            ticket.Messages.Add(new SupportMessage
            {
                Role = "user",
                Author = ticket.Name,
                Body = message.Trim(),
                CreatedAt = DateTime.Now
            });

            var aiReply = await _ai.ReplyAsync(ticket, ctl);
            ticket.Messages.Add(new SupportMessage
            {
                Role = "ai",
                Author = "AI Assistant",
                Body = aiReply,
                CreatedAt = DateTime.Now
            });
            ticket.UpdatedAt = DateTime.Now;

            await _ticketDb.InsertAsync(ticket, ctl);
            SetTicketCookie(ticket.Id);

            _ = _notifications.NotifyWhatsAppAsync($"New support ticket #{ShortRef(ticket.Id)} from {ticket.Name} ({ticket.Email}): {ticket.Subject}");
            _ = _notifications.SendEmailAsync(ticket.Email, $"We're on it — your message to {Store.Name}", _notifications.SupportTicketAckHtml(ticket));

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Send(Guid ticketId, string message, CancellationToken ctl)
        {
            var ticket = await ResolveTicketAsync(null, ctl, ticketId);
            if (ticket == null || ticket.Status == "Resolved" || string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = string.IsNullOrWhiteSpace(message) ? "Please write a message first." : "That conversation is no longer active.";
                return RedirectToAction(nameof(Index));
            }

            ticket.Messages.Add(new SupportMessage
            {
                Role = "user",
                Author = ticket.Name,
                Body = message.Trim(),
                CreatedAt = DateTime.Now
            });

            if (ticket.AiAssigned)
            {
                var aiReply = await _ai.ReplyAsync(ticket, ctl);
                ticket.Messages.Add(new SupportMessage
                {
                    Role = "ai",
                    Author = "AI Assistant",
                    Body = aiReply,
                    CreatedAt = DateTime.Now
                });
            }

            ticket.UpdatedAt = DateTime.Now;
            await _ticketDb.UpdateAsync(ticket, ctl);
            _ = _notifications.NotifyWhatsAppAsync($"Support ticket #{ShortRef(ticket.Id)} ({ticket.Name}) has a new message{(ticket.AiAssigned ? "" : " — human reply needed.")}");

            return Redirect($"/support?t={ticket.Id}#chat");
        }

        [HttpGet]
        public async Task<IActionResult> Count(string t, CancellationToken ctl)
        {
            var ticket = await ResolveTicketAsync(t, ctl);
            if (ticket == null) return Json(new { ok = false });
            return Json(new { ok = true, count = ticket.Messages.Count, status = ticket.Status });
        }

        [HttpPost]
        public async Task<IActionResult> Close(Guid ticketId, CancellationToken ctl)
        {
            var ticket = await _ticketDb.QueryOneAsync(x => x.Id == ticketId, ctl);
            if (ticket != null)
            {
                ticket.Status = "Resolved";
                ticket.UpdatedAt = DateTime.Now;
                await _ticketDb.UpdateAsync(ticket, ctl);
                Response.Cookies.Delete(TicketCookieName);
                TempData["Success"] = "This conversation is now closed. Thanks for chatting with us!";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<SupportTicket?> ResolveTicketAsync(string? t, CancellationToken ctl, Guid? explicitId = null)
        {
            Guid requested = explicitId ?? Guid.Empty;
            if (requested == Guid.Empty)
            {
                var cookie = Request.Cookies[TicketCookieName];
                if (Guid.TryParse(t ?? cookie ?? "", out var parsed)) requested = parsed;
            }
            if (requested == Guid.Empty) return null;

            var ticket = await _ticketDb.QueryOneAsync(x => x.Id == requested, ctl);
            if (ticket == null) return null;

            if (ticket.CustomerId.HasValue)
            {
                if (User.Identity?.IsAuthenticated == true
                    && Guid.TryParse(User.FindFirst("UserId")?.Value, out var uid)
                    && uid == ticket.CustomerId.Value)
                    return ticket;
                return null;
            }

            var cookieId = Request.Cookies[TicketCookieName];
            return string.Equals(cookieId, requested.ToString(), StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(t) ? ticket : null;
        }

        private void SetTicketCookie(Guid id)
        {
            Response.Cookies.Append(TicketCookieName, id.ToString(), new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                MaxAge = TimeSpan.FromDays(30),
                Path = "/"
            });
        }

        private static string SettingOrFallback(Dictionary<string, string> settings, string key, string fallback) =>
            settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

        private static string ShortRef(Guid id) => id.ToString("N")[..8].ToUpperInvariant();
    }
}