using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Extensions.Logging;
using AiHedgeFund.Contracts;
using AiHedgeFund.Api.Services;
using Microsoft.AspNetCore.Authentication;
using AiHedgeFund.Agents.Registry;
using AiHedgeFund.Agents.Services;
using AiHedgeFund.Agents;
using AiHedgeFund.Data;
using AiHedgeFund.Data.AlphaVantage;
using AiHedgeFund.Data.Mock;
using System.Net.Http.Headers;
using DataFetcherFromMemoryOrRemote = AiHedgeFund.Api.Services.DataFetcherFromMemoryOrRemote;

namespace AiHedgeFund.Api;

public class Program
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Logging
        builder.Services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddFilter("Microsoft.*", Microsoft.Extensions.Logging.LogLevel.Error);
            logging.AddNLog(new NLogProviderOptions { ShutdownOnDispose = true });
        });

        // API Versioning
        builder.Services.AddApiVersioning(options =>
        {
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });

        var configuration = BuildConfig();
        builder.Services.AddSingleton<IDataReader, AlphaVantageDataReader>();
        builder.Services.AddSingleton<IPriceVolumeProvider, FakePriceVolumeProvider>();
        builder.Services.AddSingleton<IValuationEngine, DefaultValuationEngine>();
        builder.Services.AddSingleton<IDataFetcher, DataFetcherFromMemoryOrRemote>();
        builder.Services.AddSingleton<IHttpLib, OpenAiChatter>();
        builder.Services.AddSingleton<TradingInitializer>();
        builder.Services.AddSingleton<IAgentRegistry, AgentRegistry>();
        builder.Services.AddSingleton<BenGrahamAgent>();
        builder.Services.AddSingleton<CathieWoodAgent>();
        builder.Services.AddSingleton<BillAckmanAgent>();
        builder.Services.AddSingleton<CharlieMungerAgent>();
        builder.Services.AddSingleton<StanleyDruckenmillerAgent>();
        builder.Services.AddSingleton<WarrenBuffettAgent>();
        builder.Services.AddSingleton<RiskManagerAgent>();
        builder.Services.AddHostedService<AgentBootstrapper>();
        builder.Services.AddTransient<PortfolioManager>();
        builder.Services.AddHttpClient();
        builder.Services.AddHttpClient("AlphaVantage", client =>
        {
            var apiKey = configuration["ApiSettings:AlphaVantage:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("AlphaVantage API key is missing in configuration.");
            client.BaseAddress = new Uri("https://www.alphavantage.co");
        }).AddHttpMessageHandler(() => new AlphaVantageAuthHandler(configuration["ApiSettings:AlphaVantage:ApiKey"]));
        builder.Services.AddHttpClient("OpenAI", client =>
        {
            var apiKey = configuration["ApiSettings:OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("OpenAI API key is missing in configuration.");
            client.BaseAddress = new Uri("https://api.openai.com/v1/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        });

        builder.Services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "HeaderAuth";
                options.DefaultChallengeScheme = "HeaderAuth";
            })
            .AddScheme<AuthenticationSchemeOptions, NoOpAuthenticationHandler>("HeaderAuth", null);

        // Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "AiHedgeFund API",
                Version = "v1"
            });

            // Attach API version info to each endpoint
            options.DocInclusionPredicate((docName, apiDesc) =>
            {
                return apiDesc.GroupName == docName;
            });


            // This makes the version appear in route if using [MapToApiVersion]
            options.TagActionsBy(api =>
            {
                var controllerActionDescriptor =
                    api.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
                return new[] { controllerActionDescriptor?.ControllerName ?? "Default" };
            });

            // Support for custom headers like X-API-KEY
            options.AddSecurityDefinition("X-API-KEY", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Name = "X-API-KEY",
                Type = SecuritySchemeType.ApiKey,
                Description = "API Key for authentication"
            });

            options.AddSecurityDefinition("X-TENANT-ID", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Name = "X-TENANT-ID",
                Type = SecuritySchemeType.ApiKey,
                Description = "Tenant ID for multi-tenancy"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "X-API-KEY" }
                    },
                    Array.Empty<string>()
                },
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "X-TENANT-ID" }
                    },
                    Array.Empty<string>()
                }
            });
        });


        builder.Services.AddSingleton<IAuthChecker, InMemoryAuthChecker>();
        builder.Services.AddControllers(); 

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "AiHedgeFund API v1");
                c.DefaultModelsExpandDepth(-1);
            });

            var swaggerUrl = builder.Configuration["Kestrel:Endpoints:Https:Url"] + "/swagger/index.html";
            Logger.Info("Swagger UI available at: {0}", swaggerUrl);
        }

        app.UseRouting();
        app.UseHttpsRedirection();
        app.UseTenantApiKeyAuth();
        app.UseAuthentication(); // Optional if using schemes
        app.UseAuthorization();  // Required for [Authorize]

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers(); 
        });

        app.MapGet("/status", () => Results.Ok(new
        {
            Status = "Running",
            Time = DateTime.UtcNow
        }));

        app.MapFallback(context =>
        {
            Logger.Warn("Unhandled path: {0}", context.Request.Path);
            context.Response.StatusCode = 404;
            return context.Response.WriteAsync($"Path '{context.Request.Path}' not found.");
        });


        app.Run();
        LogManager.Shutdown();
    }

    private static IConfigurationRoot BuildConfig()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "dev";
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();
        return builder.Build();
    }
}
