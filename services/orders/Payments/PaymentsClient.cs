using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Orders.Api.Payments;

public sealed class PaymentsClient(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<PaymentsClient> logger) : IPaymentsClient
{
    public async Task<PaymentsClientResult> RefundOrCancelOrderPaymentsAsync(
        Guid orderId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/payments/orders/{orderId}/refund");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateCancellationToken(userId));

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return PaymentsClientResult.Success;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return PaymentsClientResult.NotFound;
            }

            logger.LogWarning(
                "Payments API returned {StatusCode} while recording cancellation for order {OrderId}",
                (int)response.StatusCode,
                orderId);
            return PaymentsClientResult.Unavailable;
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Payments API is unavailable for order {OrderId}", orderId);
            return PaymentsClientResult.Unavailable;
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Payments API timed out for order {OrderId}", orderId);
            return PaymentsClientResult.Unavailable;
        }
    }

    private string CreateCancellationToken(Guid userId)
    {
        var signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");
        var issuer = configuration["Jwt:Issuer"] ?? "ecommerce-platform";
        var audience = configuration["Jwt:Audience"] ?? "ecommerce-platform";
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("permission", "payments.order.cancel")
        };
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
