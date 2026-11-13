using Microsoft.AspNetCore.Mvc;
using StarTalkSessions.Utils;

namespace StarTalkSessions.Controllers;

public class SiteController : Controller
{
    [Route("media")]
    [Route("programmes")]
    [Route("humanity")]
    [Route("impact")]
    [Route("events")]
    [Route("partner")]
    [Route("guest")]
    [Route("awards")]
    public IActionResult Index()
    {
        this.SetStore();
        var path = (Request.Path.Value ?? "").Trim('/').ToLowerInvariant();
        ViewBag.Page = path switch
        {
            "media" => ("Media", "Every way to stay in the conversation.", "Podcast, radio, television, news, video and live broadcast — built for curious minds."),
            "programmes" => ("Programmes", "Shows with something to say.", "Talk shows, interviews, documentaries, youth programmes and public forums that turn attention into momentum."),
            "humanity" => ("Humanity", "Impact is part of the story.", "Through education, food, shelter, skills training, healthcare and community outreach, we show up where it matters."),
            "impact" => ("Our impact", "Measure the reach. Remember the people.", "Our growing network connects media attention to practical change across communities."),
            "events" => ("Events", "Meet the moment in person.", "Conferences, youth summits, public forums and live events that bring the room to life."),
            "partner" => ("Partner with us", "Build the next big conversation together.", "Create a partnership audiences trust — from corporate sponsorship and media partnerships to equipment, studio, event, technology and humanitarian support."),
            "guest" => ("Guest application", "Bring your story to the room.", "Tell us what you are building, learning or changing. Our producers review every application."),
            "awards" => ("Celebrity circle", "Honouring the people who make culture.", "Discover the musicians, actors, creators and public voices shaping our shared future."),
            _ => ("Star Talk Sessions", "Stories that move people forward.", "A platform for honest conversations, culture and impact.")
        };
        return View();
    }
}
