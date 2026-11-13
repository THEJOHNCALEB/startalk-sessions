using StarTalkSessions.Data;
using StarTalkSessions.Utils;
using Microsoft.AspNetCore.Mvc;

namespace StarTalkSessions.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly ProjectDatabaseAccess _projectDb;

        public ProjectsController(ProjectDatabaseAccess projectDb)
        {
            _projectDb = projectDb;
        }

        public async Task<IActionResult> Index(string segment, CancellationToken ctl)
        {
            this.SetStore();
            ViewBag.Segments = new[] { "Luxury Homes", "Boutique Hotels", "Executive Offices", "Restaurants", "High-End Apartments" };
            var projects = await _projectDb.QueryManyAsync(p => p.IsActive, ctl);
            if (!string.IsNullOrWhiteSpace(segment) && segment != "all")
                projects = projects.Where(p => p.Segment == segment).ToList();
            ViewBag.Projects = projects.OrderByDescending(p => p.Year).ToList();
            ViewBag.ActiveSegment = segment;
            return View();
        }

        [HttpGet]
        [Route("projects/{slug}")]
        public async Task<IActionResult> Detail(string slug, CancellationToken ctl)
        {
            this.SetStore();
            var project = await _projectDb.QueryOneAsync(p => p.Slug == slug && p.IsActive, ctl);
            if (project == null) return NotFound();
            ViewBag.More = (await _projectDb.QueryManyAsync(p => p.IsActive && p.Id != project.Id, ctl))
                .OrderByDescending(p => p.IsFeatured).Take(3).ToList();
            return View(project);
        }
    }
}