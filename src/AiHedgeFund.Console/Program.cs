using System.Net.Http.Headers;
using AiHedgeFund.Agents;
using AiHedgeFund.Agents.Registry;
using AiHedgeFund.Agents.Services;
using AiHedgeFund.Contracts;
using AiHedgeFund.Data;
using AiHedgeFund.Data.AlphaVantage;
using AiHedgeFund.Data.Mock;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using AlphaVantageAuthHandler = AiHedgeFund.Data.AlphaVantageAuthHandler;

namespace AiHedgeFund.Console;

internal class Program
{
    private static async Task Main(string[] args)
    {
        if (args.Contains("--help") || args.Contains("-h") || args.Length == 0)
        {
            PrintHelp();
            Environment.Exit(0);
        }

        var appArgs = new AppArguments(args);
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton(appArgs);
                services.AddSingleton<IDataReader, AlphaVantageDataReader>();
                services.AddSingleton<IPriceVolumeProvider, FakePriceVolumeProvider>();
                services.AddSingleton<IValuationEngine, DefaultValuationEngine>();
                services.AddSingleton<IHttpLib, OpenAiHttp>();
                services.AddSingleton<TradingInitializer>();
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
                services.AddHttpClient("AlphaVantage", client =>
                {
                    var apiKey = configuration["AlphaVantage:ApiKey"];
                    if (string.IsNullOrEmpty(apiKey))
                        throw new InvalidOperationException("AlphaVantage API key is missing in configuration.");
                    client.BaseAddress = new Uri("https://www.alphavantage.co");
                }).AddHttpMessageHandler(() => new AlphaVantageAuthHandler(configuration["AlphaVantage:ApiKey"]));
                services.AddHttpClient("OpenAI", client =>
                {
                    var apiKey = configuration["OpenAI:ApiKey"];
                    if (string.IsNullOrEmpty(apiKey))
                        throw new InvalidOperationException("OpenAI API key is missing in configuration.");
                    client.BaseAddress = new Uri("https://api.openai.com/v1/");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                });
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Trace);
                logging.AddNLog("nlog.config");
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

    private static readonly List<string> AvailableAgents = new()
    {
        "charlie_munger", "stanley_druckenmiller", "ben_graham", "cathie_wood", "bill_ackman", "warren_buffett"
    };

    private static void PrintHelp()
    {
        System.Console.WriteLine("Usage:");
        System.Console.WriteLine("  --agent [names]        : One or more agent names to run (default: charlie_munger)");
        System.Console.WriteLine("                           Available agents:");
        foreach (var agent in AvailableAgents)
        {
            System.Console.WriteLine($"                             - {agent}");
        }

        System.Console.WriteLine("  --tickers [symbols]    : One or more stock tickers (e.g., MSFT AAPL TSLA)");
        System.Console.WriteLine("  --start-date YYYY-MM-DD: Optional start date (default: 3 months ago)");
        System.Console.WriteLine("  --end-date YYYY-MM-DD  : Optional end date (default: today)");
        System.Console.WriteLine("  --risk-level [level]   : Optional valuation risk level: low, medium, or high (default: medium)");
        System.Console.WriteLine("  --help or -h           : Show this help message and exit");
    }
}