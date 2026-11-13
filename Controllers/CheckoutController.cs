using StarTalkSessions.Utils;
using Microsoft.AspNetCore.Mvc;

namespace StarTalkSessions.Controllers
{
    public class CheckoutController : Controller
    {
        public IActionResult Index()
        {
            this.SetStore();
            ViewData["Title"] = "Checkout";
            return View();
        }
    }
}
