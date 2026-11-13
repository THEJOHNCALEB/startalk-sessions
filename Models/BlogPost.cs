using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace StarTalkSessions.Models
{
    public class BlogPost
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonElement("title")]
        public string Title { get; set; }

        [BsonElement("slug")]
        public string Slug { get; set; }

        [BsonElement("excerpt")]
        public string Excerpt { get; set; }

        [BsonElement("content")]
        public string Content { get; set; }

        [BsonElement("coverImage")]
        public string CoverImage { get; set; }

        [BsonElement("tag")]
        public string Tag { get; set; }

        [BsonElement("author")]
        public string Author { get; set; }

        [BsonElement("readTime")]
        public string ReadTime { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
