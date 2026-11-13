using StarTalkSessions.Data;
using StarTalkSessions.Models;
using StarTalkSessions.Utils;
using Microsoft.AspNetCore.Mvc;

namespace StarTalkSessions.ViewComponents
{
    public class BannerAdsViewComponent : ViewComponent
    {
        private readonly BannerAdDatabaseAccess _adsDb;

        public BannerAdsViewComponent(BannerAdDatabaseAccess adsDb)
        {
            _adsDb = adsDb;
        }

        public async Task<IViewComponentResult> InvokeAsync(string placement)
        {
            var ads = (await _adsDb.QueryManyAsync(a => a.IsActive && a.Placement == placement, CancellationToken.None))
                .OrderByDescending(a => a.CreatedAt)
                .ToList();

            var vm = new BannerAdsViewModel
            {
                Placement = placement,
                Ads = ads,
                AdSenseSlot = Store.AdSenseSlot(placement),
                AdSenseEnabled = Store.AdSenseEnabled
            };

            return View(vm);
        }
    }

    public class BannerAdsViewModel
    {
        public string Placement { get; set; }
        public List<BannerAd> Ads { get; set; } = new();
        public string AdSenseSlot { get; set; } = "";
        public bool AdSenseEnabled { get; set; }
        public bool HasContent => Ads.Any() || (AdSenseEnabled && !string.IsNullOrWhiteSpace(AdSenseSlot));
    }
}