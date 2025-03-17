using ai_hedge_fund_net.Agents;
using ai_hedge_fund_net.Contracts.Model;
using NLog;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace ai_hedge_fund_net.ConsoleApp.WorkflowSteps
{
    public class BenGrahamStep : StepBody
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public BenGrahamStep(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            // Get TradingAgent from Workflow State
            var workflowState = context.Workflow.Data as TradingWorkflowState;

            string modelProvider = workflowState.ModelProvider;
            var httpClient = _httpClientFactory.CreateClient(modelProvider); // Dynamic provider selection
            var httpService = new HttpService(httpClient);

            var tradingAgent = new BenGraham(workflowState, httpService);

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

            return ExecutionResult.Outcome(tradingAgent.GenerateOutputAsync().Result);
        }

        private void LoadFinancials(TradingWorkflowState tradingWorkflowState)
        {

        }
    }
}