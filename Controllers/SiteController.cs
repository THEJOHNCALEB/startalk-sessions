using Microsoft.AspNetCore.Mvc;
using StarTalkSessions.Data;
using StarTalkSessions.Utils;

namespace StarTalkSessions.Controllers;

public class SiteController : Controller
{
    private readonly HomeContentDatabaseAccess _homeDb;

    public SiteController(HomeContentDatabaseAccess homeDb)
    {
        _homeDb = homeDb;
    }

    [Route("media")]
    [Route("programmes")]
    [Route("humanity")]
    [Route("impact")]
    [Route("events")]
    [Route("partner")]
    [Route("guest")]
    [Route("awards")]
    public async Task<IActionResult> Index(CancellationToken ctl)
    {
        this.SetStore();
        var path = (Request.Path.Value ?? "").Trim('/').ToLowerInvariant();
        ViewBag.Page = path switch
        {
            "media" => ("Shows", "Every way to stay in the conversation.", "Podcast, radio, television, video and live shows — built for curious minds."),
            "programmes" => ("Episodes", "Shows with something to say.", "Talk shows, interviews, long-form conversations and public forums that turn attention into momentum."),
            "humanity" => ("Live", "The conversation is happening now.", "Join live broadcasts, studio audiences and community conversations as they unfold."),
            "impact" => ("Our impact", "Measure the reach. Remember the people.", "Our growing network connects the conversation to practical change across communities."),
            "events" => ("Schedule", "Meet the moment in person.", "Live recordings, public forums and studio events that bring the room to life."),
            "partner" => ("Partner with us", "Build the next big conversation together.", "Create a partnership audiences trust — from sponsorship and media partnerships to studio, event and technology support."),
            "guest" => ("Guest application", "Bring your story to the room.", "Tell us what you are building, learning or changing. Our producers review every application."),
            "awards" => ("Celebrity circle", "Honouring the people who make culture.", "Discover the musicians, actors, creators and public voices shaping our shared future."),
            _ => ("Conversations", "Talks worth staying for.", "Long-form talk shows, live broadcasts and episodes from the studio.")
        };

        ViewBag.SiteChannels = (await _homeDb.QueryManyAsync(x => x.Section == "channel" && x.IsActive, ctl))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToList();

        return View();
    }
}
