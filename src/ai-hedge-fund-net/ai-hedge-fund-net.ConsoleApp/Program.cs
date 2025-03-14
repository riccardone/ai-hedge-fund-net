using ai_hedge_fund_net.Agents;
using ai_hedge_fund_net.Contracts;
using ai_hedge_fund_net.Contracts.Model;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;

namespace ai_hedge_fund_net.ConsoleApp;

class Program
{
    static void Main()
    {
        Console.WriteLine("Running Trading Workflow...");

        var services = new ServiceCollection()
            .AddLogging()
            .AddWorkflow()
            .AddSingleton<ITradingAgent, BenGraham>()  // ✅ Register BenGraham in DI
            .BuildServiceProvider();

        var host = services.GetRequiredService<IWorkflowHost>();

        // ✅ Register workflow
        host.RegisterWorkflow<TradingWorkflow, TradingWorkflowState>();

        // ✅ Inject dependencies into workflow state
        var workflowData = new TradingWorkflowState
        {
            TradingAgent = services.GetRequiredService<ITradingAgent>()  // ✅ Assign TradingAgent
        };

        host.Start();
        host.StartWorkflow("TradingWorkflow", workflowData);

        Console.WriteLine("Workflow Started...");
        Console.ReadLine();
        host.Stop();
    }
}