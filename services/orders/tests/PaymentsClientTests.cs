using System.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Orders.Api.Payments;
using Xunit;

namespace Orders.Api.Tests;

public class PaymentsClientTests
{
    [Fact]
    public async Task RefundOrCancelOrderPaymentsAsync_CreatesShortLivedDelegatedToken()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        HttpRequestMessage? capturedRequest = null;
        using var handler = new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return HttpStatusCode.NoContent;
        });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://payments-api:8080/")
        };
        using var loggerFactory = LoggerFactory.Create(_ => { });
        var client = new PaymentsClient(httpClient, CreateConfiguration(), loggerFactory.CreateLogger<PaymentsClient>());

        var result = await client.RefundOrCancelOrderPaymentsAsync(orderId, userId, CancellationToken.None);

        Assert.Equal(PaymentsClientResult.Success, result);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal($"/api/payments/orders/{orderId}/refund", capturedRequest.RequestUri!.AbsolutePath);
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization!.Scheme);
        var delegatedToken = new JwtSecurityTokenHandler().ReadJwtToken(capturedRequest.Headers.Authorization.Parameter);
        Assert.Contains(delegatedToken.Claims, claim => claim.Value == userId.ToString());
        Assert.Contains(delegatedToken.Claims, claim =>
            claim.Type == "permission" && claim.Value == "payments.order.cancel");
        Assert.DoesNotContain(delegatedToken.Claims, claim =>
            claim.Type == "permission" && claim.Value == "payments.refund");
        Assert.True(delegatedToken.ValidTo > DateTime.UtcNow);
        Assert.True(delegatedToken.ValidTo <= DateTime.UtcNow.AddMinutes(1));
        var validatedPrincipal = new JwtSecurityTokenHandler().ValidateToken(
            capturedRequest.Headers.Authorization.Parameter,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "ecommerce-platform",
                ValidateAudience = true,
                ValidAudience = "ecommerce-platform",
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes("test-signing-key-with-at-least-thirty-two-characters"))
            },
            out _);
        Assert.Equal(userId.ToString(), validatedPrincipal.FindFirstValue(ClaimTypes.NameIdentifier));
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, PaymentsClientResult.NotFound)]
    [InlineData(HttpStatusCode.ServiceUnavailable, PaymentsClientResult.Unavailable)]
    public async Task RefundOrCancelOrderPaymentsAsync_MapsFailureResponses(
        HttpStatusCode statusCode,
        PaymentsClientResult expectedResult)
    {
        using var handler = new StubHttpMessageHandler(_ => statusCode);
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://payments-api:8080/")
        };
        using var loggerFactory = LoggerFactory.Create(_ => { });
        var client = new PaymentsClient(httpClient, CreateConfiguration(), loggerFactory.CreateLogger<PaymentsClient>());

        var result = await client.RefundOrCancelOrderPaymentsAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Equal(expectedResult, result);
    }

    private static IConfiguration CreateConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = "test-signing-key-with-at-least-thirty-two-characters",
            ["Jwt:Issuer"] = "ecommerce-platform",
            ["Jwt:Audience"] = "ecommerce-platform"
        })
        .Build();

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpStatusCode> respond)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(respond(request)));
        }
    }
}
