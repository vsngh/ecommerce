using Cart.Api.Data;
using Cart.Api.Security;
using Cart.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<CartDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CartDatabase")));
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var signingKey = builder.Configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "ecommerce-platform",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "ecommerce-platform",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PermissionPolicies.CartsRead, policy =>
        policy.RequireClaim("permission", "carts.read"));
    options.AddPolicy(PermissionPolicies.CartsManage, policy =>
        policy.RequireClaim("permission", "carts.manage"));
});
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "cart:";
});

var app = builder.Build();

app.UseSwagger();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
