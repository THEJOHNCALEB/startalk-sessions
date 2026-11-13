using StarTalkSessions.Utils;
using Microsoft.AspNetCore.Mvc;

namespace StarTalkSessions.ViewComponents
{
    public class CurrencySelectorViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View(Currency.All);
        }
    }
}