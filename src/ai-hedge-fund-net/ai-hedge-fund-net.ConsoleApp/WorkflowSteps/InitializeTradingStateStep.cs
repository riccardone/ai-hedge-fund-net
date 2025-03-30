using ai_hedge_fund_net.Contracts;
using ai_hedge_fund_net.Contracts.Model;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace ai_hedge_fund_net.ConsoleApp.WorkflowSteps;

public class InitializeTradingStateStep : StepBody
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        var state = context.Workflow.Data as TradingWorkflowState;
        if (state == null) return ExecutionResult.Next();

        // Default tickers if none provided
        if (state.Tickers == null || !state.Tickers.Any())
        {
            state.Tickers = new List<string> { "MSFT", "AAPL" };
        }

        // Default end date to today
        if (string.IsNullOrEmpty(state.EndDate))
        {
            state.EndDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        }

        // Default start date to 3 months before end date
        if (string.IsNullOrEmpty(state.StartDate))
        {
            DateTime endDateParsed = DateTime.Parse(state.EndDate);
            state.StartDate = endDateParsed.AddMonths(-3).ToString("yyyy-MM-dd");
        }

        // Initialize portfolio
        state.Portfolio = new Portfolio
        {
            Cash = state.InitialCash,
            MarginRequirement = state.MarginRequirement,
            Positions = state.Tickers.ToDictionary(
                ticker => ticker,
                ticker => new Position()
            ),
            RealizedGains = state.Tickers.ToDictionary(
                ticker => ticker,
                ticker => new RealizedGains()
            )
        };

        // Initialize empty analyst signals
        state.AnalystSignals = new Dictionary<string, IDictionary<string, object>>();

        // Initialize empty trade decisions
        state.TradeDecisions = new Dictionary<string, TradeDecision>();

        var dataReader = ServiceLocator.Instance.GetRequiredService<IDataReader>();

        foreach (var ticker in state.Tickers)
        {
            if (dataReader.TryGetFinancialMetricsAsync(ticker, DateTime.Today, "ttm", 10, out var metrics))
                state.FinancialMetrics.Add(ticker, metrics);
            if (dataReader.TryGetFinancialLineItemsAsync(ticker, DateTime.Today, "ttm", 10, out var financialLineItems))
                state.FinancialLineItems.Add(ticker, financialLineItems);
        }

        if (!state.FinancialLineItems.Any() || !state.FinancialMetrics.Any())
            throw new Exception(
                "While initializing the Trading Workflow I haven't found any financial data or metrics. Please make sure you have set the config settings to retrieve the data");

        Logger.Info("Trading workflow state initialized:");
        Logger.Info($"Start Date: {state.StartDate}, End Date: {state.EndDate}");
        Logger.Info($"Tickers: {string.Join(", ", state.Tickers)}");
        Logger.Info($"Initial Cash: {state.InitialCash}, Margin Requirement: {state.MarginRequirement}");

        return ExecutionResult.Next();
    }
}