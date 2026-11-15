using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Hubs;
using Orders.Api.Notifications;
using Orders.Api.Payments;
using Orders.Api.Security;
using Orders.Api.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR()
    .AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis")
        ?? throw new InvalidOperationException("ConnectionStrings:Redis is not configured."));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("OrdersDatabase")));
builder.Services.AddScoped<IOrdersRepository, OrdersRepository>();
builder.Services.AddScoped<IOrderStatusOutboxRepository, OrderStatusOutboxRepository>();
builder.Services.AddSingleton<IOrderStatusNotificationSender, SignalROrderStatusNotificationSender>();
builder.Services.AddSingleton<OrderStatusNotificationDispatcher>();
builder.Services.AddHttpClient<IPaymentsClient, PaymentsClient>((serviceProvider, client) =>
{
    var paymentsBaseUrl = builder.Configuration["Services:Payments:BaseUrl"]
        ?? throw new InvalidOperationException("Services:Payments:BaseUrl is not configured.");
    client.BaseAddress = new Uri(paymentsBaseUrl, UriKind.Absolute);
});
builder.Services.AddScoped<IOrdersService, OrdersService>();
builder.Services.AddHostedService<OrderStatusNotificationWorker>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments(OrderNotificationsHub.Route))
                {
                    context.Token = accessToken.ToString();
                }

                return Task.CompletedTask;
            }
        };

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
    options.AddPolicy(PermissionPolicies.OrdersCreate, policy =>
        policy.RequireClaim("permission", "orders.create"));
    options.AddPolicy(PermissionPolicies.OrdersRead, policy =>
        policy.RequireClaim("permission", "orders.read"));
    options.AddPolicy(PermissionPolicies.OrdersManage, policy =>
        policy.RequireClaim("permission", "orders.manage"));
    options.AddPolicy(PermissionPolicies.BulkOrdersUpload, policy =>
        policy.RequireClaim("permission", "orders.bulk.upload"));
    options.AddPolicy(PermissionPolicies.BulkOrdersApprove, policy =>
        policy.RequireClaim("permission", "orders.bulk.approve"));
});
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "orders:";
});

var app = builder.Build();

app.UseSwagger();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<OrderNotificationsHub>(OrderNotificationsHub.Route);
app.MapControllers();

app.Run();
