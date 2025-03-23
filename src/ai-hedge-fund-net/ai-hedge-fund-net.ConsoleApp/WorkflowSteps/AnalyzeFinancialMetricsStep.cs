using ai_hedge_fund_net.Contracts.Model;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace ai_hedge_fund_net.ConsoleApp.WorkflowSteps;

public class AnalyzeFinancialMetricsStep : StepBody
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<string, StepBody> _analystAgents;

    public AnalyzeFinancialMetricsStep(IServiceProvider serviceProvider)
    {
        _analystAgents = new Dictionary<string, StepBody>
        {
            { "BenGraham", serviceProvider.GetRequiredService<BenGrahamStep>() },
            { "WarrenBuffett", serviceProvider.GetRequiredService<WarrenBuffettStep>() },
            { "BillAckman", serviceProvider.GetRequiredService<BillAckmanStep>() },
            { "CathieWood", serviceProvider.GetRequiredService<CathieWoodStep>() },
            { "CharlieMunger", serviceProvider.GetRequiredService<CharlieMungerStep>() }
        };
    }

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        var state = context.Workflow.Data as TradingWorkflowState;
        if (state == null) return ExecutionResult.Next();

        Logger.Info("Running Financial Analysis for Selected Analysts...");

        foreach (var analyst in state.SelectedAnalysts)
        {
            if (!_analystAgents.TryGetValue(analyst, out var agent)) 
                continue;
            var analysisResult = agent.Run(context);

            if (analysisResult.OutcomeValue is TradeSignal tradeSignal)
            {
                Logger.Info($"{analyst} analysis completed with signal: {tradeSignal.Signal} {tradeSignal.Reasoning}");
                //state.FinancialMetrics.Add(analyst, new FinancialMetrics { Analysis = tradeSignal });
                state.AnalystSignals.Add(analyst, tradeSignal);
            }
            else
            {
                Logger.Error($"{analyst} returned null or invalid TradeSignal!");
            }
        }

        return ExecutionResult.Next();
    }
}