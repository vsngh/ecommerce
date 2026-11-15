using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Users.Api.Dtos;
using Users.Api.Entities;

namespace Users.Api.Security;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public AuthResponse CreateToken(User user)
    {
        var permissions = PermissionCatalog.ForRole(user.Role);
        var expiresAt = DateTime.UtcNow.AddHours(2);
        var signingKey = configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");
        var issuer = configuration["Jwt:Issuer"] ?? "ecommerce-platform";
        var audience = configuration["Jwt:Audience"] ?? "ecommerce-platform";

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };

        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AuthResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            Permissions = permissions
        };
    }
}
