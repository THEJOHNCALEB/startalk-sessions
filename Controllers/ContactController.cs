using StarTalkSessions.Data;
using StarTalkSessions.Models;
using StarTalkSessions.Services;
using StarTalkSessions.Utils;
using Microsoft.AspNetCore.Mvc;

namespace StarTalkSessions.Controllers
{
    public class ContactController : Controller
    {
        private readonly InquiryDatabaseAccess _inquiryDb;
        private readonly NotificationService _notifications;

        public ContactController(InquiryDatabaseAccess inquiryDb, NotificationService notifications)
        {
            _inquiryDb = inquiryDb;
            _notifications = notifications;
        }

        [HttpGet]
        public IActionResult Index()
        {
            this.SetStore();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Submit(Inquiry model, IFormFile? attachment, CancellationToken ctl)
        {
            this.SetStore();
            if (string.IsNullOrWhiteSpace(model.Name)
                || !Functions.IsEmailValid(model.Email ?? "")
                || string.IsNullOrWhiteSpace(model.Message))
            {
                TempData["Error"] = "Please complete your name, a valid email, and a message.";
                return RedirectToAction(nameof(Index));
            }

            if (attachment != null && attachment.Length > 0)
                model.AttachmentName = attachment.FileName;

            var inquiry = new Inquiry
            {
                Name = model.Name.Trim(),
                Email = model.Email!.Trim().ToLowerInvariant(),
                Phone = model.Phone?.Trim() ?? "",
                Type = model.Type ?? "General Inquiry",
                Message = model.Message.Trim(),
                AttachmentName = model.AttachmentName ?? "",
                Status = "New",
                CreatedAt = DateTime.Now
            };

            await _inquiryDb.InsertAsync(inquiry, ctl);

            _ = _notifications.SendEmailAsync(inquiry.Email, "Thanks — we received your message", _notifications.InquiryConfirmationHtml(inquiry));
            _ = _notifications.NotifyWhatsAppAsync($"New {inquiry.Type} inquiry from {inquiry.Name} ({inquiry.Email}).");

            TempData["Success"] = "Thank you — your inquiry has been received. Our studio responds within one business day.";
            return RedirectToAction(nameof(Index));
        }
    }
}