using StarTalkSessions.Data;
using StarTalkSessions.Utils;
using Microsoft.AspNetCore.Mvc;

namespace StarTalkSessions.Controllers
{
    public class ShopController : Controller
    {
        private readonly ProductDatabaseAccess _productDb;
        private readonly CategoryDatabaseAccess _categoryDb;
        private readonly BlogDatabaseAccess _blogDb;

        public ShopController(ProductDatabaseAccess productDb, CategoryDatabaseAccess categoryDb, BlogDatabaseAccess blogDb)
        {
            _productDb = productDb;
            _categoryDb = categoryDb;
            _blogDb = blogDb;
        }

        public async Task<IActionResult> Index(string category, string material, string style,
            string availability, string sort, string q, decimal? min, decimal? max, CancellationToken ctl)
        {
            this.SetStore();
            var categories = await _categoryDb.QueryManyAsync(c => c.IsActive, ctl);
            ViewBag.Categories = categories.OrderBy(c => c.SortOrder).ToList();

            var all = (await _productDb.QueryManyAsync(p => p.IsActive, ctl)).AsEnumerable();

            if (!string.IsNullOrWhiteSpace(category))
            {
                var cat = categories.FirstOrDefault(c => c.Slug == category);
                if (cat != null) all = all.Where(p => p.CategoryId == cat.Id);
                ViewBag.ActiveCategory = cat;
            }

            if (!string.IsNullOrWhiteSpace(material))
            {
                var mat = material.Trim();
                all = all.Where(p => p.Materials.Any(m => m.Name.Contains(mat, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrWhiteSpace(style) && style != "all")
                all = all.Where(p => string.Equals(p.Style, style, StringComparison.OrdinalIgnoreCase));

            if (availability == "in-stock")
                all = all.Where(p => p.Stock > 0);
            else if (availability == "customizable")
                all = all.Where(p => p.IsCustomizable);

            if (min.HasValue)
                all = all.Where(p => p.EffectivePrice >= min.Value);
            if (max.HasValue)
                all = all.Where(p => p.EffectivePrice <= max.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLowerInvariant();
                all = all.Where(p => p.Name.ToLowerInvariant().Contains(term)
                    || p.CategoryName.ToLowerInvariant().Contains(term)
                    || p.Style.ToLowerInvariant().Contains(term)
                    || p.Materials.Any(m => m.Name.ToLowerInvariant().Contains(term)));
            }

            var list = all.ToList();
            list = sort switch
            {
                "price-asc" => list.OrderBy(p => p.EffectivePrice).ToList(),
                "price-desc" => list.OrderByDescending(p => p.EffectivePrice).ToList(),
                "newest" => list.OrderByDescending(p => p.CreatedAt).ToList(),
                "best" => list.OrderByDescending(p => p.Rating).ThenByDescending(p => p.ReviewCount).ToList(),
                _ => list.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAt).ToList()
            };

            ViewBag.ActiveMaterial = material;
            ViewBag.ActiveStyle = style;
            ViewBag.ActiveAvailability = availability;
            ViewBag.ActiveSort = sort;
            ViewBag.ActiveQ = q;
            ViewBag.Min = min;
            ViewBag.Max = max;
            ViewBag.Products = list;
            ViewBag.Total = list.Count;
            ViewBag.AllMaterials = list
                .SelectMany(p => p.Materials.Select(m => m.Name))
                .Distinct().OrderBy(x => x).ToList();

            return View();
        }

        [HttpGet]
        [Route("shop/{slug}")]
        public async Task<IActionResult> Detail(string slug, CancellationToken ctl)
        {
            this.SetStore();
            var product = await _productDb.QueryOneAsync(p => p.Slug == slug && p.IsActive, ctl);
            if (product == null) return NotFound();

            ViewBag.Related = (await _productDb.QueryManyAsync(p =>
                    p.IsActive && p.Id != product.Id && (p.CategoryId == product.CategoryId || p.Style == product.Style), ctl))
                .OrderByDescending(p => p.IsFeatured).Take(4).ToList();

            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> SearchJson(string q, CancellationToken ctl)
        {
            var term = (q ?? "").Trim();
            var results = new List<object>();
            if (term.Length >= 1)
            {
                var t = term.ToLowerInvariant();
                var products = await _productDb.QueryManyAsync(p => p.IsActive, ctl);
                var matched = products.Where(p =>
                    p.Name.ToLowerInvariant().Contains(t) ||
                    p.CategoryName.ToLowerInvariant().Contains(t) ||
                    p.Style.ToLowerInvariant().Contains(t) ||
                    p.Sku.ToLowerInvariant().Contains(t) ||
                    p.Materials.Any(m => m.Name.ToLowerInvariant().Contains(t)))
                    .Take(6)
                    .Select(p => new
                    {
                        type = "product",
                        name = p.Name,
                        sub = p.CategoryName,
                        url = $"/shop/{p.Slug}",
                        image = p.Images.FirstOrDefault(),
                        price = p.EffectivePrice
                    });

                var posts = await _blogDb.QueryManyAsync(b => b.IsActive, ctl);
                var matchedPosts = posts.Where(b =>
                    b.Title.ToLowerInvariant().Contains(t) || b.Tag.ToLowerInvariant().Contains(t))
                    .Take(3)
                    .Select(b => new
                    {
                        type = "post",
                        name = b.Title,
                        sub = b.Tag,
                        url = $"/blog/{b.Slug}",
                        image = b.CoverImage,
                        price = (decimal?)null
                    });

                results.AddRange(matched);
                results.AddRange(matchedPosts);
            }

            return Json(new { query = term, results = results });
        }
    }
}