using StarTalkSessions.Utils;
using Microsoft.AspNetCore.Mvc;

namespace StarTalkSessions.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            this.SetStore();
            return View();
        }
    }
}