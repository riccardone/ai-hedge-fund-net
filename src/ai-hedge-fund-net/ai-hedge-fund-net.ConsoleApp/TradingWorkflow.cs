using ai_hedge_fund_net.Contracts.Model;
using WorkflowCore.Interface;

namespace ai_hedge_fund_net.ConsoleApp
{
    public class TradingWorkflow : IWorkflow<TradingWorkflowState>
    {
        public string Id => "TradingWorkflow";
        public int Version => 1;

        public void Build(IWorkflowBuilder<TradingWorkflowState> builder)
        {
            builder
                .StartWith<BenGrahamAnalysis>()  // No need to pass parameters
                .Then<RiskManagementStep>()
                .Then<PortfolioManagementStep>();
        }
    }
}