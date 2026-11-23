using StarTalkSessions.Data;
using StarTalkSessions.Models;
using StarTalkSessions.Utils;
using Microsoft.AspNetCore.Mvc;

namespace StarTalkSessions.Controllers
{
    public class HomeController : Controller
    {
        private readonly CategoryDatabaseAccess _categoryDb;

        public HomeController(CategoryDatabaseAccess categoryDb)
        {
            _categoryDb = categoryDb;
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
