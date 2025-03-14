namespace ai_hedge_fund_net.Contracts.Model
{
    public class TradingWorkflowState
    {
        public ITradingAgent TradingAgent { get; set; }  // ✅ Store Agent Here
        public List<FinancialMetrics> FinancialMetrics { get; set; } = new();
        public List<FinancialLineItem> FinancialLineItems { get; set; } = new();
    }
}