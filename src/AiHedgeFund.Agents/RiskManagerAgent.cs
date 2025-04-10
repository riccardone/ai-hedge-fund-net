using AiHedgeFund.Contracts;
using NLog;

namespace AiHedgeFund.Agents;

public class RiskManagerAgent
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public Dictionary<string, RiskAssessment> Run(TradingWorkflowState state)
    {
        var results = new Dictionary<string, RiskAssessment>();
        var portfolio = state.Portfolio;

        foreach (var ticker in state.Tickers)
        {
            if (!state.Prices.TryGetValue(ticker, out var prices) || !prices.Any())
            {
                Logger.Warn($"No price data found for {ticker}");
                continue;
            }

            var currentPrice = prices.Last().Close;

            decimal costBasis = portfolio.Positions.TryGetValue(ticker, out var pos)
                ? pos.Long * pos.LongCostBasis
                : 0m;

            decimal totalPortfolioValue = portfolio.Cash +
                portfolio.Positions.Sum(p => p.Value.Long * p.Value.LongCostBasis);

            var positionLimit = totalPortfolioValue * 0.20m; // 20% max per ticker
            var remainingLimit = positionLimit - costBasis;
            var maxPositionSize = Math.Min(remainingLimit, portfolio.Cash);

            var assessment = new RiskAssessment
            {
                RemainingPositionLimit = maxPositionSize,
                CurrentPrice = currentPrice,
                Reasoning = new RiskReasoning
                {
                    PortfolioValue = totalPortfolioValue,
                    CurrentPosition = costBasis,
                    PositionLimit = positionLimit,
                    RemainingLimit = remainingLimit,
                    AvailableCash = portfolio.Cash
                }
            };

            results[ticker] = assessment;

            //if (state.ShowReasoning)
            //{
            //    Logger.Info($"[Risk] {ticker}: pos={costBasis}, limit={positionLimit}, remaining={remainingLimit}, cash={portfolio.Cash}");
            //}
        }

        return results;
    }
}