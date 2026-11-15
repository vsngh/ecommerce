using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Payments.Api.Data;
using Payments.Api.Security;
using Payments.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PaymentsDatabase")));
builder.Services.AddScoped<IPaymentsRepository, PaymentsRepository>();
builder.Services.AddScoped<IPaymentsService, PaymentsService>();
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
    options.AddPolicy(PermissionPolicies.PaymentsRead, policy =>
        policy.RequireClaim("permission", "payments.read"));
    options.AddPolicy(PermissionPolicies.PaymentsManage, policy =>
        policy.RequireClaim("permission", "payments.manage"));
    options.AddPolicy(PermissionPolicies.PaymentsCreate, policy =>
        policy.RequireClaim("permission", "payments.create"));
    options.AddPolicy(PermissionPolicies.PaymentsRefund, policy =>
        policy.RequireClaim("permission", "payments.refund"));
    options.AddPolicy(PermissionPolicies.OrderCancellation, policy =>
        policy.RequireClaim("permission", "payments.order.cancel"));
});
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "payments:";
});

var app = builder.Build();

app.UseSwagger();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
