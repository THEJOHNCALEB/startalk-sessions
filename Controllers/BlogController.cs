using StarTalkSessions.Data;
using StarTalkSessions.Utils;
using Microsoft.AspNetCore.Mvc;

namespace StarTalkSessions.Controllers
{
    public class BlogController : Controller
    {
        private readonly BlogDatabaseAccess _blogDb;

        public BlogController(BlogDatabaseAccess blogDb)
        {
            _blogDb = blogDb;
        }

        public async Task<IActionResult> Index(string tag, CancellationToken ctl)
        {
            this.SetStore();
            var posts = await _blogDb.QueryManyAsync(b => b.IsActive, ctl);
            if (!string.IsNullOrWhiteSpace(tag) && tag != "all")
                posts = posts.Where(b => b.Tag == tag).ToList();
            ViewBag.Posts = posts.OrderByDescending(p => p.CreatedAt).ToList();
            ViewBag.Tags = (await _blogDb.QueryManyAsync(b => b.IsActive, ctl)).Select(b => b.Tag).Distinct().ToList();
            ViewBag.ActiveTag = tag;
            return View();
        }

        [HttpGet]
        [Route("blog/{slug}")]
        public async Task<IActionResult> Detail(string slug, CancellationToken ctl)
        {
            this.SetStore();
            var post = await _blogDb.QueryOneAsync(b => b.Slug == slug && b.IsActive, ctl);
            if (post == null) return NotFound();
            ViewBag.More = (await _blogDb.QueryManyAsync(b => b.IsActive && b.Id != post.Id, ctl))
                .OrderByDescending(b => b.CreatedAt).Take(3).ToList();
            return View(post);
        }
    }
}