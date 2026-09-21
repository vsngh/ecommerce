using Microsoft.EntityFrameworkCore;
using Payments.Api.Data;
using Payments.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PaymentsDatabase")));
builder.Services.AddScoped<IPaymentsService, PaymentsService>();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "payments:";
});

var app = builder.Build();

app.UseSwagger();

app.MapControllers();

app.Run();
