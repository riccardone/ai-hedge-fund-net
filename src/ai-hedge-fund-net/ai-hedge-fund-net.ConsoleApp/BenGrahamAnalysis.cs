using ai_hedge_fund_net.Contracts.Model;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace ai_hedge_fund_net.ConsoleApp
{
    public class BenGrahamAnalysis : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            // Get TradingAgent from Workflow State
            var workflowState = context.Workflow.Data as TradingWorkflowState;
            if (workflowState == null || workflowState.TradingAgent == null)
            {
                Console.WriteLine("ERROR: Missing TradingAgent in Workflow State.");
                return ExecutionResult.Next();
            }

            var tradingAgent = workflowState.TradingAgent;

            Console.WriteLine($"[{tradingAgent.Name}] Analyzing fundamental investment signals...");

            // Mock financial data (Replace with real data)
            var financialMetrics = workflowState.FinancialMetrics;
            var financialLineItems = workflowState.FinancialLineItems;

            // Call Ben Graham's analysis methods
            var earningsStability = tradingAgent.AnalyzeEarningsStability(financialMetrics, financialLineItems);
            var financialStrength = tradingAgent.AnalyzeFinancialStrength(financialMetrics.FirstOrDefault(), financialLineItems);
            var valuation = tradingAgent.AnalyzeValuation(financialMetrics.FirstOrDefault(), financialLineItems, marketCap: 1000000m);

            // Log analysis results
            Console.WriteLine($"Earnings Stability: {string.Join(", ", earningsStability["Details"])}");
            Console.WriteLine($"Financial Strength: {string.Join(", ", financialStrength["Details"])}");
            Console.WriteLine($"Valuation: {string.Join(", ", valuation["Details"])}");

            return ExecutionResult.Next();
        }
    }
}