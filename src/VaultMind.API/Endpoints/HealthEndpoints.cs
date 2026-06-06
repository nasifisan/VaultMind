namespace VaultMind.API.Endpoints;

public static class HealthEndpoints
{
    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/api/health", () =>
            Results.Ok(new
            {
                status = "healthy",
                service = "VaultMind.API",
                timestamp = DateTime.UtcNow
            })
        );

        return app;
    }
}
