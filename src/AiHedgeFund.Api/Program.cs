using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Extensions.Logging;
using AiHedgeFund.Contracts;
using AiHedgeFund.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Swashbuckle.AspNetCore.SwaggerGen;

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
}
