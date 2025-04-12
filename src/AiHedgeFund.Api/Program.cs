
namespace AiHedgeFund.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Load API key from appsettings.json or environment
        var apiKey = builder.Configuration["ApiKey"] ?? throw new InvalidOperationException("API key not configured.");

        // Register services
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Enable Swagger UI in development
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        // API Key authentication middleware
        app.Use(async (context, next) =>
        {
            if (!context.Request.Headers.TryGetValue("X-API-KEY", out var providedKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("API Key is missing.");
                return;
            }

            if (!string.Equals(providedKey, apiKey, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Invalid API Key.");
                return;
            }

            await next();
        });

        app.MapGet("/status", () =>
        {
            return Results.Ok(new
            {
                Status = "Running",
                Time = DateTime.UtcNow
            });
        });

        app.Run();
    }
}