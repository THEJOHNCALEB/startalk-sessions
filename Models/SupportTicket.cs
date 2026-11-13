using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace StarTalkSessions.Models
{
    public class SupportTicket
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonElement("customerId")]
        public Guid? CustomerId { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("subject")]
        public string Subject { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = "Open";

        [BsonElement("aiAssigned")]
        public bool AiAssigned { get; set; } = true;

        [BsonElement("messages")]
        public List<SupportMessage> Messages { get; set; } = new();

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [BsonIgnore]
        public string LastBody => Messages.LastOrDefault()?.Body ?? "";

        [BsonIgnore]
        public int AiMessageCount => Messages.Count(m => m.Role == "ai");
    }

    public class SupportMessage
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonElement("role")]
        public string Role { get; set; }

        [BsonElement("body")]
        public string Body { get; set; }

        [BsonElement("author")]
        public string Author { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}