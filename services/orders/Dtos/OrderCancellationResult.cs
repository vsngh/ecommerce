namespace Orders.Api.Dtos;

public enum OrderCancellationResult
{
    Cancelled,
    NotFound,
    Conflict,
    PaymentsUnavailable
}
