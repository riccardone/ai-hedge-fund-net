using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace ai_hedge_fund_net.ConsoleApp;

public class RiskManagementStep : StepBody
{
    public override ExecutionResult Run(IStepExecutionContext context)
    {
        Console.WriteLine("[Risk Manager] Evaluating risk exposure...");
        return ExecutionResult.Next();
    }
}