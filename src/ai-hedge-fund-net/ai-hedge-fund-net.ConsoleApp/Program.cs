using ai_hedge_fund_net.Agents;
using ai_hedge_fund_net.Contracts;
using ai_hedge_fund_net.Contracts.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using WorkflowCore.Interface;

namespace ai_hedge_fund_net.ConsoleApp;

public class Program
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static async Task Main(string[] args)
    {
        // Parse command-line arguments
        var selectedAgent = ParseArgument(args, "--agent") ?? "BenGraham"; // Default to BenGraham
        var selectedTickers = ParseArgument(args, "--tickers")?.Split(',') ?? new string[] { };
        var selectedLLM = ParseArgument(args, "--llm") ?? "openai"; // Default to OpenAI

        Logger.Info($"Selected Agent: {selectedAgent}");
        Logger.Info($"Selected Tickers: {string.Join(", ", selectedTickers)}");
        Logger.Info($"Selected LLM: {selectedLLM}");

        var services = new ServiceCollection()
            .AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddNLog();
                loggingBuilder.AddConsole();
                loggingBuilder.AddFilter("Microsoft.*", Microsoft.Extensions.Logging.LogLevel.Error);
                loggingBuilder.AddFilter("System.Net.Http.*", Microsoft.Extensions.Logging.LogLevel.Error);
#if DEBUG
                var configPath = "nlog-dev.config";
#else
                var configPath = "nlog.config";
#endif
                LogManager.Setup().SetupExtensions(e => e.AutoLoadAssemblies(false))
                    .LoadConfigurationFromFile(configPath, optional: true);
            })
            .AddWorkflow()
            .AddSingleton<ITradingAgent>(sp => CreateTradingAgent(selectedAgent))
            .BuildServiceProvider();

        var host = services.GetRequiredService<IWorkflowHost>();
        host.RegisterWorkflow<TradingWorkflow, TradingWorkflowState>();

        var workflowData = new TradingWorkflowState
        {
            TradingAgent = services.GetRequiredService<ITradingAgent>()
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

    private static ITradingAgent CreateTradingAgent(string agentName)
    {
        return agentName.ToLower() switch
        {
            "bengraham" => new BenGraham(),
            "warrenbuffett" => new WarrenBuffett(),
            "billackman" => new BillAckman(),
            "cathiewood" => new CathieWood(),
            "charliemunger" => new CharlieMunger(),
            _ => throw new ArgumentException($"Unknown trading agent: {agentName}")
        };
    }
}
