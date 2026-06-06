using Microsoft.SemanticKernel;
using VaultMind.API.Endpoints;
using VaultMind.API.Services;
using VaultMind.API.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ──
var config = builder.Configuration;

// ── Services ──
builder.Services.AddSingleton<ISseService, SseService>();
builder.Services.AddKernel();
builder.Services.AddOpenAIChatCompletion(
    modelId: config["OpenAI:ModelId"] ?? "gpt-4o-mini",
    apiKey: config["OpenAI:ApiKey"] ?? throw new InvalidOperationException(
        "OpenAI:ApiKey is required. Set it in appsettings.json or environment variables.")
);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDashboard", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ── Build ──
var app = builder.Build();

// ── Middleware ──
app.UseCors("AllowDashboard");

// ── Map Endpoints ──
app.MapHealthEndpoints();
app.MapChatEndpoints();

app.Run();
