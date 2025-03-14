using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace ai_hedge_fund_net.ConsoleApp;

public class PortfolioManagementStep : StepBody
{
    public override ExecutionResult Run(IStepExecutionContext context)
    {
        Console.WriteLine("[Portfolio Manager] Rebalancing portfolio...");
        return ExecutionResult.Next();
    }
}