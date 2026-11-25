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
        private readonly HomeContentDatabaseAccess _homeDb;
        private readonly LiveStreamDatabaseAccess _liveDb;
        private readonly SiteSettingDatabaseAccess _settingsDb;
        private readonly SupportTicketDatabaseAccess _supportDb;
        private readonly AiSupportService _ai;
        private readonly NotificationService _notifications;
        private readonly IWebHostEnvironment _env;

        public AdminController(OrderDatabaseAccess orderDb, UserDatabaseAccess userDb,
            BlogDatabaseAccess blogDb, InquiryDatabaseAccess inquiryDb,
            CustomQuoteDatabaseAccess quoteDb, NewsletterDatabaseAccess newsDb,
            ProductDatabaseAccess productDb, BannerAdDatabaseAccess adsDb,
            HomeContentDatabaseAccess homeDb, LiveStreamDatabaseAccess liveDb, SiteSettingDatabaseAccess settingsDb,
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
            _homeDb = homeDb;
            _liveDb = liveDb;
            _settingsDb = settingsDb;
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
            var streams = (await _liveDb.QueryManyAsync(x => true, ctl))
                .OrderBy(x => x.ScheduledAt)
                .ToList();

            ViewBag.Orders = orders;
            ViewBag.Users = users;
            ViewBag.Blogs = blogs;
            ViewBag.Inquiries = inquiries;
            ViewBag.Quotes = quotes;
            ViewBag.Subscribers = subs;
            ViewBag.LiveStreams = streams;

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
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
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
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
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

        // ---------- Homepage content ----------
        [HttpGet]
        public async Task<IActionResult> HomeContent(Guid? edit, CancellationToken ctl)
        {
            this.SetStore();
            var all = (await _homeDb.QueryManyAsync(x => true, ctl))
                .OrderBy(x => x.Section)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.Title)
                .ToList();
            ViewBag.Content = all;
            ViewBag.Editing = edit.HasValue ? all.FirstOrDefault(x => x.Id == edit.Value) : null;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> HomeContentSave(Guid? id, string section, string kicker, string title,
            string summary, string duration, string badge, string tone, string schedule, string imageUrl,
            string targetUrl, int sortOrder, IFormFile image, CancellationToken ctl)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["AdminError"] = "Title is required.";
                return RedirectToAction(nameof(HomeContent));
            }

            var sections = new[] { "hero", "spotlight", "recommended", "archive", "channel", "explore", "brand" };
            var normalized = sections.Contains(section?.ToLowerInvariant() ?? "") ? section.ToLowerInvariant() : "recommended";

            var savedImage = (imageUrl ?? "").Trim();
            if (image != null && image.Length > 0)
            {
                savedImage = MediaStore.Save(image, _env) ?? "";
                if (string.IsNullOrWhiteSpace(savedImage))
                {
                    TempData["AdminError"] = "Unsupported image type.";
                    return RedirectToAction(nameof(HomeContent));
                }
            }

            var item = id.HasValue ? await _homeDb.QueryOneAsync(x => x.Id == id.Value, ctl) : null;
            var creating = item == null;
            item ??= new HomeContent();
            item.Section = normalized;
            item.Kicker = (kicker ?? "").Trim();
            item.Title = title.Trim();
            item.Summary = (summary ?? "").Trim();
            item.Duration = (duration ?? "").Trim();
            item.Badge = (badge ?? "").Trim();
            item.Tone = (tone ?? "live").Trim();
            item.Schedule = (schedule ?? "").Trim();
            item.ImageUrl = savedImage;
            item.TargetUrl = (targetUrl ?? "/media").Trim();
            item.SortOrder = sortOrder;

            if (creating) await _homeDb.InsertAsync(item, ctl);
            else await _homeDb.UpdateAsync(item, ctl);

            TempData["AdminSuccess"] = creating ? "Homepage item created." : "Homepage item updated.";
            return RedirectToAction(nameof(HomeContent));
        }

        [HttpPost]
        public async Task<IActionResult> HomeContentToggle(Guid id, CancellationToken ctl)
        {
            var item = await _homeDb.QueryOneAsync(x => x.Id == id, ctl);
            if (item != null)
            {
                item.IsActive = !item.IsActive;
                await _homeDb.UpdateAsync(item, ctl);
            }
            return RedirectToAction(nameof(HomeContent));
        }

        [HttpPost]
        public async Task<IActionResult> HomeContentDelete(Guid id, CancellationToken ctl)
        {
            await _homeDb.DeleteAsync(id, ctl);
            TempData["AdminSuccess"] = "Homepage item deleted.";
            return RedirectToAction(nameof(HomeContent));
        }

        // ---------- Live broadcasts ----------
        [HttpGet]
        public async Task<IActionResult> LiveStreams(Guid? edit, CancellationToken ctl)
        {
            this.SetStore();
            var all = (await _liveDb.QueryManyAsync(x => true, ctl)).OrderBy(x => x.ScheduledAt).ToList();
            ViewBag.LiveStreams = all;
            ViewBag.Editing = edit.HasValue ? all.FirstOrDefault(x => x.Id == edit.Value) : null;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> LiveStreamSave(Guid? id, string title, string description, string platform,
            string youtubeUrl, string tiktokUrl, string scheduledAt, int durationMinutes, bool isActive,
            string? returnUrl, CancellationToken ctl)
        {
            string destination = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : "/Admin/LiveStreams";

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["AdminError"] = "Give the broadcast a title.";
                return Redirect(destination);
            }
            if (!DateTime.TryParse(scheduledAt, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var startsAt) || startsAt == default)
            {
                TempData["AdminError"] = "Choose a valid broadcast date and time.";
                return Redirect(destination);
            }

            var normalizedPlatform = (platform ?? "").Trim().ToLowerInvariant();
            if (!new[] { "youtube", "tiktok", "both" }.Contains(normalizedPlatform)) normalizedPlatform = "youtube";
            var yt = (youtubeUrl ?? "").Trim();
            var tt = (tiktokUrl ?? "").Trim();

            if (!IsPlatformUrl(yt, "youtube") && !IsPlatformUrl(tt, "tiktok"))
            {
                TempData["AdminError"] = "Add a valid YouTube or TikTok watch link.";
                return Redirect(destination);
            }
            if (normalizedPlatform != "tiktok" && !IsPlatformUrl(yt, "youtube"))
            {
                TempData["AdminError"] = "A valid YouTube link is required for this platform.";
                return Redirect(destination);
            }
            if (normalizedPlatform != "youtube" && !IsPlatformUrl(tt, "tiktok"))
            {
                TempData["AdminError"] = "A valid TikTok link is required for this platform.";
                return Redirect(destination);
            }

            var stream = id.HasValue ? await _liveDb.QueryOneAsync(x => x.Id == id.Value, ctl) : null;
            var creating = stream == null;
            stream ??= new LiveStream();
            stream.Title = title.Trim();
            stream.Description = (description ?? "").Trim();
            stream.Platform = normalizedPlatform;
            stream.YouTubeUrl = yt;
            stream.TikTokUrl = tt;
            stream.ScheduledAt = startsAt;
            stream.DurationMinutes = durationMinutes > 0 ? Math.Min(durationMinutes, 1440) : 60;
            stream.IsActive = isActive;

            if (creating) await _liveDb.InsertAsync(stream, ctl);
            else await _liveDb.UpdateAsync(stream, ctl);

            TempData["AdminSuccess"] = creating ? "Broadcast scheduled. It will appear on the landing page." : "Broadcast updated.";
            return Redirect(destination);
        }

        [HttpPost]
        public async Task<IActionResult> LiveStreamToggle(Guid id, CancellationToken ctl)
        {
            var stream = await _liveDb.QueryOneAsync(x => x.Id == id, ctl);
            if (stream != null)
            {
                stream.IsActive = !stream.IsActive;
                await _liveDb.UpdateAsync(stream, ctl);
                TempData["AdminSuccess"] = stream.IsActive ? "Broadcast is visible on the landing page." : "Broadcast hidden.";
            }
            return RedirectToAction(nameof(LiveStreams));
        }

        [HttpPost]
        public async Task<IActionResult> LiveStreamDelete(Guid id, CancellationToken ctl)
        {
            await _liveDb.DeleteAsync(id, ctl);
            TempData["AdminSuccess"] = "Broadcast deleted.";
            return RedirectToAction(nameof(LiveStreams));
        }

        private static bool IsPlatformUrl(string? url, string platform)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) return false;
            return platform == "youtube"
                ? uri.Host.Contains("youtube", StringComparison.OrdinalIgnoreCase) || uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase)
                : uri.Host.Contains("tiktok", StringComparison.OrdinalIgnoreCase);
        }

        // ---------- Landing page text ----------
        private static readonly (string Key, string Label, bool Multiline)[] LandingTextFields =
        {
            ("brand", "Brand line", false),
            ("heroTagline", "Hero tagline", false),
            ("heroHeading", "Hero heading", true),
            ("heroDescription", "Hero description", true),
            ("heroPrimaryLabel", "Primary button label", false),
            ("heroSecondaryLabel", "Secondary button label", false),
            ("heroPartner", "Partner credit", false),
            ("exploreTitle", "Explore section title", false),
            ("exploreDescription", "Explore section description", false),
            ("latestTitle", "Latest section title", false),
            ("latestSubtitle", "Latest section subtitle", false),
            ("latestDescription", "Latest section description", true),
            ("supportTitle", "Support section title", false),
            ("supportDescription", "Support section description", true),
            ("supportCta", "Support button label", false),
            ("supportAccountNumber", "Support account number", false),
            ("supportAccountName", "Support account name", false),
            ("supportBankName", "Support bank", false),
            ("brandsTitle", "Brands section title", false),
            ("brandsDescription", "Brands section description", false),
            ("liveTitle", "Live section title", false)
        };

        [HttpGet]
        public async Task<IActionResult> SiteSettings(CancellationToken ctl)
        {
            this.SetStore();
            var values = await _settingsDb.QueryAllAsync(ctl);
            ViewBag.TextFields = LandingTextFields
                .Select(x => new { x.Key, x.Label, x.Multiline, Value = values.TryGetValue(x.Key, out var v) ? v : "" })
                .ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SiteSettingsSave(CancellationToken ctl)
        {
            foreach (var field in LandingTextFields)
            {
                await _settingsDb.SetAsync(field.Key, Request.Form[field.Key].ToString(), ctl);
            }
            TempData["AdminSuccess"] = "Landing page text updated.";
            return RedirectToAction(nameof(SiteSettings));
        }
    }
}
