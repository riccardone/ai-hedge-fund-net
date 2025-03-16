using ai_hedge_fund_net.Agents;
using ai_hedge_fund_net.Contracts.Model;
using NLog;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace ai_hedge_fund_net.ConsoleApp.WorkflowSteps
{
    public class CathieWoodStep : StepBody
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            // Get TradingAgent from Workflow State
            var workflowState = context.Workflow.Data as TradingWorkflowState;
            if (workflowState == null || workflowState.TradingAgent == null)
            {
                Logger.Error("ERROR: Missing TradingAgent in Workflow State.");
                return ExecutionResult.Next();
            }

            var tradingAgent = new CathieWood();

            Logger.Info($"[{tradingAgent.Name}] Analyzing fundamental investment signals...");

            // Mock financial data (Replace with real data)
            var financialMetrics = workflowState.FinancialMetrics;
            var financialLineItems = workflowState.FinancialLineItems;

            // Call Ben Graham's analysis methods
            var earningsStability = tradingAgent.AnalyzeEarningsStability(financialMetrics, financialLineItems);
            var financialStrength = tradingAgent.AnalyzeFinancialStrength(financialMetrics.FirstOrDefault(), financialLineItems);
            var valuation = tradingAgent.AnalyzeValuation(financialMetrics.FirstOrDefault(), financialLineItems, marketCap: 1000000m);

            // Log analysis results
            Logger.Info($"Earnings Stability: {string.Join(", ", earningsStability["Details"])}");
            Logger.Info($"Financial Strength: {string.Join(", ", financialStrength["Details"])}");
            Logger.Info($"Valuation: {string.Join(", ", valuation["Details"])}");

            ExecutionResult.Outcome(tradingAgent.GenerateOutput());

            return ExecutionResult.Next();
        }

        private void LoadFinancials(TradingWorkflowState tradingWorkflowState)
        {

        }
    }
}