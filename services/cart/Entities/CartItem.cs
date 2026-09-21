using System.Text.Json.Serialization;

namespace Cart.Api.Entities;

public class CartItem
{
    public Guid Id { get; set; }

    public Guid CartId { get; set; }

    [JsonIgnore]
    public Cart? Cart { get; set; }

    public Guid ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
