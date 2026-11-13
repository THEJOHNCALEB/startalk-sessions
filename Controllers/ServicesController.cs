using StarTalkSessions.Data;
using StarTalkSessions.Utils;
using Microsoft.AspNetCore.Mvc;

namespace StarTalkSessions.Controllers
{
    public class ServicesController : Controller
    {
        private readonly ServiceDatabaseAccess _serviceDb;

        public ServicesController(ServiceDatabaseAccess serviceDb)
        {
            _serviceDb = serviceDb;
        }

        public async Task<IActionResult> Index(CancellationToken ctl)
        {
            this.SetStore();
            ViewBag.Services = (await _serviceDb.QueryManyAsync(s => s.IsActive, ctl))
                .OrderBy(s => s.SortOrder).ToList();
            return View();
        }

        [HttpGet]
        [Route("services/{slug}")]
        public async Task<IActionResult> Detail(string slug, CancellationToken ctl)
        {
            this.SetStore();
            var service = await _serviceDb.QueryOneAsync(s => s.Slug == slug && s.IsActive, ctl);
            if (service == null) return NotFound();
            ViewBag.More = (await _serviceDb.QueryManyAsync(s => s.IsActive && s.Id != service.Id, ctl))
                .OrderBy(s => s.SortOrder).Take(3).ToList();
            return View(service);
        }
    }
}