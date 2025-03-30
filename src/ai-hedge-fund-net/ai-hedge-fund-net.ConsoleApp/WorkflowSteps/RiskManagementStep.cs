using ai_hedge_fund_net.Agents;
using ai_hedge_fund_net.Contracts;
using ai_hedge_fund_net.Contracts.Model;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace ai_hedge_fund_net.ConsoleApp.WorkflowSteps;

public class RiskManagementStep : StepBody
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        var workflowState = context.Workflow.Data as TradingWorkflowState;

        var dataReader = ServiceLocator.Instance.GetRequiredService<IDataReader>();
        var agent = new RiskManagementAgent(workflowState, dataReader);

        Logger.Info("[RiskManagementStep] Starting risk analysis...");
        var signals = agent.Analyze(); //.AnalyzeRisk();

        workflowState.AnalystSignals["risk_management_agent"] = signals.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value); 

        foreach (var (ticker, signal) in signals)
        {
            Logger.Info($"[{ticker}] {signal.Reasoning}");
        }

        return ExecutionResult.Next();
    }
}