﻿using ai_hedge_fund_net.ConsoleApp.WorkflowSteps;
using ai_hedge_fund_net.Contracts.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using System.Net.Http.Headers;
using ai_hedge_fund_net.Agents;
using ai_hedge_fund_net.Contracts;
using ai_hedge_fund_net.Data;
using WorkflowCore.Interface;

namespace ai_hedge_fund_net.ConsoleApp;

public class Program
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static async Task Main(string[] args)
    {
        var initialCash = ParseDoubleArgument(args, "--initial-cash", 100000.0);
        var marginRequirement = ParseDoubleArgument(args, "--margin-requirement", 0.0);

        var tickers = ParseArgument(args, "--tickers")?.Split(',') ?? new string[] { "MSFT", "AAPL" };

        var endDate = ParseArgument(args, "--end-date") ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
        var startDate = ParseArgument(args, "--start-date") ?? DateTime.UtcNow.AddMonths(-3).ToString("yyyy-MM-dd");

        var showReasoning = args.Contains("--show-reasoning");
        var selectedAnalysts = ParseArgument(args, "--analysts")?.Split(',') ?? new string[] { "BenGraham" };
        var modelName = ParseArgument(args, "--llm") ?? "gpt-4o";
        var modelProvider = "OpenAI"; // Default, can be changed dynamically

        Logger.Info($"Initial Cash: {initialCash}");
        Logger.Info($"Margin Requirement: {marginRequirement}");
        Logger.Info($"Selected Tickers: {string.Join(", ", tickers)}");
        Logger.Info($"Start Date: {startDate}");
        Logger.Info($"End Date: {endDate}");
        Logger.Info($"Show Reasoning: {showReasoning}");
        Logger.Info($"Model Name: {modelName}");

        var services = new ServiceCollection()
            .AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddNLog();
                loggingBuilder.AddConsole();
                loggingBuilder.AddFilter("Microsoft.*", Microsoft.Extensions.Logging.LogLevel.Error);
                loggingBuilder.AddFilter("System.Net.Http.*", Microsoft.Extensions.Logging.LogLevel.Error);
            })
            .AddWorkflow()
            .AddSingleton<TradingWorkflow>()
            .AddSingleton<BenGrahamStep>()
            .AddSingleton<BillAckmanStep>()
            .AddSingleton<CathieWoodStep>()
            .AddSingleton<CharlieMungerStep>()
            .AddSingleton<WarrenBuffettStep>()
            .AddSingleton<InitializeTradingStateStep>()
            .AddSingleton<AnalyzeFinancialMetricsStep>()
            .AddSingleton<RiskManagementStep>()
            .AddSingleton<PortfolioManagementStep>();

        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();
        var configuration = configBuilder.Build();
        services.AddSingleton<IConfiguration>(configuration);

        services.AddHttpClient();
        services.AddHttpClient("OpenAI", client =>
        {
            var apiKey = configuration["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("OpenAI API key is missing in configuration.");
            }
            client.BaseAddress = new Uri("https://api.openai.com/v1/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        });
        services.AddHttpClient("DeepSeek", client =>
        {
            var apiKey = configuration["DeepSeek:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("DeepSeek API key is missing in configuration.");
            }
            client.BaseAddress = new Uri("https://api.deepseek.com/v1/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        });
        services.AddHttpClient<IDataReader>(client =>
        {
            client.BaseAddress = new Uri("https://www.alphavantage.co/");
        });
        services.AddSingleton(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var apiKey = config["AlphaVantage:ApiKey"];
            return new AlphaVantageService(provider.GetRequiredService<HttpClient>(), apiKey);
        });

        var serviceProvider = services.BuildServiceProvider();
        var host = serviceProvider.GetRequiredService<IWorkflowHost>();
        host.RegisterWorkflow<TradingWorkflow, TradingWorkflowState>();

        var workflowData = new TradingWorkflowState
        {
            InitialCash = initialCash,
            MarginRequirement = marginRequirement,
            Tickers = tickers.ToList(),
            StartDate = startDate,
            EndDate = endDate,
            ShowReasoning = showReasoning,
            SelectedAnalysts = selectedAnalysts.ToList(),
            ModelName = modelName,
            ModelProvider = modelProvider,
            Portfolio = new Portfolio
            {
                Cash = initialCash,
                MarginRequirement = marginRequirement,
                Positions = tickers.ToDictionary(
                    ticker => ticker,
                    ticker => new Position()
                ),
                RealizedGains = tickers.ToDictionary(
                    ticker => ticker,
                    ticker => new RealizedGains()
                )
            }
        };

        host.Start();
        await host.StartWorkflow("TradingWorkflow", workflowData);

        Logger.Info("Workflow Started...");
        Console.ReadLine();

        host.Stop();
        Logger.Info("Workflow Stopped.");
    }

    private static string? ParseArgument(string[] args, string key)
    {
        var index = Array.FindIndex(args, a => a.Equals(key, StringComparison.OrdinalIgnoreCase));
        return (index >= 0 && index + 1 < args.Length) ? args[index + 1] : null;
    }

    private static double ParseDoubleArgument(string[] args, string key, double defaultValue)
    {
        var argValue = ParseArgument(args, key);
        return double.TryParse(argValue, out double result) ? result : defaultValue;
    }
}
