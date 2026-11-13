using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace StarTalkSessions.Models
{
    public class UserModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; }

        [BsonElement("firstName")]
        public string FirstName { get; set; }

        [BsonElement("lastName")]
        public string LastName { get; set; }

        [BsonElement("phone")]
        public string Phone { get; set; }

        [BsonElement("address")]
        public string Address { get; set; }

        [BsonElement("city")]
        public string City { get; set; }

        [BsonElement("country")]
        public string Country { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("isAdmin")]
        public bool IsAdmin { get; set; } = false;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [BsonElement("lastLogin")]
        public DateTime? LastLogin { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [BsonElement("passwordResetTokenHash")]
        [BsonIgnoreIfNull]
        public string? PasswordResetTokenHash { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [BsonElement("passwordResetExpiresAt")]
        [BsonIgnoreIfNull]
        public DateTime? PasswordResetExpiresAt { get; set; }

        [BsonIgnore]
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}