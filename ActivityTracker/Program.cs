using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using ActivityTracker.Data;
using ActivityTracker.Models;
using ActivityTracker.Services;


var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// 1. Konfiguracja bazy danych
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

// 2. Konfiguracja ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 3. Konfiguracja Swaggera (OpenAPI)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Activity Tracker API", Version = "v1" });
});

builder.Services.AddControllers();
builder.Services.AddScoped<DataSeeder>();

var app = builder.Build();

// Konfiguracja pipeline'u HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => // Teraz ta linia nie powinna powodowaæ b³êdu
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Activity Tracker API V1");
        c.RoutePrefix = "api/documentation";
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Seedowanie bazy danych
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var seeder = services.GetRequiredService<DataSeeder>();
    await seeder.SeedRolesAsync();
    await seeder.SeedAdminAsync();
}

app.Run();

