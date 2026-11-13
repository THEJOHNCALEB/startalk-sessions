using StarTalkSessions.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace StarTalkSessions.Controllers
{
    /// <summary>
    /// Lightweight, failure-tolerant endpoints behind custom banner ads.
    /// </summary>
    public class AdsController : Controller
    {
        private readonly BannerAdDatabaseAccess _adsDb;
        private readonly ILogger<AdsController> _logger;

        public AdsController(BannerAdDatabaseAccess adsDb, ILogger<AdsController> logger)
        {
            _adsDb = adsDb;
            _logger = logger;
        }

        [HttpGet]
        [Route("ads/view/{id:guid}")]
        public async Task<IActionResult> View(Guid id, CancellationToken ctl)
        {
            var ad = await _adsDb.QueryOneAsync(a => a.Id == id, ctl);
            if (ad != null)
            {
                try
                {
                    ad.Views += 1;
                    await _adsDb.UpdateAsync(ad, ctl);
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Ad view tracking failed."); }
            }
            return Ok(new { ok = true });
        }

        [HttpGet]
        [Route("ads/go/{id:guid}")]
        public async Task<IActionResult> Go(Guid id, CancellationToken ctl)
        {
            var ad = await _adsDb.QueryOneAsync(a => a.Id == id, ctl);
            if (ad != null)
            {
                try
                {
                    ad.Clicks += 1;
                    await _adsDb.UpdateAsync(ad, ctl);
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Ad click tracking failed."); }
                if (!string.IsNullOrWhiteSpace(ad.TargetUrl)) return Redirect(ad.TargetUrl);
            }
            return Redirect("/");
        }
    }
}