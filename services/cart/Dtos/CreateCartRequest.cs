namespace Cart.Api.Dtos;

public class CreateCartRequest
{
    public Guid UserId { get; set; }
    public string Status { get; set; } = "Active";
    public ICollection<CreateCartItemRequest> CartItems { get; set; } = new List<CreateCartItemRequest>();
}
