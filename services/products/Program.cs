using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Products.Api.Data;
using Products.Api.Security;
using Products.Api.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ProductsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ProductsDatabase")));
builder.Services.AddScoped<IProductsRepository, ProductsRepository>();
builder.Services.AddScoped<IProductsService, ProductsService>();
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
    options.AddPolicy(PermissionPolicies.ProductsManage, policy =>
        policy.RequireClaim("permission", "products.manage"));
});
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "products:";
});

var app = builder.Build();

app.UseSwagger();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
