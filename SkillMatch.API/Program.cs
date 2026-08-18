using Microsoft.EntityFrameworkCore;
using SkillMatch.API.Models;
using SkillMatch.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Get database connection string
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

// Register Entity Framework Core
builder.Services.AddDbContext<SkillMatchDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    )
);

// Add controllers
builder.Services.AddControllers();

// Register application services
builder.Services.AddScoped<ResumeParserService>();
builder.Services.AddScoped<MatchingEngine>();

// Add OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Swagger UI
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "SkillMatch API v1"
        );
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();