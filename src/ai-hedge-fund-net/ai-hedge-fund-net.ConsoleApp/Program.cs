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
            .AddSingleton<ITradingAgent, BenGraham>() // Register BenGraham in DI
            .BuildServiceProvider();

        var host = services.GetRequiredService<IWorkflowHost>();
        host.RegisterWorkflow<TradingWorkflow, TradingWorkflowState>();
        
        // Inject dependencies into workflow state
        var workflowData = new TradingWorkflowState
        {
            TradingAgent = services.GetRequiredService<ITradingAgent>() // Assign TradingAgent
        };

        host.Start();
        await host.StartWorkflow("TradingWorkflow", workflowData);

        Logger.Info("Workflow Started...");
        Console.ReadLine();

        host.Stop();
        Logger.Info("Workflow Stopped.");
    }
}