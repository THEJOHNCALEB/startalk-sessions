using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarTalkSessions.Data;
using StarTalkSessions.Models;
using StarTalkSessions.Services;
using StarTalkSessions.Utils;

namespace StarTalkSessions.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly OrderDatabaseAccess _orderDb;
        private readonly UserDatabaseAccess _userDb;
        private readonly BlogDatabaseAccess _blogDb;
        private readonly InquiryDatabaseAccess _inquiryDb;
        private readonly CustomQuoteDatabaseAccess _quoteDb;
        private readonly NewsletterDatabaseAccess _newsDb;
        private readonly ProductDatabaseAccess _productDb;
        private readonly BannerAdDatabaseAccess _adsDb;
        private readonly SupportTicketDatabaseAccess _supportDb;
        private readonly AiSupportService _ai;
        private readonly NotificationService _notifications;
        private readonly IWebHostEnvironment _env;

        public AdminController(OrderDatabaseAccess orderDb, UserDatabaseAccess userDb,
            BlogDatabaseAccess blogDb, InquiryDatabaseAccess inquiryDb,
            CustomQuoteDatabaseAccess quoteDb, NewsletterDatabaseAccess newsDb,
            ProductDatabaseAccess productDb, BannerAdDatabaseAccess adsDb,
            SupportTicketDatabaseAccess supportDb, AiSupportService ai,
            NotificationService notifications, IWebHostEnvironment env)
        {
            _orderDb = orderDb;
            _userDb = userDb;
            _blogDb = blogDb;
            _inquiryDb = inquiryDb;
            _quoteDb = quoteDb;
            _newsDb = newsDb;
            _productDb = productDb;
            _adsDb = adsDb;
            _supportDb = supportDb;
            _ai = ai;
            _notifications = notifications;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ctl)
        {
            this.SetStore();
            var orders = (await _orderDb.QueryManyAsync(o => true, ctl)).OrderByDescending(o => o.CreatedAt).ToList();
            var users = await _userDb.QueryManyAsync(u => true, ctl);
            var blogs = await _blogDb.QueryManyAsync(b => true, ctl);
            var inquiries = (await _inquiryDb.QueryManyAsync(i => true, ctl)).OrderByDescending(i => i.CreatedAt).ToList();
            var quotes = (await _quoteDb.QueryManyAsync(q => true, ctl)).OrderByDescending(q => q.CreatedAt).ToList();
            var subs = (await _newsDb.QueryManyAsync(s => s.IsActive, ctl)).ToList();

            ViewBag.Orders = orders;
            ViewBag.Users = users;
            ViewBag.Blogs = blogs;
            ViewBag.Inquiries = inquiries;
            ViewBag.Quotes = quotes;
            ViewBag.Subscribers = subs;

            ViewBag.Revenue = orders.Sum(o => o.Total);
            ViewBag.OrderCount = orders.Count;
            ViewBag.NewOrders = orders.Count(o => o.Status == "Processing");
            ViewBag.PendingInquiries = inquiries.Count(i => i.Status != "Done" && i.Status != "Closed");
            ViewBag.NewUsers30 = users.Count(u => u.CreatedAt >= DateTime.Now.AddDays(-30));
            ViewBag.MonthOrders = orders.Count(o => o.CreatedAt >= DateTime.Now.AddDays(-30));
            ViewBag.MonthRevenue = orders.Where(o => o.CreatedAt >= DateTime.Now.AddDays(-30)).Sum(o => o.Total);
            ViewBag.TopProducts = orders
                .SelectMany(o => o.Items)
                .GroupBy(i => i.ProductName)
                .Select(g => new { Name = g.Key, Qty = g.Sum(x => x.Quantity), Revenue = g.Sum(x => x.UnitPrice * x.Quantity) })
                .OrderByDescending(x => x.Qty)
                .Take(5)
                .ToList();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Blogs(CancellationToken ctl)
        {
            this.SetStore();
            ViewBag.Blogs = (await _blogDb.QueryManyAsync(b => true, ctl)).OrderByDescending(b => b.CreatedAt).ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> BlogCreate(string title, string slug, string tag, string excerpt, string content, string coverImage, string author, string readTime, CancellationToken ctl)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["AdminError"] = "Title is required.";
                return RedirectToAction(nameof(Blogs));
            }
            var post = new StarTalkSessions.Models.BlogPost
            {
                Title = title.Trim(),
                Slug = string.IsNullOrWhiteSpace(slug) ? Functions.ToSlug(title) : Functions.ToSlug(slug),
                Tag = tag?.Trim() ?? "Journal",
                Excerpt = excerpt?.Trim() ?? "",
                Content = content?.Trim() ?? "",
                CoverImage = coverImage?.Trim() ?? "",
                Author = author?.Trim() ?? "Studio",
                ReadTime = readTime?.Trim() ?? "3 min read",
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            await _blogDb.InsertAsync(post, ctl);
            TempData["AdminSuccess"] = "Post published.";
            return RedirectToAction(nameof(Blogs));
        }

        [HttpPost]
        public async Task<IActionResult> BlogToggle(Guid id, CancellationToken ctl)
        {
            var post = await _blogDb.QueryOneAsync(b => b.Id == id, ctl);
            if (post != null)
            {
                post.IsActive = !post.IsActive;
                await _blogDb.UpdateAsync(post, ctl);
            }
            return RedirectToAction(nameof(Blogs));
        }

        [HttpPost]
        public async Task<IActionResult> BlogDelete(Guid id, CancellationToken ctl)
        {
            await _blogDb.DeleteAsync(id, ctl);
            TempData["AdminSuccess"] = "Post deleted.";
            return RedirectToAction(nameof(Blogs));
        }

        [HttpGet]
        public async Task<IActionResult> Inquiries(CancellationToken ctl)
        {
            this.SetStore();
            ViewBag.Inquiries = (await _inquiryDb.QueryManyAsync(i => true, ctl)).OrderByDescending(i => i.CreatedAt).ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> InquiryToggle(Guid id, CancellationToken ctl)
        {
            var item = await _inquiryDb.QueryOneAsync(i => i.Id == id, ctl);
            if (item != null)
            {
                item.Status = item.Status == "Done" ? "New" : "Done";
                await _inquiryDb.UpdateAsync(item, ctl);
            }
            return RedirectToAction(nameof(Inquiries));
        }

        [HttpPost]
        public async Task<IActionResult> InquiryDelete(Guid id, CancellationToken ctl)
        {
            await _inquiryDb.DeleteAsync(id, ctl);
            TempData["AdminSuccess"] = "Inquiry deleted.";
            return RedirectToAction(nameof(Inquiries));
        }

        [HttpGet]
        public async Task<IActionResult> Users(CancellationToken ctl)
        {
            this.SetStore();
            ViewBag.Users = (await _userDb.QueryManyAsync(u => true, ctl)).OrderByDescending(u => u.CreatedAt).ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UserToggleActive(Guid id, CancellationToken ctl)
        {
            var user = await _userDb.QueryOneAsync(u => u.Id == id, ctl);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                await _userDb.UpdateAsync(user, ctl);
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        public async Task<IActionResult> UserToggleAdmin(Guid id, CancellationToken ctl)
        {
            var email = User.Identity?.Name;
            var user = await _userDb.QueryOneAsync(u => u.Id == id, ctl);
            if (user != null && user.Email != email)
            {
                user.IsAdmin = !user.IsAdmin;
                await _userDb.UpdateAsync(user, ctl);
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        public async Task<IActionResult> UserDelete(Guid id, CancellationToken ctl)
        {
            var email = User.Identity?.Name;
            var user = await _userDb.QueryOneAsync(u => u.Id == id, ctl);
            if (user != null && user.Email != email)
            {
                await _userDb.DeleteAsync(id, ctl);
                TempData["AdminSuccess"] = "User deleted.";
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public async Task<IActionResult> Orders(CancellationToken ctl)
        {
            this.SetStore();
            ViewBag.Orders = (await _orderDb.QueryManyAsync(o => true, ctl)).OrderByDescending(o => o.CreatedAt).ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> OrderStatus(Guid id, string status, CancellationToken ctl)
        {
            var order = await _orderDb.QueryOneAsync(o => o.Id == id, ctl);
            if (order != null)
            {
                order.Status = status;
                await _orderDb.UpdateAsync(order, ctl);
                TempData["AdminSuccess"] = "Order updated.";

                if (!string.IsNullOrWhiteSpace(order.CustomerEmail))
                {
                    _ = _notifications.SendEmailAsync(
                        order.CustomerEmail,
                        $"Order {order.OrderNumber} — {status}",
                        _notifications.OrderStatusHtml(order));
                    _ = _notifications.NotifyWhatsAppAsync(
                        $"Order {order.OrderNumber} status changed to {status} ({order.CustomerName}).");
                }
            }
            return RedirectToAction(nameof(Orders));
        }

        [HttpPost]
        public async Task<IActionResult> OrderDelete(Guid id, CancellationToken ctl)
        {
            await _orderDb.DeleteAsync(id, ctl);
            TempData["AdminSuccess"] = "Order deleted.";
            return RedirectToAction(nameof(Orders));
        }

        [HttpGet]
        public async Task<IActionResult> Quotes(CancellationToken ctl)
        {
            this.SetStore();
            ViewBag.Quotes = (await _quoteDb.QueryManyAsync(q => true, ctl)).OrderByDescending(q => q.CreatedAt).ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> QuoteStatus(Guid id, string status, CancellationToken ctl)
        {
            var quote = await _quoteDb.QueryOneAsync(q => q.Id == id, ctl);
            if (quote != null)
            {
                quote.Status = status;
                await _quoteDb.UpdateAsync(quote, ctl);
                TempData["AdminSuccess"] = "Quote updated.";
            }
            return RedirectToAction(nameof(Quotes));
        }

        [HttpPost]
        public async Task<IActionResult> QuoteDelete(Guid id, CancellationToken ctl)
        {
            await _quoteDb.DeleteAsync(id, ctl);
            TempData["AdminSuccess"] = "Quote deleted.";
            return RedirectToAction(nameof(Quotes));
        }

        // ---------- Support chat (AI + human) ----------
        [HttpGet]
        public async Task<IActionResult> Support(CancellationToken ctl)
        {
            this.SetStore();
            var all = (await _supportDb.QueryManyAsync(x => true, ctl)).OrderByDescending(x => x.UpdatedAt).ToList();
            ViewBag.Tickets = all;
            ViewBag.AiRemoteEnabled = _ai.RemoteEnabled;
            ViewBag.OpenCount = all.Count(x => x.Status == "Open" && x.AiAssigned);
            ViewBag.HumanCount = all.Count(x => x.Status == "Open" && !x.AiAssigned);
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SupportDetail(Guid id, CancellationToken ctl)
        {
            this.SetStore();
            var ticket = await _supportDb.QueryOneAsync(x => x.Id == id, ctl);
            if (ticket == null) return RedirectToAction(nameof(Support));
            ViewBag.Ticket = ticket;
            ViewBag.AiRemoteEnabled = _ai.RemoteEnabled;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SupportReply(Guid id, string message, CancellationToken ctl)
        {
            var ticket = await _supportDb.QueryOneAsync(x => x.Id == id, ctl);
            if (ticket != null && !string.IsNullOrWhiteSpace(message))
            {
                var adminName = User.Identity?.Name ?? "Studio";
                ticket.Messages.Add(new SupportMessage
                {
                    Role = "admin",
                    Author = adminName,
                    Body = message.Trim(),
                    CreatedAt = DateTime.Now
                });
                ticket.AiAssigned = false;
                ticket.Status = "Open";
                ticket.UpdatedAt = DateTime.Now;
                await _supportDb.UpdateAsync(ticket, ctl);
                _ = _notifications.SendEmailAsync(ticket.Email, $"A reply from {Store.Name} — {ticket.Subject}", _notifications.SupportAdminReplyHtml(ticket.Name, message.Trim()));
                TempData["AdminSuccess"] = "Reply sent to the customer and the AI has been paused on this thread.";
            }
            return RedirectToAction(nameof(SupportDetail), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> SupportAiToggle(Guid id, CancellationToken ctl)
        {
            var ticket = await _supportDb.QueryOneAsync(x => x.Id == id, ctl);
            if (ticket != null)
            {
                ticket.AiAssigned = !ticket.AiAssigned;
                if (ticket.AiAssigned) ticket.Status = "Open";
                ticket.UpdatedAt = DateTime.Now;
                await _supportDb.UpdateAsync(ticket, ctl);
                TempData["AdminSuccess"] = ticket.AiAssigned ? "AI assistant re-enabled on this thread." : "AI assistant paused — you are now handling it.";
            }
            return RedirectToAction(nameof(SupportDetail), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> SupportResolve(Guid id, CancellationToken ctl)
        {
            var ticket = await _supportDb.QueryOneAsync(x => x.Id == id, ctl);
            if (ticket != null)
            {
                ticket.Status = "Resolved";
                ticket.AiAssigned = false;
                ticket.UpdatedAt = DateTime.Now;
                await _supportDb.UpdateAsync(ticket, ctl);
                TempData["AdminSuccess"] = "Conversation marked as resolved.";
            }
            return RedirectToAction(nameof(Support));
        }

        [HttpPost]
        public async Task<IActionResult> SupportReopen(Guid id, CancellationToken ctl)
        {
            var ticket = await _supportDb.QueryOneAsync(x => x.Id == id, ctl);
            if (ticket != null)
            {
                ticket.Status = "Open";
                ticket.UpdatedAt = DateTime.Now;
                await _supportDb.UpdateAsync(ticket, ctl);
                TempData["AdminSuccess"] = "Conversation reopened.";
            }
            return RedirectToAction(nameof(SupportDetail), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> SupportDelete(Guid id, CancellationToken ctl)
        {
            await _supportDb.DeleteAsync(id, ctl);
            TempData["AdminSuccess"] = "Conversation deleted.";
            return RedirectToAction(nameof(Support));
        }

        // ---------- Media library (local storage) ----------
        [HttpGet]
        public IActionResult Media()
        {
            this.SetStore();
            ViewBag.Media = MediaStore.List(_env);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> MediaUpload(IFormFile file, CancellationToken ctl)
        {
            var url = MediaStore.Save(file, _env);
            if (url == null)
            {
                return Json(new { ok = false, message = "Unsupported file type or file too large (images up to 50MB, videos up to 600MB)." });
            }
            return Json(new { ok = true, url = url });
        }

        [HttpPost]
        public IActionResult MediaDelete(string url)
        {
            var deleted = MediaStore.Delete(url, _env);
            return Json(new { ok = deleted });
        }

        // ---------- Advertisements (custom banner ads) ----------
        [HttpGet]
        public async Task<IActionResult> Advertisements(CancellationToken ctl)
        {
            this.SetStore();
            ViewBag.Ads = (await _adsDb.QueryManyAsync(a => true, ctl)).OrderByDescending(a => a.CreatedAt).ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AdCreate(string title, string targetUrl, string placement, IFormFile image, CancellationToken ctl)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(targetUrl))
            {
                TempData["AdminError"] = "Title and target URL are required.";
                return RedirectToAction(nameof(Advertisements));
            }

            var imageUrl = "";
            if (image != null && image.Length > 0)
            {
                imageUrl = MediaStore.Save(image, _env) ?? "";
                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    TempData["AdminError"] = "Unsupported image type (use JPG, PNG, WEBP, GIF, AVIF or an MP4/WEBM video).";
                    return RedirectToAction(nameof(Advertisements));
                }
            }
            else
            {
                TempData["AdminError"] = "Please upload a banner image or video.";
                return RedirectToAction(nameof(Advertisements));
            }

            var placements = new[] { "header", "sidebar", "inline", "footer", "detail" };
            var normalizedPlacement = placements.Contains(placement?.ToLowerInvariant() ?? "") ? placement.ToLowerInvariant() : "inline";

            await _adsDb.InsertAsync(new BannerAd
            {
                Title = title.Trim(),
                TargetUrl = targetUrl.Trim(),
                ImageUrl = imageUrl,
                Placement = normalizedPlacement,
                IsActive = true,
                CreatedAt = DateTime.Now
            }, ctl);

            TempData["AdminSuccess"] = "Banner ad created.";
            return RedirectToAction(nameof(Advertisements));
        }

        [HttpPost]
        public async Task<IActionResult> AdToggle(Guid id, CancellationToken ctl)
        {
            var ad = await _adsDb.QueryOneAsync(a => a.Id == id, ctl);
            if (ad != null)
            {
                ad.IsActive = !ad.IsActive;
                await _adsDb.UpdateAsync(ad, ctl);
            }
            return RedirectToAction(nameof(Advertisements));
        }

        [HttpPost]
        public async Task<IActionResult> AdDelete(Guid id, CancellationToken ctl)
        {
            var ad = await _adsDb.QueryOneAsync(a => a.Id == id, ctl);
            if (ad != null)
            {
                MediaStore.Delete(ad.ImageUrl, _env);
                await _adsDb.DeleteAsync(id, ctl);
            }
            TempData["AdminSuccess"] = "Banner ad deleted.";
            return RedirectToAction(nameof(Advertisements));
        }
    }
}