using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace StarTalkSessions.Models
{
    public class HomeContent
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonElement("section")]
        public string Section { get; set; } = "recommended";

        [BsonElement("kicker")]
        public string Kicker { get; set; } = "";

        [BsonElement("title")]
        public string Title { get; set; } = "";

        [BsonElement("summary")]
        public string Summary { get; set; } = "";

        [BsonElement("duration")]
        public string Duration { get; set; } = "";

        [BsonElement("badge")]
        public string Badge { get; set; } = "";

        [BsonElement("tone")]
        public string Tone { get; set; } = "live";

        [BsonElement("schedule")]
        public string Schedule { get; set; } = "";

        [BsonElement("imageUrl")]
        public string ImageUrl { get; set; } = "";

        [BsonElement("targetUrl")]
        public string TargetUrl { get; set; } = "";

        [BsonElement("sortOrder")]
        public int SortOrder { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
