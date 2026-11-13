using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace StarTalkSessions.Models
{
    public class VariantOption
    {
        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("swatch")]
        public string Swatch { get; set; }

        [BsonElement("priceDelta")]
        public decimal PriceDelta { get; set; } = 0;
    }

    public class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("slug")]
        public string Slug { get; set; }

        [BsonElement("sku")]
        public string Sku { get; set; }

        [BsonElement("categoryId")]
        [BsonRepresentation(BsonType.String)]
        public Guid CategoryId { get; set; }

        [BsonElement("categoryName")]
        public string CategoryName { get; set; }

        [BsonElement("price")]
        public decimal Price { get; set; }

        [BsonElement("compareAtPrice")]
        public decimal? CompareAtPrice { get; set; }

        [BsonElement("shortDescription")]
        public string ShortDescription { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("style")]
        public string Style { get; set; }

        [BsonElement("materials")]
        public List<VariantOption> Materials { get; set; } = new();

        [BsonElement("finishes")]
        public List<VariantOption> Finishes { get; set; } = new();

        [BsonElement("sizes")]
        public List<VariantOption> Sizes { get; set; } = new();

        [BsonElement("badges")]
        public List<string> Badges { get; set; } = new();

        [BsonElement("images")]
        public List<string> Images { get; set; } = new();

        [BsonElement("dimensions")]
        public string Dimensions { get; set; }

        [BsonElement("weight")]
        public string Weight { get; set; }

        [BsonElement("careInstructions")]
        public string CareInstructions { get; set; }

        [BsonElement("leadTime")]
        public string LeadTime { get; set; }

        [BsonElement("warranty")]
        public string Warranty { get; set; }

        [BsonElement("stock")]
        public int Stock { get; set; } = 0;

        [BsonElement("rating")]
        public double Rating { get; set; } = 5.0;

        [BsonElement("reviewCount")]
        public int ReviewCount { get; set; } = 0;

        [BsonElement("isFeatured")]
        public bool IsFeatured { get; set; } = false;

        [BsonElement("isCustomizable")]
        public bool IsCustomizable { get; set; } = false;

        [BsonElement("isNew")]
        public bool IsNew { get; set; } = false;

        [BsonElement("isBestSeller")]
        public bool IsBestSeller { get; set; } = false;

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [BsonIgnore]
        public decimal EffectivePrice => CompareAtPrice ?? Price;
    }
}