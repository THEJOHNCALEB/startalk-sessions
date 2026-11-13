using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace StarTalkSessions.Models
{
    public class BannerAd
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonElement("title")]
        public string Title { get; set; }

        [BsonElement("imageUrl")]
        public string ImageUrl { get; set; }

        [BsonElement("targetUrl")]
        public string TargetUrl { get; set; }

        [BsonElement("placement")]
        public string Placement { get; set; } = "inline";

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("views")]
        public long Views { get; set; } = 0;

        [BsonElement("clicks")]
        public long Clicks { get; set; } = 0;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}