using ai_hedge_fund_net.Contracts;
using ai_hedge_fund_net.Contracts.Model;

namespace ai_hedge_fund_net.Agents;

public class RiskManagementAgent : ITradingAgent
{
    private readonly TradingWorkflowState _state;
    private readonly IDataReader _dataReader;

    public RiskManagementAgent(TradingWorkflowState state, IDataReader dataReader)
    {
        _state = state;
        _dataReader = dataReader;
    }

    public Dictionary<string, RiskManagementOutput> Analyze()
    {
        var result = new Dictionary<string, RiskManagementOutput>();
        var portfolio = _state.Portfolio;
        var startDate = DateTime.Parse(_state.StartDate);
        var endDate = DateTime.Parse(_state.EndDate);

        foreach (var ticker in _state.Tickers)
        {
            //var prices = _state.PriceData.TryGetValue(ticker, out var priceList) ? priceList : null;
            var prices = _dataReader.GetPrices(ticker, startDate, endDate).ToList();
            if (prices == null || prices.Count == 0)
                continue;

            var currentPrice = prices.Last().Close;
            var currentPositionValue = portfolio.Positions.TryGetValue(ticker, out var pos)
                ? pos.LongCostBasis
                : 0m;

            var totalPortfolioValue = (decimal)portfolio.Cash + portfolio.Positions.Values.Sum(p => p.LongCostBasis + p.ShortCostBasis);
            var positionLimit = totalPortfolioValue * 0.20m;
            var remainingLimit = positionLimit - currentPositionValue;
            var maxSize = Math.Min(remainingLimit, (decimal)portfolio.Cash);

            result[ticker] = new RiskManagementOutput
            {
                CurrentPrice = currentPrice,
                RemainingPositionLimit = maxSize,
                Reasoning = new RiskReasoning
                {
                    PortfolioValue = totalPortfolioValue,
                    CurrentPosition = currentPositionValue,
                    PositionLimit = positionLimit,
                    RemainingLimit = remainingLimit,
                    AvailableCash = (decimal)portfolio.Cash
                }
            };
        }

        return result;
    }

    public string Name => nameof(RiskManagementAgent);

    public FinancialAnalysisResult AnalyzeEarningsStability(string ticker) => throw new NotImplementedException();
    public FinancialAnalysisResult AnalyzeFinancialStrength(string ticker) => throw new NotImplementedException();
    public FinancialAnalysisResult AnalyzeValuation(string ticker) => throw new NotImplementedException();
    public TradeSignal GenerateOutput(string ticker) => throw new NotImplementedException();
}


//using ai_hedge_fund_net.Contracts;
//using ai_hedge_fund_net.Contracts.Model;
//using NLog;

//namespace ai_hedge_fund_net.Agents;

//public class RiskManagementAgent : ITradingAgent
//{
//    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
//    private readonly TradingWorkflowState _state;
//    private readonly IDataReader _dataReader;

//    public string Name => "risk_management_agent";

//    public RiskManagementAgent(TradingWorkflowState state, IDataReader dataReader)
//    {
//        _state = state;
//        _dataReader = dataReader;
//    }

//    public Dictionary<string, TradeSignal> AnalyzeRisk()
//    {
//        var portfolio = _state.Portfolio;
//        var tickers = _state.Tickers;
//        var startDate = DateTime.Parse(_state.StartDate);
//        var endDate = DateTime.Parse(_state.EndDate);

//        var riskAnalysis = new Dictionary<string, TradeSignal>();

//        foreach (var ticker in tickers)
//        {
//            Logger.Info($"[RiskManagementAgent] Fetching prices for {ticker}...");

//            var prices = _dataReader.GetPrices(ticker, startDate, endDate).ToList();
//            if (prices.Count == 0)
//            {
//                Logger.Warn($"No price data found for {ticker}");
//                continue;
//            }

//            var currentPrice = prices.Last().Close;

//            // Get position details
//            var position = portfolio.Positions.TryGetValue(ticker, out var p) ? p : new Position();

//            double positionValue = position.LongCostBasis + position.ShortCostBasis;
//            double totalPortfolioValue = portfolio.Cash + portfolio.Positions.Values
//                .Sum(pos => pos.LongCostBasis + pos.ShortCostBasis);

//            double positionLimit = totalPortfolioValue * 0.20;
//            double remainingLimit = positionLimit - positionValue;
//            double maxPositionSize = Math.Min(remainingLimit, portfolio.Cash);

//            var reasoningLines = new List<string>
//            {
//                $"Portfolio value: {totalPortfolioValue:C}",
//                $"Long position: {position.Long} shares @ cost {position.LongCostBasis:C}",
//                $"Short position: {position.Short} shares @ cost {position.ShortCostBasis:C}",
//                $"Position value: {positionValue:C}",
//                $"20% position limit: {positionLimit:C}",
//                $"Remaining limit: {remainingLimit:C}",
//                $"Cash available: {portfolio.Cash:C}"
//            };

//            var reasoning = string.Join(". ", reasoningLines);

//            var signal = new TradeSignal(
//                ticker: ticker,
//                signal: "neutral",
//                confidence: 0,
//                reasoning: $"Max position size: {maxPositionSize:C}. Current price: {currentPrice:C}. {reasoning}."
//            );

//            riskAnalysis[ticker] = signal;
//        }

//        return riskAnalysis;
//    }

//    public FinancialAnalysisResult AnalyzeEarningsStability(string ticker) => throw new NotImplementedException();
//    public FinancialAnalysisResult AnalyzeFinancialStrength(string ticker) => throw new NotImplementedException();
//    public FinancialAnalysisResult AnalyzeValuation(string ticker) => throw new NotImplementedException();
//    public TradeSignal GenerateOutput(string ticker) => throw new NotImplementedException();
//}
