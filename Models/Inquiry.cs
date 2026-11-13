using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace StarTalkSessions.Models
{
    public class Inquiry
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("phone")]
        public string Phone { get; set; }

        [BsonElement("type")]
        public string Type { get; set; }

        [BsonElement("message")]
        public string Message { get; set; }

        [BsonElement("attachmentName")]
        public string AttachmentName { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = "New";

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}