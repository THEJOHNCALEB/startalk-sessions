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

        public HomeController(CategoryDatabaseAccess categoryDb, HomeContentDatabaseAccess homeDb)
        {
            _categoryDb = categoryDb;
            _homeDb = homeDb;
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
