using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace StarTalkSessions.Models
{
    public class CustomQuote
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonElement("refNumber")]
        public string RefNumber { get; set; }

        [BsonElement("category")]
        public string Category { get; set; }

        [BsonElement("style")]
        public string Style { get; set; }

        [BsonElement("referenceFiles")]
        public List<string> ReferenceFiles { get; set; } = new();

        [BsonElement("dimensionUnit")]
        public string DimensionUnit { get; set; }

        [BsonElement("width")]
        public string Width { get; set; }

        [BsonElement("height")]
        public string Height { get; set; }

        [BsonElement("depth")]
        public string Depth { get; set; }

        [BsonElement("wood")]
        public string Wood { get; set; }

        [BsonElement("fabric")]
        public string Fabric { get; set; }

        [BsonElement("hardware")]
        public string Hardware { get; set; }

        [BsonElement("cushionDensity")]
        public string CushionDensity { get; set; }

        [BsonElement("budget")]
        public string Budget { get; set; }

        [BsonElement("deliveryBy")]
        public string DeliveryBy { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("phone")]
        public string Phone { get; set; }

        [BsonElement("notes")]
        public string Notes { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = "Submitted";

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.String)]
        public Guid? UserId { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}