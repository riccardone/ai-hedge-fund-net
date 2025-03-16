using NLog;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace ai_hedge_fund_net.ConsoleApp;

public class RiskManagementStep : StepBody
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        Logger.Info("[Risk Manager] Evaluating risk exposure...");
        return ExecutionResult.Next();
    }
}