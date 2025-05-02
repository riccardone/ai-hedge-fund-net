using AiHedgeFund.Contracts;
using AiHedgeFund.Contracts.Model;
using AiHedgeFund.Agents.Services;
using Microsoft.Extensions.Logging;

namespace AiHedgeFund.Agents;

/// <summary>
/// Analyzes stocks using Cathie Wood's investing principles and LLM reasoning.
/// 1. Prioritizes companies with breakthrough technologies or business models
/// 2. Focuses on industries with rapid adoption curves and massive TAM(Total Addressable Market).
/// 3. Invests mostly in AI, robotics, genomic sequencing, fintech, and blockchain.
/// 4. Willing to endure short-term volatility for long-term gains.
/// </summary>
public class CathieWoodAgent 
{
    private readonly ILogger<CathieWoodAgent> _logger;
    private readonly IHttpLib _httpLib;

    public CathieWoodAgent(IHttpLib chatter, ILogger<CathieWoodAgent> logger)
    {
        _httpLib = chatter;
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
            _logger.LogDebug($"[CathieWood] Starting analysis for {ticker}");

            if (!state.FinancialMetrics.TryGetValue(ticker, out var metrics)
                || !state.FinancialLineItems.TryGetValue(ticker, out var lineItems))
            {
                _logger.LogWarning($"Missing data for {ticker}");
                continue;
            }

            var marketCap = metrics.MaxBy(m => m.Period)?.MarketCap;
            if (marketCap == null)
            {
                _logger.LogWarning($"No market cap for {ticker}");
                continue;
            }

            var disruptive = DisruptivePotential(metrics, lineItems);
            var innovation = InnovationGrowth(metrics, lineItems);
            var valuation = Valuation(metrics, marketCap.Value);

            var totalScore = disruptive.Score + innovation.Score + valuation.Score;
            var maxScore = Math.Max(1, disruptive.MaxScore + innovation.MaxScore + valuation.MaxScore);

            if (TryGenerateOutput(state, ticker, disruptive, innovation, valuation, totalScore, maxScore,
                    out var tradeSignal))
                state.AddOrUpdateAgentReport<CathieWoodAgent>(tradeSignal, new[] { disruptive, innovation, valuation });
            else
                _logger.LogError($"Error while running {nameof(CathieWoodAgent)}");
        }
    }

    private FinancialAnalysisResult DisruptivePotential(IEnumerable<FinancialMetrics> metrics, IEnumerable<FinancialLineItem> lineItems)
    {
        var score = 0;
        var details = new List<string>();
        var ordered = metrics.OrderBy(m => m.EndDate).ToList();

        // 1. Revenue Growth Trend (from FinancialMetrics)
        var revenueGrowths = ordered.Select(m => m.RevenueGrowth).Where(x => x.HasValue).Select(x => x.Value).ToList();
        if (revenueGrowths.Count >= 2)
        {
            var growthTrend = revenueGrowths[^1] - revenueGrowths[0];
            if (growthTrend > 0) { score += 2; details.Add($"Revenue growth trend: {growthTrend:P1}"); }

            var latest = revenueGrowths[^1];
            if (latest > 0.4m) { score += 3; details.Add($"Exceptional revenue growth: {latest:P1}"); }
            else if (latest > 0.25m) { score += 2; details.Add($"Strong revenue growth: {latest:P1}"); }
            else if (latest > 0.10m) { score += 1; details.Add($"Moderate revenue growth: {latest:P1}"); }
        }
        else
        {
            details.Add("Insufficient revenue growth data");
        }

        // 2. Gross Margin Trend
        var margins = ordered.Select(m => m.GrossMargin).Where(x => x.HasValue).Select(x => x.Value).ToList();
        if (margins.Count >= 2)
        {
            var marginTrend = margins[^1] - margins[0];
            if (marginTrend > 0.05m) { score += 2; details.Add($"Expanding gross margin: +{marginTrend:P1}"); }
            else if (marginTrend > 0) { score += 1; details.Add($"Slightly improving margin: +{marginTrend:P1}"); }

            if (margins[^1] > 0.50m) { score += 2; details.Add($"High gross margin: {margins[^1]:P1}"); }
        }
        else
        {
            details.Add("Insufficient gross margin data");
        }

        // 3. Operating Leverage
        var opMargins = ordered.Select(m => m.OperatingMargin).Where(x => x.HasValue).Select(x => x.Value).ToList();
        var revenues = revenueGrowths; // already filtered above
        if (opMargins.Count >= 2 && revenues.Count >= 2)
        {
            var revGrowth = revenues[^1] - revenues[0];
            var opGrowth = opMargins[^1] - opMargins[0];
            if (revGrowth > opGrowth) { score += 2; details.Add("Positive operating leverage: Revenue growth exceeds op margin growth"); }
        }

        // 4. R&D intensity relative to latest revenue
        var rAndDs = lineItems?
            .Where(li => li?.Extras != null)
            .SelectMany(li => li.Extras.TryGetValue("ResearchAndDevelopment", out var r) && r is decimal d ? new[] { d } : Array.Empty<decimal>())
            .ToList() ?? new List<decimal>();

        // Extract latest revenue from the most recent line item
        var latestRevenue = lineItems?
            .OrderByDescending(li => li.ReportPeriod)
            .FirstOrDefault(x => x.Extras.TryGetValue("TotalRevenue", out var val) && val is decimal) is { } recentItem
            ? (decimal?)recentItem.Extras["TotalRevenue"]
            : null;

        if (rAndDs.Count >= 2 && latestRevenue.HasValue && latestRevenue.Value < 10_000_000) // $10M
        {
            var rAndDTrend = rAndDs[^1] - rAndDs[0];
            if (rAndDTrend > 0) score += 2;

            var rAndDIntensity = rAndDs[^1] / (latestRevenue.Value == 0 ? 1 : latestRevenue.Value);
            if (rAndDIntensity > 5) // R&D is 5x revenue
            {
                score += 3;
                details.Add($"Disruptive profile: R&D intensity = {rAndDIntensity:N1}x revenue");
            }
        }

        if (score == 0)
            details.Add("No strong indicators of disruptive potential found");

        return new FinancialAnalysisResult(nameof(DisruptivePotential), score, details);
    }

    private FinancialAnalysisResult InnovationGrowth(IEnumerable<FinancialMetrics> metrics, IEnumerable<FinancialLineItem> lineItems)
    {
        int score = 0;
        var details = new List<string>();

        var rds = lineItems?
            .Where(li => li?.Extras != null)
            .SelectMany(li => li.Extras.TryGetValue("ResearchAndDevelopment", out var r) && r is decimal d ? new[] { d } : Array.Empty<decimal>())
            .ToList() ?? new List<decimal>();

        var revenues = lineItems?
            .Where(li => li?.Extras != null)
            .SelectMany(li => li.Extras.TryGetValue("TotalRevenue", out var rev) && rev is decimal d ? new[] { d } : Array.Empty<decimal>())
            .ToList() ?? new List<decimal>();

        if (rds.Count >= 2 && revenues.Count >= 2)
        {
            var rdGrowth = (rds[^1] - rds[0]) / Math.Abs(rds[0]);
            if (rdGrowth > 0.5m) { score += 3; details.Add($"Strong R&D growth: +{rdGrowth:P1}"); }
            else if (rdGrowth > 0.2m) { score += 2; details.Add($"Moderate R&D growth: +{rdGrowth:P1}"); }

            var rdIntensityStart = rds[0] / revenues[0];
            var rdIntensityEnd = rds[^1] / revenues[^1];
            if (rdIntensityEnd > rdIntensityStart)
            {
                score += 2;
                details.Add($"R&D intensity increasing: {rdIntensityStart:P1} → {rdIntensityEnd:P1}");
            }
        }

        var fcf = metrics.Select(li => li.OperatingCashFlow).ToList();
        if (fcf.Count >= 2)
        {
            var growth = (fcf[^1] - fcf[0]) / Math.Abs(fcf[0].Value);
            var positiveCount = fcf.Count(f => f > 0);
            if (growth > 0.3m && positiveCount == fcf.Count) { score += 3; details.Add("Strong, consistent FCF growth"); }
            else if (positiveCount >= fcf.Count * 0.75) { score += 2; details.Add("Consistently positive FCF"); }
            else if (positiveCount > fcf.Count * 0.5) { score += 1; details.Add("Moderate FCF stability"); }
        }

        var opMargins = metrics.Select(m => m.OperatingMargin).Where(m => m.HasValue).Select(m => m.Value).ToList();
        if (opMargins.Count >= 2)
        {
            var marginTrend = opMargins[^1] - opMargins[0];
            if (opMargins[^1] > 0.15m && marginTrend > 0) { score += 3; details.Add($"Strong improving op margin: {opMargins[^1]:P1}"); }
            else if (opMargins[^1] > 0.10m) { score += 2; details.Add($"Healthy op margin: {opMargins[^1]:P1}"); }
            else if (marginTrend > 0) { score += 1; details.Add("Improving op efficiency"); }
        }

        var capex = metrics.Select(li => li.CapitalExpenditure).ToList();
        if (capex.Count >= 2 && revenues.Count >= 2)
        {
            var intensity = Math.Abs(capex[^1].Value) / revenues[^1];
            var growth = (Math.Abs(capex[^1].Value) - Math.Abs(capex[0].Value)) / Math.Abs(capex[0].Value);
            if (intensity > 0.10m && growth > 0.2m) { score += 2; details.Add("Strong capex for growth"); }
            else if (intensity > 0.05m) { score += 1; details.Add("Moderate capex for growth"); }
        }

        var dividends = metrics.Select(li => li.DividendsAndOtherCashDistributions).ToList();
        if (dividends.Count > 0 && fcf.Count > 0)
        {
            var payoutRatio = fcf[^1] != 0 ? dividends[^1] / fcf[^1] : 1;
            if (payoutRatio < 0.2m) { score += 2; details.Add("Strong reinvestment (low dividend payout)"); }
            else if (payoutRatio < 0.4m) { score += 1; details.Add("Moderate reinvestment"); }
        }

        return new FinancialAnalysisResult(nameof(InnovationGrowth), score, details);
    }

    private FinancialAnalysisResult Valuation(IEnumerable<FinancialMetrics> metrics, decimal marketCap)
    {
        var fcfList = metrics.Where(v => v.OperatingCashFlow.HasValue)
                              .OrderByDescending(v => v.Period)
                              .Select(v => v.OperatingCashFlow.Value)
                              .ToList();

        if (fcfList.Count == 0)
            return new FinancialAnalysisResult(nameof(Valuation), 0, new[] { "No positive OperatingCashFlow available" });

        decimal baseFcf;
        bool isQuarterly = metrics.Count() > 4; // crude, better if you have IsQuarterly flag

        if (isQuarterly)
            baseFcf = fcfList.Take(4).Sum(); // Sum last 4 quarters
        else
            baseFcf = fcfList.First(); // Use latest annual

        // Dynamic sector-based assumptions
        decimal growthRate;
        int terminalMultiple;

        var sector = metrics.First(m => !string.IsNullOrWhiteSpace(m.Industry)).Industry.ToLower();
        if (sector.Contains("ai") || sector.Contains("semiconductors"))
        {
            growthRate = 0.30m;
            terminalMultiple = 35;
        }
        else if (sector.Contains("biotech") || sector.Contains("genomics"))
        {
            growthRate = 0.25m;
            terminalMultiple = 30;
        }
        else if (sector.Contains("fintech") || sector.Contains("crypto"))
        {
            growthRate = 0.20m;
            terminalMultiple = 25;
        }
        else if (sector.Contains("ev") || sector.Contains("energy"))
        {
            growthRate = 0.25m;
            terminalMultiple = 30;
        }
        else
        {
            growthRate = 0.20m;
            terminalMultiple = 25;
        }

        const decimal discountRate = 0.15m;
        const int years = 5;

        decimal presentValue = 0;
        for (int i = 1; i <= years; i++)
        {
            var futureFcf = baseFcf * (decimal)Math.Pow((double)(1 + growthRate), i);
            var pv = futureFcf / (decimal)Math.Pow((double)(1 + discountRate), i);
            presentValue += pv;
        }

        var terminalValue = (baseFcf * (decimal)Math.Pow((double)(1 + growthRate), years) * terminalMultiple)
                             / (decimal)Math.Pow((double)(1 + discountRate), years);

        var intrinsicValue = presentValue + terminalValue;
        var marginOfSafety = (intrinsicValue - marketCap) / marketCap;

        int score = 0;
        if (marginOfSafety > 0.5m) score += 3;
        else if (marginOfSafety > 0.2m) score += 2;
        else if (marginOfSafety > 0m) score += 1;

        var details = $"Intrinsic value: ~{intrinsicValue:N0}; Market cap: ~{marketCap:N0}; Margin of safety: {marginOfSafety:P1}";
        return new FinancialAnalysisResult(nameof(Valuation), score, new[] { details });
    }

    private bool TryGenerateOutput(TradingWorkflowState state, string ticker, FinancialAnalysisResult disruptive,
        FinancialAnalysisResult innovation, FinancialAnalysisResult valuation, decimal totalScore, int maxScore,
        out TradeSignal tradeSignal)
    {
        var systemMessage = @"You are a Cathie Wood AI agent, making investment decisions using her principles:
1. Seek companies leveraging disruptive innovation.
2. Emphasize exponential growth potential, large TAM.
3. Focus on technology, healthcare, or other future-facing sectors.
4. Consider multi-year time horizons for potential breakthroughs.
5. Accept higher volatility in pursuit of high returns.
6. Evaluate management's vision and ability to invest in R&D.

Rules:
- Identify disruptive or breakthrough technology.
- Evaluate strong potential for multi-year revenue growth.
- Check if the company can scale effectively in a large market.
- Use a growth-biased valuation approach.
- Provide a data-driven recommendation (bullish, bearish, or neutral).";

        var analysisData = new
        {
            score = totalScore,
            max_score = maxScore,
            disruptive_analysis = disruptive,
            innovation_analysis = innovation,
            valuation_analysis = valuation
        };

        return LlmTradeSignalGenerator.TryGenerateSignal(
            _httpLib,
            "chat/completions",
            ticker,
            systemMessage,
            analysisData,
            agentName: "Cathie Wood",
            out tradeSignal
        );
    }
}
