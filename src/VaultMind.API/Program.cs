using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel;
using VaultMind.API.Interfaces;
using VaultMind.API.Plugins;
using VaultMind.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ──
var config = builder.Configuration;

// ── Services ──
builder.Services.AddSingleton<ISseService, SseService>();
builder.Services.AddSingleton<IMongoDbContext, MongoDbContext>();
builder.Services.AddSingleton(typeof(IMongoRepository<>), typeof(MongoRepository<>));
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddSingleton<IStorageService, GoogleStorageService>();
builder.Services.AddSingleton<IDocumentParserService, DocumentParserService>();
builder.Services.AddSingleton<IChunkingService, ChunkingService>();
builder.Services.AddSingleton<IVectorStoreService, QdrantVectorStoreService>();
builder.Services.AddSingleton<IIngestionService, IngestionService>();
builder.Services.AddHostedService<MongoDbInitializer>();


// AI Service initialization ----->
builder.Services.AddOpenAIChatCompletion(
    modelId: config["AI:ModelId"] ?? "phi3",
    apiKey: config["AI:ApiKey"] ?? "ollama",           // Ollama doesn't need a real key
    endpoint: new Uri(config["AI:Endpoint"] ?? "http://localhost:11434/v1")
);

builder.Services.AddOpenAITextEmbeddingGeneration(
    modelId: config["Embedding:ModelId"] ?? "nomic-embed-text",
    openAIClient: new global::OpenAI.OpenAIClient(
        new global::System.ClientModel.ApiKeyCredential(config["Embedding:ApiKey"] ?? "ollama"),
        new global::OpenAI.OpenAIClientOptions
        {
            Endpoint = new Uri(config["Embedding:Endpoint"] ?? "http://localhost:11434/v1"),
            NetworkTimeout = TimeSpan.FromMinutes(5)
        }
    )
);

builder.Services.AddSingleton(sp =>
{
    var kernel = new Kernel(sp);

    // Register Native C# Plugin
    kernel.Plugins.AddFromType<UtilityPlugin>("UtilityPlugin");

    // Register Semantic Prompt Plugin
    var promptsPath = Path.Combine(AppContext.BaseDirectory, "Prompts");
    var chatPluginPath = Path.Combine(promptsPath, "ChatPlugin");
    if (Directory.Exists(chatPluginPath))
    {
        kernel.ImportPluginFromPromptDirectory(chatPluginPath, "ChatPlugin");
    }

    // Diagnostic output to see what is loaded
    foreach (var plugin in kernel.Plugins)
    {
        Console.WriteLine($"[DIAGNOSTIC] Loaded Plugin: '{plugin.Name}'");
        foreach (var function in plugin)
        {
            Console.WriteLine($"[DIAGNOSTIC]   Function: '{function.Name}'");
        }
    }

    return kernel;
});

//End AI service initialization ----<>

// Register Controllers and preserve PascalCase casing for JSON serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
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

// Warm up and verify Kernel plugins on startup, avoid lazy loading
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetService<Kernel>();
}

app.Run();

