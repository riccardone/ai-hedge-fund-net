using ai_hedge_fund_net.Agents;
using ai_hedge_fund_net.Contracts;
using ai_hedge_fund_net.Contracts.Model;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace ai_hedge_fund_net.ConsoleApp.WorkflowSteps;

public class BenGrahamStep : StepBody
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        // Get TradingAgent from Workflow State
        var workflowState = context.Workflow.Data as TradingWorkflowState;

        //string modelProvider = workflowState.ModelProvider;
        //var httpClientFactory = ServiceLocator.Instance.GetRequiredService<IHttpClientFactory>();
        //var httpClient = httpClientFactory.CreateClient(modelProvider); // Dynamic provider selection

        var chatter = ServiceLocator.Instance.GetRequiredService<IChatter>();
        var tradingAgent = new BenGraham(workflowState, chatter);

        Logger.Info($"[{tradingAgent.Name}] Analyzing fundamental investment signals...");

        // Call Ben Graham's analysis methods
        var earningsStability = tradingAgent.AnalyzeEarningsStability();
        var financialStrength = tradingAgent.AnalyzeFinancialStrength();
        var valuation = tradingAgent.AnalyzeValuation();

        // Log analysis results
        foreach (var ticker in workflowState.Tickers)
        {
            Logger.Info($"Earnings Stability: {string.Join(", ", earningsStability[ticker]["Details"])}");
            if (financialStrength.TryGetValue(ticker, out var value))
                Logger.Info($"Financial Strength: {string.Join(", ", value.Details)}");
            Logger.Info($"Valuation: {string.Join(", ", valuation["Details"])}");
        }

        return ExecutionResult.Outcome(tradingAgent.GenerateOutput());
    }
}