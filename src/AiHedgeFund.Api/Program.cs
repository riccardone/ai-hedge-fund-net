using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Mvc;
using NLog.Extensions.Logging;
using AiHedgeFund.Contracts;
using AiHedgeFund.Api.Services;

namespace AiHedgeFund.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddFilter("Microsoft.*", Microsoft.Extensions.Logging.LogLevel.Error);

            logging.AddNLog(new NLogProviderOptions
            {
                ShutdownOnDispose = true,
            });
        });

        builder.Services.AddApiVersioning(options =>
        {
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });

        builder.Services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        builder.Services.AddSingleton<IAuthChecker, InMemoryAuthChecker>(); 

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.DefaultModelsExpandDepth(-1);
            });
        }

        app.UseHttpsRedirection();

        app.UseTenantApiKeyAuth();

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
