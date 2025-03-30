using ai_hedge_fund_net.Agents;
using ai_hedge_fund_net.Contracts;
using ai_hedge_fund_net.Contracts.Model;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace ai_hedge_fund_net.ConsoleApp.WorkflowSteps;

public class PortfolioManagementStep : StepBody
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        var state = context.Workflow.Data as TradingWorkflowState;
        var chatter = ServiceLocator.Instance.GetRequiredService<IChatter>();
        var agent = new PortfolioManagementAgent(state!, chatter);

        Logger.Info("[Portfolio Manager] Rebalancing portfolio...");

        var output = agent.GeneratePortfolioDecision();
        foreach (var (ticker, decision) in output.Decisions)
        {
            Logger.Info($"{ticker}: {decision.Action.ToUpper()} {decision.Quantity} shares with {decision.Confidence}% confidence.");
            Logger.Info($"Reason: {decision.Reasoning}");
        }

        return ExecutionResult.Next();
    }
}