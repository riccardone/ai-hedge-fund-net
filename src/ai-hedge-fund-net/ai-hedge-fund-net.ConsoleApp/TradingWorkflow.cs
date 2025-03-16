using ai_hedge_fund_net.ConsoleApp.WorkflowSteps;
using ai_hedge_fund_net.Contracts.Model;
using WorkflowCore.Interface;

namespace ai_hedge_fund_net.ConsoleApp;

public class TradingWorkflow : IWorkflow<TradingWorkflowState>
{
    public string Id => "TradingWorkflow";
    public int Version => 1;

    public void Build(IWorkflowBuilder<TradingWorkflowState> builder)
    {
        builder
            .StartWith<InitializeTradingStateStep>() 
            .Then<AnalyzeFinancialMetricsStep>() 
            .Then<RiskManagementStep>() 
            .Then<PortfolioManagementStep>() 
            .EndWorkflow();
    }
}