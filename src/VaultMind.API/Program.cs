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
    modelId: config["AI:ModelId"] ?? "phi3",
    apiKey: config["AI:ApiKey"] ?? "ollama",           // Ollama doesn't need a real key
    endpoint: new Uri(config["AI:Endpoint"] ?? "http://localhost:11434/v1")
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
