using ECommerce.Shared.Extensions;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Container/Azure runs set ASPNETCORE_ENVIRONMENT=Docker to use ocelot.Docker.json (service-name hosts, http, port 8080).
var ocelotFile = builder.Configuration["OCELOT_FILE"]
    ?? (builder.Environment.IsEnvironment("Docker") ? "ocelot.Docker.json" : "ocelot.json");
builder.Configuration.AddJsonFile(ocelotFile, optional: false, reloadOnChange: true);

// Validates the same JWT the services use (scheme "Bearer"); Jwt:Key comes from shared user-secrets / Jwt__Key.
builder.AddJwtAuthentication();

// Extra allowed origins (e.g. the deployed frontend) via Cors__Origins="https://a.com,https://b.com".
var origins = new[] { "http://localhost:5173", "http://localhost:3000" }
    .Concat((builder.Configuration["Cors:Origins"] ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    .ToArray();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(origins)
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.UseCors();
await app.UseOcelot();

app.Run();
