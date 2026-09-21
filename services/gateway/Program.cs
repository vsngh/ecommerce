var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "swagger";
    options.SwaggerEndpoint("/orders/swagger/v1/swagger.json", "Orders API");
    options.SwaggerEndpoint("/products/swagger/v1/swagger.json", "Products API");
    options.SwaggerEndpoint("/users/swagger/v1/swagger.json", "Users API");
    options.SwaggerEndpoint("/inventory/swagger/v1/swagger.json", "Inventory API");
    options.SwaggerEndpoint("/cart/swagger/v1/swagger.json", "Cart API");
    options.SwaggerEndpoint("/payments/swagger/v1/swagger.json", "Payments API");
});

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapReverseProxy();

app.Run();
