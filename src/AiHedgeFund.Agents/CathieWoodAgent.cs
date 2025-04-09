using AiHedgeFund.Contracts;
using AiHedgeFund.Contracts.Model;
using NLog;
using System.Text.Json;
using System.Text.RegularExpressions;
using AiHedgeFund.Agents.Services;

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
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly IHttpLib _httpLib;

    public CathieWoodAgent(IHttpLib chatter)
    {
        _httpLib = chatter;
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
            Logger.Info("[CathieWood] Starting analysis for {0}", ticker);

            if (!state.FinancialMetrics.TryGetValue(ticker, out var metrics)
                || !state.FinancialLineItems.TryGetValue(ticker, out var lineItems))
            {
                Logger.Warn($"Missing data for {ticker}");
                continue;
            }

            var marketCap = metrics.OrderByDescending(m => m.Period).FirstOrDefault()?.MarketCap;
            if (marketCap == null)
            {
                Logger.Warn($"No market cap for {ticker}");
                continue;
            }

            var disruptive = AnalyzeDisruptivePotential(metrics, lineItems);
            var innovation = AnalyzeInnovationGrowth(metrics, lineItems);
            var valuation = AnalyzeValuation(metrics, marketCap.Value);

            Logger.Info("{0} Disruptive Potential: {1}", ticker, disruptive.Details);
            Logger.Info("{0} Innovation Growth: {1}", ticker, innovation.Details);
            Logger.Info("{0} Valuation: {1}", ticker, valuation.Details);

            var totalScore = disruptive.Score + innovation.Score + valuation.Score;
            const int maxScore = 15;

            if (TryGenerateOutput(state, ticker, disruptive, innovation, valuation, totalScore, maxScore, out var tradeSignal))
            {
                signals.Add(tradeSignal);
            }
            else
            {
                var signal = GenerateOutputWithoutLLM(totalScore, maxScore, disruptive, innovation, valuation, out var reasoning, out var confidence);
                signals.Add(new TradeSignal(ticker, signal, confidence, reasoning));
            }
        }

        return signals;
    }

    private AnalysisResult AnalyzeDisruptivePotential(IEnumerable<FinancialMetrics> metrics, IEnumerable<FinancialLineItem> lineItems)
    {
        int score = 0;
        var details = new List<string>();
        var ordered = metrics.OrderBy(m => m.Period).ToList();

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

        var opMargins = ordered.Select(m => m.OperatingMargin).Where(x => x.HasValue).Select(x => x.Value).ToList();
        var revenues = ordered.Select(m => m.RevenueGrowth).Where(x => x.HasValue).Select(x => x.Value).ToList();
        if (opMargins.Count >= 2 && revenues.Count >= 2)
        {
            var revGrowth = revenues[^1] - revenues[0];
            var opGrowth = opMargins[^1] - opMargins[0];
            if (revGrowth > opGrowth) { score += 2; details.Add("Positive operating leverage: Revenue growth exceeds op margin growth"); }
        }

        var rAndDs = lineItems?
            .Where(li => li?.Extras != null)
            .SelectMany(li => li.Extras.TryGetValue("ResearchAndDevelopment", out var r) && r is decimal d ? new[] { d } : Array.Empty<decimal>())
            .ToList() ?? new List<decimal>();

        var latestRevenue = ordered.LastOrDefault()?.TotalRevenue;
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
        {
            details.Add("No strong indicators of disruptive potential found");
        }

        return new AnalysisResult { Score = score, Details = string.Join("; ", details) };
    }

    private AnalysisResult AnalyzeInnovationGrowth(IEnumerable<FinancialMetrics> metrics, IEnumerable<FinancialLineItem> lineItems)
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
            var growth = (fcf[^1] - fcf[0]) / Math.Abs(fcf[0]);
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
            var intensity = Math.Abs(capex[^1]) / revenues[^1];
            var growth = (Math.Abs(capex[^1]) - Math.Abs(capex[0])) / Math.Abs(capex[0]);
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

        return new AnalysisResult { Score = score, Details = string.Join("; ", details) };
    }

    private AnalysisResult AnalyzeValuation(IEnumerable<FinancialMetrics> metrics, decimal marketCap)
    {
        var fcf = metrics.Select(li => li.OperatingCashFlow).ToList();
        if (fcf.Count == 0 || fcf[^1] <= 0)
        {
            return new AnalysisResult { Score = 0, Details = $"No positive FCF for valuation; FCF = {fcf.LastOrDefault():N2}" };
        }

        var baseFcf = fcf[^1];
        const decimal growthRate = 0.20m;
        const decimal discountRate = 0.15m;
        const int terminalMultiple = 25;
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
        else if (marginOfSafety > 0.2m) score += 1;

        var details = $"Intrinsic value: ~{intrinsicValue:N0}; Market cap: ~{marketCap:N0}; Margin of safety: {marginOfSafety:P1}";
        return new AnalysisResult { Score = score, Details = details };
    }

    private class AnalysisResult
    {
        public decimal Score { get; set; }
        public string Details { get; set; } = string.Empty;
    }

    private static string GenerateOutputWithoutLLM(decimal totalScore, int maxScore, AnalysisResult disruptive,
        AnalysisResult innovation, AnalysisResult valuation, out string reasoning, out decimal confidence)
    {
        string signal;
        if (totalScore >= 0.7m * maxScore)
            signal = "bullish";
        else if (totalScore <= 0.3m * maxScore)
            signal = "bearish";
        else
            signal = "neutral";

        reasoning = $"Disruptive: {disruptive.Details}; Innovation: {innovation.Details}; Valuation: {valuation.Details}";
        confidence = Math.Round((totalScore / maxScore) * 100, 2);
        return signal;
    }

    private bool TryGenerateOutput(TradingWorkflowState state, string ticker, AnalysisResult disruptive,
        AnalysisResult innovation, AnalysisResult valuation, decimal totalScore, int maxScore,
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

        return LlmTradeSignalGenerator.TryGenerateSignal(
            _httpLib,
            "chat/completions",
            ticker,
            systemMessage,
            new
            {
                score = totalScore,
                max_score = maxScore,
                disruptive_analysis = disruptive,
                innovation_analysis = innovation,
                valuation_analysis = valuation
            },
            agentName: "Cathie Wood",
            out tradeSignal
        );
    }
}
