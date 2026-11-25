using StarTalkSessions.Data;
using StarTalkSessions.Models;
using StarTalkSessions.Utils;
using Microsoft.AspNetCore.Mvc;

namespace StarTalkSessions.Controllers
{
    public class HomeController : Controller
    {
        private readonly CategoryDatabaseAccess _categoryDb;
        private readonly HomeContentDatabaseAccess _homeDb;
        private readonly LiveStreamDatabaseAccess _liveDb;
        private readonly BlogDatabaseAccess _blogDb;
        private readonly SiteSettingDatabaseAccess _settingsDb;

        public HomeController(CategoryDatabaseAccess categoryDb, HomeContentDatabaseAccess homeDb,
            LiveStreamDatabaseAccess liveDb, BlogDatabaseAccess blogDb, SiteSettingDatabaseAccess settingsDb)
        {
            _categoryDb = categoryDb;
            _homeDb = homeDb;
            _liveDb = liveDb;
            _blogDb = blogDb;
            _settingsDb = settingsDb;
        }

        public async Task<IActionResult> Index(CancellationToken ctl)
        {
            this.SetStore();
            const int categoryPreview = 8;

            ViewBag.Categories = (await _categoryDb.QueryManyAsync(c => c.IsActive, ctl))
                .GroupBy(c => string.IsNullOrWhiteSpace(c.Slug) ? c.Id.ToString() : c.Slug, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .Take(categoryPreview)
                .ToList();

            ViewBag.HomeContent = (await _homeDb.QueryManyAsync(x => x.IsActive, ctl))
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.CreatedAt)
                .GroupBy(x => x.Section ?? "", StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            ViewBag.LatestPosts = (await _blogDb.QueryManyAsync(x => x.IsActive, ctl))
                .OrderByDescending(x => x.CreatedAt)
                .Take(3)
                .ToList();

            var now = DateTime.Now;
            var streams = (await _liveDb.QueryManyAsync(x => x.IsActive, ctl))
                .OrderBy(x => x.ScheduledAt)
                .ToList();
            var current = streams.FirstOrDefault(x => x.IsLiveAt(now));
            ViewBag.LiveStream = current ?? streams.FirstOrDefault(x => x.IsUpcomingAt(now));
            ViewBag.IsLiveNow = current != null;
            ViewBag.UpcomingStreams = streams.Where(x => x.IsUpcomingAt(now)).Take(4).ToList();

            ViewBag.Settings = await _settingsDb.QueryAllAsync(ctl);

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            this.SetStore();
            return View();
        }
    }
}
