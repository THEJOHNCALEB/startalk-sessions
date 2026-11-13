using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace StarTalkSessions.Models
{
    public class Testimonial
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("role")]
        public string Role { get; set; }

        [BsonElement("location")]
        public string Location { get; set; }

        [BsonElement("content")]
        public string Content { get; set; }

        [BsonElement("rating")]
        public int Rating { get; set; } = 5;

        [BsonElement("tag")]
        public string Tag { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}