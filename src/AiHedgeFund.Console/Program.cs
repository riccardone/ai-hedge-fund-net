﻿using System.Net.Http.Headers;
using AiHedgeFund.Agents;
using AiHedgeFund.Agents.Registry;
using AiHedgeFund.Agents.Services;
using AiHedgeFund.Contracts;
using AiHedgeFund.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace AiHedgeFund.Console;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var appArgs = new AppArguments(args);
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton(appArgs);
                services.AddSingleton<IChatter, OpenAiChatter>();
                services.AddSingleton<IAgentRegistry, AgentRegistry>();
                services.AddSingleton<BenGrahamAgent>();
                services.AddSingleton<CathieWoodAgent>();
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
}