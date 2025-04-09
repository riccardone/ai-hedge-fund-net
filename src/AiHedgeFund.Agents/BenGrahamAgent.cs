using AiHedgeFund.Agents.Services;
using AiHedgeFund.Contracts;
using AiHedgeFund.Contracts.Model;
using NLog;

namespace AiHedgeFund.Agents;

/// <summary>
/// Analyzes stocks using Benjamin Graham's classic value-investing principles:
/// 1. Earnings stability over multiple years.
/// 2. Solid financial strength(low debt, adequate liquidity).
/// 3. Discount to intrinsic value(e.g.Graham Number or net-net).
/// 4. Adequate margin of safety.
/// </summary>
public class BenGrahamAgent
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly IHttpLib _httpLib;

    public BenGrahamAgent(IHttpLib httpLib)
    {
        _httpLib = httpLib;
    }

    public IEnumerable<TradeSignal> Run(TradingWorkflowState state)
    {
        var signals = new List<TradeSignal>();
        if (!state.Tickers.Any())
        {
            Logger.Warn("No ticker provided.");
            return signals;
        }
        
        foreach (var ticker in state.Tickers)
        {
            Logger.Info("[BenGraham] Starting analysis for {0}", ticker);

            var earnings = AnalyzeEarningsStability(state, ticker);
            var strength = AnalyzeFinancialStrength(state, ticker);
            var valuation = AnalyzeValuation(state, ticker);

            Logger.Info("{0} Earnings Stability: {1}", ticker, string.Join("; ", earnings.Details));
            Logger.Info("{0} Financial Strength: {1}", ticker, string.Join("; ", strength.Details));
            Logger.Info("{0} Valuation: {1}", ticker, string.Join("; ", valuation.Details));

            if (TryGenerateOutput(state, ticker, out TradeSignal tradeSignal))
                signals.Add(tradeSignal);
            else
                Logger.Error($"Error while running {nameof(BenGrahamAgent)}");
        }

        return signals;
    }

    private static FinancialAnalysisResult AnalyzeEarningsStability(TradingWorkflowState state, string ticker)
    {
        var result = new FinancialAnalysisResult();
        result.SetScore(0);

        if (!state.FinancialMetrics.TryGetValue(ticker, out var metrics))
        {
            result.AddDetail($"Data not present for {ticker}");
            return result;
        }

        var epsValues = metrics
            .Where(m => m.EarningsPerShare.HasValue)
            .Select(m => m.EarningsPerShare.Value)
            .ToList();

        if (epsValues.Count < 2)
        {
            result.AddDetail("Not enough multi-period EPS data.");
            return result;
        }

        // Determine threshold from risk level
        double growthThreshold = state.RiskLevel?.ToLower() switch
        {
            "low" => 0.80,
            "medium" => 0.70,
            "high" => 0.60,
            _ => 0.70 // default to medium
        };

        // EPS positivity
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

        // EPS trend analysis
        int growthPeriods = epsValues
            .Zip(epsValues.Skip(1), (prev, next) => next > prev ? 1 : 0)
            .Sum();

        double growthRatio = (double)growthPeriods / (epsValues.Count - 1);

        if (growthRatio >= growthThreshold)
        {
            result.IncreaseScore(1);
            result.AddDetail("EPS showed a generally increasing trend.");
        }
        else if (epsValues.Last() > epsValues.First())
        {
            result.AddDetail("EPS grew overall but with fluctuations.");
        }
        else
        {
            result.AddDetail("EPS did not grow.");
        }

        // EPS growth rate trend
        var growthRates = metrics
            .Where(m => m.EarningsPerShareGrowth.HasValue)
            .Select(m => m.EarningsPerShareGrowth.Value)
            .ToList();

        if (growthRates.Count >= 2)
        {
            int positiveGrowths = growthRates.Count(g => g > 0);
            double growthRateRatio = (double)positiveGrowths / growthRates.Count;

            if (growthRateRatio >= growthThreshold)
            {
                result.IncreaseScore(1);
                result.AddDetail("EPS growth rate was positive in most periods.");
            }
            else
            {
                result.AddDetail("EPS growth rate was inconsistent.");
            }
        }

        return result;
    }

    private FinancialAnalysisResult AnalyzeFinancialStrength(TradingWorkflowState state, string ticker)
    {
        var result = new FinancialAnalysisResult();
        result.SetScore(0);

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

    private FinancialAnalysisResult AnalyzeValuation(TradingWorkflowState state, string ticker)
    {
        var result = new FinancialAnalysisResult();
        result.SetScore(0);

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

        var marketCap = metrics.Last().MarketCap;
        if (marketCap <= 0)
        {
            result.AddDetail("Market Cap missing.");
            return result;
        }

        if (!latest.Extras.TryGetValue("TotalAssets", out var taObj) ||
            !latest.Extras.TryGetValue("TotalLiabilities", out var tlObj))
        {
            result.AddDetail("Missing TotalAssets or TotalLiabilities.");
            return result;
        }

        decimal totalAssets = Convert.ToDecimal(taObj);
        decimal totalLiabilities = Convert.ToDecimal(tlObj);
        decimal ncav = totalAssets - totalLiabilities;

        if (ncav > marketCap)
        {
            result.IncreaseScore(4);
            result.AddDetail("Net-Net: NCAV > Market Cap (classic Graham deep value).");
        }
        else if (ncav >= marketCap * 0.67m)
        {
            result.IncreaseScore(2);
            result.AddDetail("NCAV Per Share >= 2/3 of Price Per Share (moderate net-net discount).");
        }
        else
        {
            result.AddDetail($"NCAV (${ncav:N0}) is less than 2/3 of Market Cap (${marketCap:N0}) — no deep value opportunity.");
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

    private bool TryGenerateOutput(TradingWorkflowState state, string ticker, out TradeSignal tradeSignal)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            tradeSignal = new TradeSignal(ticker, "neutral", 0, "No ticker provided.");
            return false;
        }

        if (!state.FinancialMetrics.Any())
        {
            tradeSignal = new TradeSignal(ticker, "neutral", 0, "No metrics provided.");
            return false;
        }

        if (!state.FinancialLineItems.Any())
        {
            tradeSignal = new TradeSignal(ticker, "neutral", 0, "No financial data provided.");
            return false;
        }

        var analysisData = new Dictionary<string, object>
        {
            { "FinancialMetrics", state.FinancialMetrics },
            { "FinancialLineItems", state.FinancialLineItems },
            { "StartDate", state.StartDate },
            { "EndDate", state.EndDate },
            { "InitialCash", state.Portfolio.Cash },
            { "MarginRequirement", state.MarginRequirement },
            { "Portfolio", state.Portfolio }
        };

        var systemMessage = @"You are a Benjamin Graham AI agent, making investment decisions using his principles:
            1. Insist on a margin of safety by buying below intrinsic value (e.g., using Graham Number, net-net).
            2. Emphasize the company's financial strength (low leverage, ample current assets).
            3. Prefer stable earnings over multiple years.
            4. Consider dividend record for extra safety.
            5. Avoid speculative or high-growth assumptions; focus on proven metrics.

            Return a rational recommendation: bullish, bearish, or neutral, with a confidence level (0-100) and concise reasoning.";

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
