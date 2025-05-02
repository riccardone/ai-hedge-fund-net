using AiHedgeFund.Agents.Services;
using AiHedgeFund.Contracts;
using AiHedgeFund.Contracts.Model;
using Microsoft.Extensions.Logging; 

namespace AiHedgeFund.Agents;

/// <summary>
/// Analyzes stocks using Benjamin Graham's classic value-investing principles:
/// 1. Earnings stability over multiple years.
/// 2. Solid financial strength (low debt, adequate liquidity).
/// 3. Discount to intrinsic value (e.g., Graham Number or net-net).
/// 4. Adequate margin of safety.
/// </summary>
public class BenGrahamAgent 
{
    private readonly IHttpLib _httpLib;
    private readonly ILogger<BenGrahamAgent> _logger; 

    public BenGrahamAgent(IHttpLib httpLib, ILogger<BenGrahamAgent> logger)
    {
        _httpLib = httpLib;
        _logger = logger;
    }

    public void Run(TradingWorkflowState state)
    {
        if (!state.Tickers.Any())
        {
            _logger.LogWarning("No ticker provided.");
            return;
        }

        foreach (var ticker in state.Tickers)
        {
            _logger.LogDebug("[BenGraham] Starting analysis for {Ticker}", ticker);

            var earnings = EarningsStability(state, ticker);
            var strength = FinancialStrength(state, ticker);
            var valuation = Valuation(state, ticker);

            var totalScore = earnings.Score + strength.Score + valuation.Score;
            var maxScore = Math.Max(1, earnings.MaxScore + strength.MaxScore + valuation.MaxScore);

            if (TryGenerateOutput(ticker, totalScore, maxScore, earnings, strength, valuation, out var tradeSignal))
                state.AddOrUpdateAgentReport<BenGrahamAgent>(tradeSignal, new[] { earnings, strength, valuation });
            else
                _logger.LogError("Error while running {AgentName}", nameof(BenGrahamAgent));
        }
    }

    private static FinancialAnalysisResult EarningsStability(TradingWorkflowState state, string ticker)
    {
        var result = new FinancialAnalysisResult(nameof(EarningsStability), 0, new List<string>());

        if (!state.FinancialMetrics.TryGetValue(ticker, out var metrics))
        {
            result.AddDetail($"Data not present for {ticker}");
            return result;
        }

        var epsValues = metrics
            .Where(m => m.EarningsPerShare.HasValue)
            .OrderBy(m => m.EndDate)
            .Select(m => m.EarningsPerShare.Value)
            .ToList();

        if (epsValues.Count < 2)
        {
            result.AddDetail("Not enough multi-period EPS data.");
            return result;
        }

        // EPS positivity check
        int positiveYears = epsValues.Count(e => e > 0);
        if (positiveYears == epsValues.Count)
        {
            result.IncreaseScore(3);
            result.AddDetail("EPS was positive in all periods.");
        }
        else if (positiveYears >= epsValues.Count * 0.8)
        {
            result.IncreaseScore(2);
            result.AddDetail("EPS was positive in most periods.");
        }
        else
        {
            result.AddDetail("EPS was negative in multiple periods.");
        }

        // Overall EPS growth check
        var epsGrowth = metrics.ComputeEpsGrowth();

        if (epsGrowth.HasValue)
        {
            if (epsGrowth > 0.10m)
            {
                result.IncreaseScore(1);
                result.AddDetail($"EPS grew consistently over the period: {epsGrowth:P1}");
            }
            else if (epsValues.Last() > epsValues.First())
            {
                result.AddDetail("EPS grew overall but inconsistently.");
            }
            else
            {
                result.AddDetail("EPS did not grow over the full period.");
            }
        }
        else
        {
            result.AddDetail("Not enough EPS data to evaluate long-term growth.");
        }

        return result;
    }

    private FinancialAnalysisResult FinancialStrength(TradingWorkflowState state, string ticker)
    {
        var result = new FinancialAnalysisResult(nameof(FinancialStrength), 0, new List<string>());

        if (!state.FinancialLineItems.TryGetValue(ticker, out var items) || !TryGetLatestCompleteItem(items, out var latest))
        {
            result.AddDetail("No data for financial strength.");
            return result;
        }

        var extras = latest.Extras;

        decimal assets = extras["TotalAssets"];
        decimal liabilities = extras["TotalLiabilities"];
        decimal currentAssets = extras["TotalCurrentAssets"];
        decimal currentLiabilities = extras["TotalCurrentLiabilities"];

        if (currentLiabilities > 0)
        {
            var ratio = currentAssets / currentLiabilities;
            if (ratio >= 2) result.IncreaseScore(2);
            else if (ratio >= 1.5m) result.IncreaseScore(1);

            result.AddDetail($"Current ratio = {ratio:F2}");
        }

        if (assets > 0)
        {
            var debtRatio = liabilities / assets;
            if (debtRatio < 0.5m) result.IncreaseScore(2);
            else if (debtRatio < 0.8m) result.IncreaseScore(1);

            result.AddDetail($"Debt ratio = {debtRatio:F2}");
        }

        return result;
    }

    private FinancialAnalysisResult Valuation(TradingWorkflowState state, string ticker)
    {
        var result = new FinancialAnalysisResult(nameof(Valuation), 0, new List<string>());

        if (!state.FinancialLineItems.TryGetValue(ticker, out var items) ||
            !TryGetLatestCompleteItem(items, out var latest))
        {
            result.AddDetail("No data for valuation.");
            return result;
        }

        if (!state.FinancialMetrics.TryGetValue(ticker, out var metrics) || !metrics.Any())
        {
            result.AddDetail("Missing financial metrics.");
            return result;
        }

        var latestMetrics = metrics.OrderByDescending(m => m.EndDate).First();
        var marketCap = latestMetrics.MarketCap;
        if (marketCap is null or <= 0)
        {
            result.AddDetail("Market Cap missing.");
            return result;
        }

        var extras = latest.Extras;
        if (!extras.TryGetValue("TotalAssets", out var taObj) ||
            !extras.TryGetValue("TotalLiabilities", out var tlObj))
        {
            result.AddDetail("Missing TotalAssets or TotalLiabilities.");
            return result;
        }

        decimal totalAssets = Convert.ToDecimal(taObj);
        decimal totalLiabilities = Convert.ToDecimal(tlObj);
        decimal ncav = totalAssets - totalLiabilities;

        // === Path 1: NCAV deep value check for small/mid caps ===
        if (marketCap < 200_000_000_000) // < $200B
        {
            if (ncav > marketCap)
            {
                result.IncreaseScore(4);
                result.AddDetail("Net-Net: NCAV > Market Cap (classic Graham deep value).");
            }
            else if (ncav >= marketCap * 0.67m)
            {
                result.IncreaseScore(2);
                result.AddDetail("NCAV >= 2/3 of Market Cap (moderate Graham value).");
            }
            else
            {
                result.AddDetail($"NCAV (${ncav:N0}) is less than 2/3 of Market Cap (${marketCap:N0}) — no deep value opportunity.");
            }

            return result;
        }

        // === Path 2: Graham Number check for large caps ===
        var ttmEps = metrics.ComputeTtmEps();
        var bvps = metrics.ComputeBookValuePerShare(); 

        if (ttmEps.HasValue && bvps.HasValue && ttmEps > 0 && bvps > 0)
        {
            _logger.LogDebug("TTM EPS: {eps}, BVPS: {bvps}", ttmEps, bvps);

            var grahamNumber = Convert.ToDecimal(Math.Sqrt(22.5 * (double)ttmEps.Value * (double)bvps.Value));

            if (state.Prices.TryGetValue(ticker, out var prices) && prices.Any())
            {
                var price = prices.Last().Close;

                if (price <= grahamNumber)
                {
                    result.IncreaseScore(3);
                    result.AddDetail($"Price (${price:F2}) is below Graham Number (${grahamNumber:F2}) — valuation is attractive.");
                }
                else
                {
                    result.IncreaseScore(1);
                    result.AddDetail($"Price (${price:F2}) exceeds Graham Number (${grahamNumber:F2}) — limited margin of safety.");
                }
            }
            else
            {
                result.AddDetail("No price data available for Graham Number comparison.");
            }
        }
        else
        {
            result.AddDetail("Missing or invalid TTM EPS or Book Value Per Share for Graham Number calculation.");
        }


        return result;
    }

    private static bool TryGetLatestCompleteItem(IEnumerable<FinancialLineItem> items, out FinancialLineItem? result)
    {
        foreach (var item in items.Reverse())
        {
            var x = item.Extras;
            if (x.ContainsKey("TotalAssets") && x.ContainsKey("TotalLiabilities"))
            {
                result = item;
                return true;
            }
        }

        result = null;
        return false;
    }

    private bool TryGenerateOutput(string ticker, int totalScore, int maxScore, FinancialAnalysisResult earnings,
        FinancialAnalysisResult strength, FinancialAnalysisResult valuation, out TradeSignal tradeSignal)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            tradeSignal = new TradeSignal(ticker, "neutral", 0, "No ticker provided.");
            return false;
        }

        var systemMessage = @"You are a Benjamin Graham AI agent, making investment decisions using his principles:
            1. Insist on a margin of safety by buying below intrinsic value (e.g., using Graham Number, net-net).
            2. Emphasize the company's financial strength (low leverage, ample current assets).
            3. Prefer stable earnings over multiple years.
            4. Consider dividend record for extra safety.
            5. Avoid speculative or high-growth assumptions; focus on proven metrics.

            Return a rational recommendation: bullish, bearish, or neutral, with a confidence level (0-100) and concise reasoning.";

        var analysisData = new
        {
            Score = totalScore,
            Maxscore = maxScore,
            Earnings = earnings,
            Strength = strength,
            Valuation = valuation
        };

        return LlmTradeSignalGenerator.TryGenerateSignal(
            _httpLib,
            "chat/completions",
            ticker,
            systemMessage,
            analysisData,
            agentName: "Ben Graham",
            out tradeSignal
        );
    }
}
