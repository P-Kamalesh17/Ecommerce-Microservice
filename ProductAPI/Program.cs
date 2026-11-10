using ECommerce.Shared.Authorization;
using ECommerce.Shared.Extensions;
using ECommerce.Shared.Middleware;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Services;
using ProductAPI.Services.IServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectionString"));
});

// Jwt:Key must come from user-secrets or environment variables (Jwt__Key).
builder.AddJwtAuthentication();
builder.Services.AddAuthorization();

// Shared secret for service-to-service stock calls; from user-secrets or the Internal__ApiKey environment variable.
InternalServiceKey.Require(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddScoped<IImageStorageService, ImageStorageService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
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

app.UseStaticFiles();

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
        if(_db.Database.GetPendingMigrations().Count() > 0)
        {
            _db.Database.Migrate();
        }
    }
}
