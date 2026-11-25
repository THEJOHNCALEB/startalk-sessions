using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace StarTalkSessions.Models
{
    public class LiveStream
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonElement("title")]
        public string Title { get; set; } = "";

        [BsonElement("description")]
        public string Description { get; set; } = "";

        [BsonElement("platform")]
        public string Platform { get; set; } = "youtube";

        [BsonElement("youtubeUrl")]
        public string YouTubeUrl { get; set; } = "";

        [BsonElement("tiktokUrl")]
        public string TikTokUrl { get; set; } = "";

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [BsonElement("scheduledAt")]
        public DateTime ScheduledAt { get; set; } = DateTime.Now;

        [BsonElement("durationMinutes")]
        public int DurationMinutes { get; set; } = 60;

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [BsonIgnore]
        public bool HasYouTube => Platform != "tiktok" && !string.IsNullOrWhiteSpace(YouTubeUrl);

        [BsonIgnore]
        public bool HasTikTok => Platform != "youtube" && !string.IsNullOrWhiteSpace(TikTokUrl);

        [BsonIgnore]
        public DateTime EndsAt => ScheduledAt.AddMinutes(Math.Max(15, DurationMinutes));

        public bool IsLiveAt(DateTime now) => IsActive && now >= ScheduledAt && now <= EndsAt;

        public bool IsUpcomingAt(DateTime now) => IsActive && now < ScheduledAt;

        [BsonIgnore]
        public string YouTubeEmbedUrl
        {
            get
            {
                var value = (YouTubeUrl ?? "").Trim();
                if (value.Length == 0) return "";
                var id = "";
                try
                {
                    var uri = new Uri(value);
                    if (uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
                    {
                        id = uri.AbsolutePath.Trim('/');
                    }
                    else if (uri.Query.Contains("v=", StringComparison.OrdinalIgnoreCase))
                    {
                        var pair = uri.Query.TrimStart('?').Split('&')
                            .FirstOrDefault(p => p.StartsWith("v=", StringComparison.OrdinalIgnoreCase));
                        id = pair?[2..] ?? "";
                    }
                    else
                    {
                        var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                        var embed = Array.IndexOf(parts, "embed");
                        var live = Array.IndexOf(parts, "live");
                        var index = embed >= 0 ? embed : live;
                        if (index >= 0 && index + 1 < parts.Length) id = parts[index + 1];
                    }
                }
                catch (UriFormatException) { return ""; }
                var videoId = new string(id.TakeWhile(c => char.IsLetterOrDigit(c) || c is '_' or '-').ToArray());
                return videoId.Length == 0 ? "" : $"https://www.youtube-nocookie.com/embed/{videoId}?autoplay=1&rel=0";
            }
        }
    }
}
