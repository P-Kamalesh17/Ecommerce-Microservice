using ECommerce.Shared.Authorization;
using ECommerce.Shared.Extensions;
using ECommerce.Shared.Middleware;
using Microsoft.EntityFrameworkCore;
using OrderAPI.Data;
using OrderAPI.Services;
using OrderAPI.Services.IServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectionString"));
});

// Jwt:Key must come from user-secrets or environment variables (Jwt__Key); it is not stored in appsettings.json.
builder.AddJwtAuthentication();
builder.Services.AddAuthorization();

// Shared secret for service-to-service stock calls; from user-secrets or the Internal__ApiKey environment variable.
InternalServiceKey.Require(builder.Configuration);

builder.Services.AddHttpClient("ProductAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ProductApi:BaseUrl"]!);
});

builder.Services.AddHttpClient("CartAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["CartApi:BaseUrl"]!);
});

builder.Services.AddHttpClient("PaymentAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PaymentApi:BaseUrl"]!);
});

builder.Services.AddHttpClient("AuthAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["AuthApi:BaseUrl"]!);
});

builder.Services.AddControllers();
builder.Services.AddScoped<ICartClient, CartClient>();
builder.Services.AddScoped<IPaymentClient, PaymentClient>();
builder.Services.AddScoped<IProductClient, ProductClient>();
builder.Services.AddScoped<IUserClient, UserClient>();
builder.Services.AddScoped<IOrderService, OrderService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.AddSwaggerGen();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiXssMiddleware();

app.MapControllers();

ApplyMigration();

app.Run();


void ApplyMigration()
{
    using (var scope = app.Services.CreateScope())
    {
        var _db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (_db.Database.GetPendingMigrations().Count() > 0)
        {
            _db.Database.Migrate();
        }
    }
}
