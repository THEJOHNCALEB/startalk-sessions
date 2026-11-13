using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace StarTalkSessions.Models
{
    public class NewsletterSubscription
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}