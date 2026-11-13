using StarTalkSessions.Data;
using StarTalkSessions.Models;
using StarTalkSessions.Utils;
using Microsoft.AspNetCore.Mvc;

namespace StarTalkSessions.Controllers
{
    public class HomeController : Controller
    {
        private readonly ProductDatabaseAccess _productDb;
        private readonly CategoryDatabaseAccess _categoryDb;

        public HomeController(ProductDatabaseAccess productDb, CategoryDatabaseAccess categoryDb)
        {
            _productDb = productDb;
            _categoryDb = categoryDb;
        }

        public async Task<IActionResult> Index(CancellationToken ctl)
        {
            this.SetStore();
            const int categoryPreview = 3;
            const int bestsellerPreview = 8;

            // The catalog can hold more than one document per slug (re-seeded data);
            // keep the first of each so the landing page never repeats a card.
            static List<Product> Rank(IEnumerable<Product> rows) => rows
                .GroupBy(p => string.IsNullOrWhiteSpace(p.Slug) ? p.Id.ToString() : p.Slug, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderByDescending(p => p.Rating)
                .ThenByDescending(p => p.ReviewCount)
                .ToList();

            ViewBag.Categories = (await _categoryDb.QueryManyAsync(c => c.IsActive, ctl))
                .GroupBy(c => string.IsNullOrWhiteSpace(c.Slug) ? c.Id.ToString() : c.Slug, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .Take(categoryPreview)
                .ToList();

            // Flagged bestsellers first, topped up with the highest-rated pieces so the
            // rail stays full even when few products carry the flag.
            var ranked = Rank(await _productDb.QueryManyAsync(p => p.IsActive, ctl));
            var picks = ranked.Where(p => p.IsBestSeller)
                .Concat(ranked.Where(p => !p.IsBestSeller))
                .ToList();

            // The public landing page uses editorial media cards; merchandise is
            // intentionally kept in the dedicated shop until the new catalogue is loaded.
            ViewBag.Bestsellers = new List<Product>();
            ViewBag.BestsellersTotal = 0;

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
