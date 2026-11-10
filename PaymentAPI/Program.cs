using ECommerce.Shared.Extensions;
using ECommerce.Shared.Middleware;
using Microsoft.EntityFrameworkCore;
using PaymentAPI.Data;
using PaymentAPI.Services;
using PaymentAPI.Services.IServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectionString"));
});

// Jwt:Key must come from user-secrets or environment variables (Jwt__Key); it is not stored in appsettings.json.
builder.AddJwtAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddHttpClient("OrderAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["OrderApi:BaseUrl"]!);
});

builder.Services.AddControllers();
builder.Services.AddScoped<IOrderClient, OrderClient>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
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
