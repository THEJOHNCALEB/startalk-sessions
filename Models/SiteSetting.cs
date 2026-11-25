using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace StarTalkSessions.Models
{
    public class SiteSetting
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Key { get; set; } = "";

        [BsonElement("value")]
        public string Value { get; set; } = "";

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
