using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace StarTalkSessions.Models
{
    public class Project
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonElement("title")]
        public string Title { get; set; }

        [BsonElement("slug")]
        public string Slug { get; set; }

        [BsonElement("client")]
        public string Client { get; set; }

        [BsonElement("segment")]
        public string Segment { get; set; }

        [BsonElement("location")]
        public string Location { get; set; }

        [BsonElement("year")]
        public string Year { get; set; }

        [BsonElement("brief")]
        public string Brief { get; set; }

        [BsonElement("challenge")]
        public string Challenge { get; set; }

        [BsonElement("solution")]
        public string Solution { get; set; }

        [BsonElement("coverImage")]
        public string CoverImage { get; set; }

        [BsonElement("beforeImage")]
        public string BeforeImage { get; set; }

        [BsonElement("afterImage")]
        public string AfterImage { get; set; }

        [BsonElement("gallery")]
        public List<string> Gallery { get; set; } = new();

        [BsonElement("featuredProductNames")]
        public List<string> FeaturedProductNames { get; set; } = new();

        [BsonElement("isFeatured")]
        public bool IsFeatured { get; set; } = false;

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}