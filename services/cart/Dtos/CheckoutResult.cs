namespace Cart.Api.Dtos;

public record CheckoutResult(Guid Id, Guid UserId, string Status, string PaymentMethod, int ItemCount);
