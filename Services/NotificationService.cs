using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using StarTalkSessions.Models;
using StarTalkSessions.Utils;
using Microsoft.Extensions.Options;
using Resend;

namespace StarTalkSessions.Services
{
    /// <summary>
    /// Transaction/notification email (Resend) and WhatsApp (Meta Cloud API).
    /// Every method is non-blocking to the caller and silently skips work when
    /// the corresponding API key is missing or unconfigured, so a missing key
    /// can never crash or slow down a request.
    /// </summary>
    public class NotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly ResendClient? _resend;
        private readonly IHttpClientFactory _httpClientFactory;

        private static string ResendApiKey =>
            Environment.GetEnvironmentVariable("RESEND_API_KEY")
            ?? Environment.GetEnvironmentVariable("RESEND_APITOKEN")
            ?? "";

        private static string WhatsappToken => Environment.GetEnvironmentVariable("WHATSAPP_API_TOKEN") ?? "";
        private static string WhatsappNumberId => Environment.GetEnvironmentVariable("WHATSAPP_NUMBER_ID") ?? "";
        private static string WhatsappTo => Environment.GetEnvironmentVariable("WHATSAPP_NUMBER") ?? "";

        public NotificationService(IHttpClientFactory httpClientFactory, ILogger<NotificationService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            if (!string.IsNullOrWhiteSpace(ResendApiKey))
            {
                try
                {
                    var options = new ResendClientOptions
                    {
                        ApiToken = ResendApiKey.Trim(),
                        ThrowExceptions = false
                    };
                    _resend = new ResendClient(new ResendOptionsSnapshot(options), httpClientFactory.CreateClient("resend"));
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Resend client init failed; email delivery disabled."); _resend = null; }
            }
        }

        public bool ResendEnabled => _resend != null;
        public bool WhatsappEnabled =>
            !string.IsNullOrWhiteSpace(WhatsappToken) && !string.IsNullOrWhiteSpace(WhatsappNumberId) && !string.IsNullOrWhiteSpace(WhatsappTo);

        /// <summary>Fire-and-forget default email: prepared but never throws.</summary>
        public Task SendEmailAsync(string to, string subject, string htmlBody)
        {
            _ = FireEmailAsync(to, subject, htmlBody);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Awaited email used for security-sensitive flows (password reset) so the caller
        /// can tell the user whether the message really went out. Never throws.
        /// </summary>
        public async Task<bool> TrySendEmailAsync(string to, string subject, string htmlBody)
        {
            if (_resend == null || string.IsNullOrWhiteSpace(to)) return false;
            try
            {
                var message = BuildMessage(to, subject, htmlBody);
                var result = await _resend.EmailSendAsync(message);
                return result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Resend email not sent to {To} (subject: {Subject}).", to, subject);
                return false;
            }
        }

        private EmailMessage BuildMessage(string to, string subject, string htmlBody) => new()
        {
            From = (EmailAddress)$"{Environment.GetEnvironmentVariable("RESEND_FROM_NAME") ?? Store.Name} <{(Environment.GetEnvironmentVariable("RESEND_FROM_EMAIL") ?? "noreply@solutionconcept.org")}>",
            To = new[] { to },
            Subject = subject,
            HtmlBody = htmlBody
        };

        private async Task FireEmailAsync(string to, string subject, string htmlBody)
        {
            if (_resend == null || string.IsNullOrWhiteSpace(to)) return;
            try
            {
                var message = BuildMessage(to, subject, htmlBody);
                await _resend.EmailSendAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Resend email not sent to {To} (subject: {Subject}).", to, subject);
            }
        }

        /// <summary>A tiny, immutable IOptionsSnapshot shim so ResendClient can be built without DI wiring.</summary>
        private sealed class ResendOptionsSnapshot : IOptionsSnapshot<ResendClientOptions>
        {
            private readonly ResendClientOptions _value;
            public ResendOptionsSnapshot(ResendClientOptions value) { _value = value; }
            public ResendClientOptions Value => _value;
            public ResendClientOptions Get(string name) => _value;
        }

        /// <summary>Fire-and-forget WhatsApp notification to the studio number.</summary>
        public Task NotifyWhatsAppAsync(string message)
        {
            _ = FireWhatsAppAsync(message);
            return Task.CompletedTask;
        }

        private async Task FireWhatsAppAsync(string message)
        {
            if (!WhatsappEnabled || string.IsNullOrWhiteSpace(message)) return;
            try
            {
                using var client = _httpClientFactory.CreateClient("whatsapp");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", WhatsappToken.Trim());
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var body = new
                {
                    messaging_product = "whatsapp",
                    to = WhatsappTo.Trim(),
                    type = "text",
                    text = new { body = message }
                };
                var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
                var url = $"https://graph.facebook.com/v21.0/{WhatsappNumberId.Trim()}/messages";
                var resp = await client.PostAsync(url, content);
                resp.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WhatsApp notification not sent.");
            }
        }

        // ---------- Helpers to build the HTML bodies ----------
        public string OrderConfirmationHtml(Order order)
        {
            var sb = new StringBuilder();
            sb.Append("<div style=\"font-family:Arial,Helvetica,sans-serif;max-width:600px;margin:0 auto;color:#16362C;\">");
            sb.Append("<h2 style=\"color:#2A1B3D;\">Your order is confirmed</h2>");
            sb.Append($"<p>Hi <strong>{System.Net.WebUtility.HtmlEncode(order.CustomerName)}</strong>, thank you for your order.</p>");
            sb.Append("<table style=\"width:100%;border-collapse:collapse;font-size:14px;\"><thead><tr style=\"text-align:left;border-bottom:2px solid #DCEAE0;\"><th>Item</th><th>Qty</th><th>Amount</th></tr></thead><tbody>");
            foreach (var i in order.Items)
            {
                sb.Append($"<tr style=\"border-bottom:1px solid #DCEAE0;\"><td>{System.Net.WebUtility.HtmlEncode(i.ProductName)}{(string.IsNullOrWhiteSpace(i.Variants) ? "" : $" <span style=\"color:#5e6b66;\">({System.Net.WebUtility.HtmlEncode(i.Variants)})</span>")}</td><td>{i.Quantity}</td><td>{order.Currency} {i.UnitPrice * i.Quantity:N2}</td></tr>");
            }
            sb.Append("</tbody></table>");
            sb.Append($"<p><strong>Order:</strong> {order.OrderNumber}<br/><strong>Tracking:</strong> {order.TrackingRef}<br/><strong>Status:</strong> {order.Status}</p>");
            sb.Append($"<p>Estimated delivery: {order.EstimatedDelivery?.ToString("MMM d, yyyy") ?? "TBC"}.</p>");
            sb.Append("<p style=\"margin-top:32px;padding-top:16px;border-top:1px solid #DCEAE0;color:#5e6b66;font-size:12px;\">Star Talk Sessions studio — kiln-dried hardmedia kits, precision joinery, lifetime endurance.</p>");
            sb.Append("</div>");
            return sb.ToString();
        }

        public string QuoteConfirmationHtml(CustomQuote quote)
        {
            return $@"<div style=""font-family:Arial,Helvetica,sans-serif;max-width:600px;margin:0 auto;color:#16362C;"">
<h2 style=""color:#2A1B3D;"">We received your custom media brief</h2>
<p>Hi <strong>{System.Net.WebUtility.HtmlEncode(quote.Name)}</strong>, thank you for trusting us with your project.</p>
<p><strong>Reference:</strong> {quote.RefNumber}<br/><strong>Category:</strong> {System.Net.WebUtility.HtmlEncode(quote.Category ?? "-")}<br/><strong>Style:</strong> {System.Net.WebUtility.HtmlEncode(quote.Style ?? "-")}</p>
<p>Our studio reviews every brief personally and responds within one business day with a concept and quote.</p>
<p style=""margin-top:32px;padding-top:16px;border-top:1px solid #DCEAE0;color:#5e6b66;font-size:12px;"">Star Talk Sessions studio — bespoke media and interiors crafted to last.</p></div>";
        }

        public string InquiryConfirmationHtml(Inquiry inquiry)
        {
            return $@"<div style=""font-family:Arial,Helvetica,sans-serif;max-width:600px;margin:0 auto;color:#16362C;"">
<h2 style=""color:#2A1B3D;"">Thanks — we got your message</h2>
<p>Hi <strong>{System.Net.WebUtility.HtmlEncode(inquiry.Name)}</strong>, your inquiry (<strong>{System.Net.WebUtility.HtmlEncode(inquiry.Type ?? "General Inquiry")}</strong>) is in.</p>
<p>Our studio responds within one business day. We'll only use your email for this conversation.</p>
<p style=""margin-top:32px;padding-top:16px;border-top:1px solid #DCEAE0;color:#5e6b66;font-size:12px;"">Star Talk Sessions studio — questions, commissions and trade partnerships welcome.</p></div>";
        }

        public string NewsletterWelcomeHtml(string email)
        {
            return $@"<div style=""font-family:Arial,Helvetica,sans-serif;max-width:600px;margin:0 auto;color:#16362C;"">
<h2 style=""color:#2A1B3D;"">Welcome aboard</h2>
<p>You're subscribed at <strong>{System.Net.WebUtility.HtmlEncode(email)}</strong>. Expect monthly design notes, care guides and exclusive drops — never spam.</p>
<p style=""margin-top:32px;padding-top:16px;border-top:1px solid #DCEAE0;color:#5e6b66;font-size:12px;"">Star Talk Sessions studio — designed for living, built to last.</p></div>";
        }

        public string SupportTicketAckHtml(Models.SupportTicket ticket)
        {
            var reply = ticket.Messages.LastOrDefault(m => m.Role == "ai" || m.Role == "admin");
            var preview = reply == null ? "Our team has been notified and will follow up shortly." : reply.Body;
            var previewShort = preview.Length > 320 ? preview[..320] + "…" : preview;
            var threadUrl = $"{Store.SiteUrl}/support?t={ticket.Id}";
            return $@"<div style=""font-family:Arial,Helvetica,sans-serif;max-width:600px;margin:0 auto;color:#16362C;"">
<h2 style=""color:#2A1B3D;"">We're on it</h2>
<p>Hi <strong>{System.Net.WebUtility.HtmlEncode(ticket.Name)}</strong>, thanks for reaching out about <em>{System.Net.WebUtility.HtmlEncode(ticket.Subject)}</em>.</p>
<p>We're already on it. Here's a quick update:</p>
<p style=""background:#F0F6F1;border-left:4px solid #2A1B3D;padding:12px 16px;border-radius:8px;font-size:14px;white-space:pre-wrap;"">{System.Net.WebUtility.HtmlEncode(previewShort)}</p>
<p>Replies appear live on <a href=""{threadUrl}"" style=""color:#2A1B3D;"">your support thread</a>. A specialist will take over personally if a human touch is needed.</p>
<p style=""margin-top:32px;padding-top:16px;border-top:1px solid #DCEAE0;color:#5e6b66;font-size:12px;"">Star Talk Sessions studio — designed for living, built to last.</p></div>";
        }

        public string SupportAdminReplyHtml(string customerName, string reply)
        {
            return $@"<div style=""font-family:Arial,Helvetica,sans-serif;max-width:600px;margin:0 auto;color:#16362C;"">
<h2 style=""color:#2A1B3D;"">A reply from our team</h2>
<p>Hi <strong>{System.Net.WebUtility.HtmlEncode(customerName)}</strong>,</p>
<p style=""background:#F0F6F1;border-left:4px solid #2A1B3D;padding:12px 16px;border-radius:8px;font-size:14px;white-space:pre-wrap;"">{System.Net.WebUtility.HtmlEncode(reply)}</p>
<p>You can continue the conversation on <a href=""{Store.SiteUrl}/support"" style=""color:#2A1B3D;"">your support thread</a>.</p>
<p style=""margin-top:32px;padding-top:16px;border-top:1px solid #DCEAE0;color:#5e6b66;font-size:12px;"">Star Talk Sessions studio — designed for living, built to last.</p></div>";
        }

        public string WelcomeEmailHtml(Models.UserModel user)
        {
            return $@"<div style=""font-family:Arial,Helvetica,sans-serif;max-width:600px;margin:0 auto;color:#16362C;"">
<h2 style=""color:#2A1B3D;"">Welcome to the studio</h2>
<p>Hi <strong>{System.Net.WebUtility.HtmlEncode(user.FullName)}</strong>, your account is ready. Track orders, request custom quotes and save your favourites.</p>
<p style=""margin-top:32px;padding-top:16px;border-top:1px solid #DCEAE0;color:#5e6b66;font-size:12px;"">{Store.SiteUrl.Replace("https://", "")} — designed for living, built to last.</p></div>";
        }

        public string OrderStatusHtml(Order order)
        {
            var eta = order.EstimatedDelivery?.ToString("MMM d, yyyy");
            return $@"<div style=""font-family:Arial,Helvetica,sans-serif;max-width:600px;margin:0 auto;color:#16362C;"">
<h2 style=""color:#2A1B3D;"">Your order has an update</h2>
<p>Hi <strong>{System.Net.WebUtility.HtmlEncode(order.CustomerName)}</strong>, here's where things stand with your order.</p>
<table style=""width:100%;border-collapse:collapse;font-size:14px;margin:16px 0;""><tbody>
<tr style=""border-bottom:1px solid #DCEAE0;""><td style=""padding:8px 0;color:#5e6b66;"">Order</td><td style=""padding:8px 0;font-weight:600;"">{order.OrderNumber}</td></tr>
<tr style=""border-bottom:1px solid #DCEAE0;""><td style=""padding:8px 0;color:#5e6b66;"">Status</td><td style=""padding:8px 0;font-weight:600;"">{System.Net.WebUtility.HtmlEncode(order.Status)}</td></tr>
{(string.IsNullOrWhiteSpace(order.TrackingRef) ? "" : $"<tr style=\"border-bottom:1px solid #DCEAE0;\"><td style=\"padding:8px 0;color:#5e6b66;\">Tracking</td><td style=\"padding:8px 0;font-weight:600;\">{System.Net.WebUtility.HtmlEncode(order.TrackingRef)}</td></tr>")}
{(eta == null ? "" : $"<tr><td style=\"padding:8px 0;color:#5e6b66;\">Estimated delivery</td><td style=\"padding:8px 0;font-weight:600;\">{eta}</td></tr>")}
</tbody></table>
<p>You can see the latest details any time from <a href=""{Store.SiteUrl}/account"" style=""color:#2A1B3D;"">your account</a>.</p>
<p style=""margin-top:32px;padding-top:16px;border-top:1px solid #DCEAE0;color:#5e6b66;font-size:12px;"">Star Talk Sessions studio — kiln-dried hardmedia kits, precision joinery, lifetime endurance.</p></div>";
        }

        public string PasswordResetHtml(string fullName, string resetUrl, int validMinutes)
        {
            return $@"<div style=""font-family:Arial,Helvetica,sans-serif;max-width:600px;margin:0 auto;color:#16362C;"">
<h2 style=""color:#2A1B3D;"">Reset your password</h2>
<p>Hi <strong>{System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(fullName) ? "there" : fullName)}</strong>,</p>
<p>We received a request to reset the password for your {System.Net.WebUtility.HtmlEncode(Store.Name)} account.</p>
<p style=""text-align:center;margin:28px 0;""><a href=""{resetUrl}"" style=""background:#2A1B3D;color:#fff;text-decoration:none;padding:13px 28px;border-radius:9999px;font-weight:600;display:inline-block;"">Choose a new password</a></p>
<p style=""color:#5e6b66;font-size:13px;"">This link works once and expires in {validMinutes} minutes. If the button does not work, paste this into your browser:</p>
<p style=""word-break:break-all;color:#2A1B3D;font-size:13px;"">{resetUrl}</p>
<p style=""color:#5e6b66;font-size:13px;"">If you did not ask for this, you can safely ignore this email — your password will not change.</p>
<p style=""margin-top:32px;padding-top:16px;border-top:1px solid #DCEAE0;color:#5e6b66;font-size:12px;"">Need a hand? Reply to this email or reach us at <a href=""mailto:{Store.SupportEmail}"" style=""color:#2A1B3D;"">{Store.SupportEmail}</a>.</p></div>";
        }

        public string PasswordChangedHtml(string fullName)
        {
            return $@"<div style=""font-family:Arial,Helvetica,sans-serif;max-width:600px;margin:0 auto;color:#16362C;"">
<h2 style=""color:#2A1B3D;"">Your password has been changed</h2>
<p>Hi <strong>{System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(fullName) ? "there" : fullName)}</strong>,</p>
<p>This is a confirmation that the password on your {System.Net.WebUtility.HtmlEncode(Store.Name)} account was just changed. You can sign in with your new password now.</p>
<p style=""background:#F0F6F1;border-left:4px solid #2A1B3D;padding:12px 16px;border-radius:8px;font-size:14px;"">If this wasn't you, reset your password immediately and contact us at <a href=""mailto:{Store.SupportEmail}"" style=""color:#2A1B3D;"">{Store.SupportEmail}</a>.</p>
<p style=""margin-top:32px;padding-top:16px;border-top:1px solid #DCEAE0;color:#5e6b66;font-size:12px;""><a href=""{Store.SiteUrl}/account"" style=""color:#2A1B3D;"">Go to your account</a></p></div>";
        }
    }
}