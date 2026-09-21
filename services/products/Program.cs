using Microsoft.EntityFrameworkCore;
using Products.Api.Data;
using Products.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ProductsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ProductsDatabase")));
builder.Services.AddScoped<IProductsService, ProductsService>();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "products:";
});

var app = builder.Build();

app.UseSwagger();

app.MapControllers();

app.Run();
