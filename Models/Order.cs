using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace StarTalkSessions.Models
{
    public class Order
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonElement("orderNumber")]
        public string OrderNumber { get; set; }

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.String)]
        public Guid? UserId { get; set; }

        [BsonElement("customerName")]
        public string CustomerName { get; set; }

        [BsonElement("customerEmail")]
        public string CustomerEmail { get; set; }

        [BsonElement("userEmail")]
        public string UserEmail { get; set; }

        [BsonElement("customerPhone")]
        public string CustomerPhone { get; set; }

        [BsonElement("shippingAddress")]
        public string ShippingAddress { get; set; }

        [BsonElement("country")]
        public string Country { get; set; }

        [BsonElement("items")]
        public List<OrderItem> Items { get; set; } = new();

        [BsonElement("subtotal")]
        public decimal Subtotal { get; set; }

        [BsonElement("shipping")]
        public decimal Shipping { get; set; }

        [BsonElement("total")]
        public decimal Total { get; set; }

        [BsonElement("currency")]
        public string Currency { get; set; } = "USD";

        [BsonElement("status")]
        public string Status { get; set; } = "Processing";

        [BsonElement("trackingRef")]
        public string TrackingRef { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [BsonElement("estimatedDelivery")]
        public DateTime? EstimatedDelivery { get; set; }
    }

    public class OrderItem
    {
        [BsonElement("productId")]
        [BsonRepresentation(BsonType.String)]
        public Guid ProductId { get; set; }

        [BsonElement("productName")]
        public string ProductName { get; set; }

        [BsonElement("sku")]
        public string Sku { get; set; }

        [BsonElement("variants")]
        public string Variants { get; set; }

        [BsonElement("quantity")]
        public int Quantity { get; set; } = 1;

        [BsonElement("unitPrice")]
        public decimal UnitPrice { get; set; }

        [BsonElement("image")]
        public string Image { get; set; }
    }
}