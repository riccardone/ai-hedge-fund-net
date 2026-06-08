using System.Net.Http.Headers;
using AiHedgeFund.Agents;
using AiHedgeFund.Agents.Registry;
using AiHedgeFund.Agents.Services;
using AiHedgeFund.Contracts;
using AiHedgeFund.Data;
using AiHedgeFund.Data.AlphaVantage;
using AiHedgeFund.Data.ChartLibrary;
using AiHedgeFund.Data.Mock;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AlphaVantageAuthHandler = AiHedgeFund.Data.AlphaVantage.AlphaVantageAuthHandler;

namespace AiHedgeFund.Console;

internal class Program
{
    private static async Task Main(string[] args)
    {
        System.Console.Title = "AI Hedge Fund - Console App";
        var appArgs = new AppArguments(args);
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton(appArgs);
                services.AddSingleton<IDataReader, AlphaVantageDataReader>();
                services.AddSingleton<IPriceVolumeProvider, FakePriceVolumeProvider>();
                services.AddSingleton<IHttpLib, OpenAiHttp>();
                services.AddSingleton<TradingInitializer>();
                services.AddSingleton<FileDataManager>();
                services.AddSingleton<DataFetcher>();
                services.AddSingleton<IAgentRegistry, AgentRegistry>();
                services.AddSingleton<BenGrahamAgent>();
                services.AddSingleton<CathieWoodAgent>();
                services.AddSingleton<BillAckmanAgent>();
                services.AddSingleton<CharlieMungerAgent>();
                services.AddSingleton<StanleyDruckenmillerAgent>();
                services.AddSingleton<WarrenBuffettAgent>();
                services.AddSingleton<RiskManagerAgent>();
                services.AddHostedService<AgentBootstrapper>();
                services.AddTransient<PortfolioManager>();
                services.AddHostedService<MainApp>();

                var configuration = BuildConfig();
                services.AddHttpClient();
                var apiKey = configuration["AlphaVantage:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey))
                    throw new InvalidOperationException("AlphaVantage API key is missing in configuration.");

                services.AddHttpClient("AlphaVantage", client =>
                    {
                        client.BaseAddress = new Uri("https://www.alphavantage.co");
                    })
                    .AddHttpMessageHandler(() => new RateLimitingHandler())
                    .AddHttpMessageHandler(() => new AlphaVantageAuthHandler(apiKey));
                services.AddHttpClient("OpenAI", client =>
                {
                    var apiKey = configuration["OpenAI:ApiKey"];
                    if (string.IsNullOrEmpty(apiKey))
                        throw new InvalidOperationException("OpenAI API key is missing in configuration.");
                    client.BaseAddress = new Uri("https://api.openai.com/v1/");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                });

                // Optional historical base-rate grounding (off by default). Only does anything
                // when the "Grounding" section sets Enabled=true AND a key is present (config or
                // the CHART_LIBRARY_API_KEY environment variable). Otherwise it is a clean no-op.
                var grounding = configuration.GetSection(GroundingOptions.SectionName).Get<GroundingOptions>()
                                ?? new GroundingOptions();
                var groundingKey = Environment.GetEnvironmentVariable("CHART_LIBRARY_API_KEY");
                if (!string.IsNullOrWhiteSpace(groundingKey))
                    grounding.ApiKey = groundingKey;
                var groundingUrl = Environment.GetEnvironmentVariable("CHART_LIBRARY_API_URL");
                if (!string.IsNullOrWhiteSpace(groundingUrl))
                    grounding.BaseUrl = groundingUrl;
                services.AddSingleton(grounding);
                services.AddSingleton<IBaseRateProvider, ChartLibraryBaseRateProvider>();
                services.AddHttpClient("ChartLibrary", client =>
                {
                    client.BaseAddress = new Uri(grounding.BaseUrl.TrimEnd('/') + "/");
                    client.Timeout = TimeSpan.FromSeconds(grounding.TimeoutSeconds <= 0 ? 8.0 : grounding.TimeoutSeconds);
                });
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
                    options.IncludeScopes = false;
                });
            })
            .Build();

        await host.RunAsync();
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