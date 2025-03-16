using NLog;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace ai_hedge_fund_net.ConsoleApp;

public class PortfolioManagementStep : StepBody
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        Logger.Info("[Portfolio Manager] Rebalancing portfolio...");
        return ExecutionResult.Next();
    }
}