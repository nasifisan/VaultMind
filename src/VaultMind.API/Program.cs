using Microsoft.SemanticKernel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VaultMind.API.Services;
using VaultMind.API.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ──
var config = builder.Configuration;

// ── Services ──
builder.Services.AddSingleton<ISseService, SseService>();
builder.Services.AddSingleton<IMongoDbContext, MongoDbContext>();
builder.Services.AddSingleton(typeof(IMongoRepository<>), typeof(MongoRepository<>));
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddHostedService<MongoDbInitializer>();

builder.Services.AddKernel();
builder.Services.AddOpenAIChatCompletion(
    modelId: config["AI:ModelId"] ?? "phi3",
    apiKey: config["AI:ApiKey"] ?? "ollama",           // Ollama doesn't need a real key
    endpoint: new Uri(config["AI:Endpoint"] ?? "http://localhost:11434/v1")
);

// Register Controllers and preserve PascalCase casing for JSON serialization
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDashboard", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configure JwtBearer authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secret = config["Auth:JwtSecret"] ?? "your-super-secret-vaultmind-jwt-signing-key-2026-must-be-long-enough";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = config["Auth:JwtIssuer"] ?? "VaultMind.API",
            ValidAudience = config["Auth:JwtAudience"] ?? "VaultMind.Dashboard",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── Build ──
var app = builder.Build();

// ── Middleware ──
app.UseCors("AllowDashboard");
app.UseAuthentication();
app.UseAuthorization();

// ── Map Controllers ──
app.MapControllers();

app.Run();
