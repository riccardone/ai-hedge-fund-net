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

        var earningsStability = new Dictionary<string, FinancialAnalysisResult>();
        var financialStrength = new Dictionary<string, FinancialAnalysisResult>();
        var valuation = new Dictionary<string, FinancialAnalysisResult>();

        foreach (var ticker in workflowState.Tickers)
        {
            earningsStability.Add(ticker, tradingAgent.AnalyzeEarningsStability(ticker));
            financialStrength.Add(ticker, tradingAgent.AnalyzeFinancialStrength(ticker));
            valuation.Add(ticker, tradingAgent.AnalyzeValuation(ticker));
        }

        // Log analysis results
        foreach (var ticker in workflowState.Tickers)
        {
            Logger.Info($"{ticker} Earnings Stability: Score {earningsStability[ticker].Score} {string.Join(", ", earningsStability[ticker].Details)}");
            if (financialStrength.TryGetValue(ticker, out var value))
                Logger.Info($"{ticker} Financial Strength: Score {value.Score}, {string.Join(", ", value.Details)}");
            Logger.Info($"{ticker} Valuation: Score {valuation[ticker].Score} {string.Join(", ", valuation[ticker].Details)}");
        }

        var tradeSignals = new List<TradeSignal>();
        foreach (var ticker in workflowState.Tickers)
        {
            tradeSignals.Add(tradingAgent.GenerateOutput(ticker));
        }
        return ExecutionResult.Outcome(tradeSignals);
    }
}