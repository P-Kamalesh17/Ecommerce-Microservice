using CartAPI.Data;
using CartAPI.Services;
using CartAPI.Services.IServices;
using ECommerce.Shared.Extensions;
using ECommerce.Shared.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Jwt:Key must come from user-secrets or environment variables (Jwt__Key); it is not stored in appsettings.json.
builder.AddJwtAuthentication();

// Add services to the container.

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectionString"));
});

builder.Services.AddHttpClient("ProductAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ProductApi:BaseUrl"]!);
});

builder.Services.AddControllers();
builder.Services.AddScoped<IProductClient, ProductClient>();
builder.Services.AddScoped<ICartService, CartService>();
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
