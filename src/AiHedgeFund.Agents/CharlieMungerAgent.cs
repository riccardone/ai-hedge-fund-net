using AiHedgeFund.Contracts;
using AiHedgeFund.Contracts.Model;
using NLog;
using System.Text.RegularExpressions;

namespace AiHedgeFund.Agents;

/// <summary>
/// Analyzes stocks using Charlie Munger's investing principles and mental models.
/// Focuses on moat strength, management quality, predictability, and valuation.
/// </summary>
public class CharlieMungerAgent
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly IHttpLib _chatter;

    public CharlieMungerAgent(IHttpLib chatter)
    {
        _chatter = chatter;
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
            Logger.Info("[CharlieMunger] Starting analysis for {0}", ticker);

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

            var moatStrength = AnalyzeMoatStrength(metrics, lineItems);
            var managementQuality = AnalyzeManagementQuality(metrics, null);
            var predictability = AnalyzePredictability(metrics, lineItems);
            var valuation = AnalyzeValuation(metrics, marketCap);

            Logger.Info("{0} Moat Strength: {1}", ticker, string.Join("; ", moatStrength.Details));
            Logger.Info("{0} Management Quality: {1}", ticker, string.Join("; ", managementQuality.Details));
            Logger.Info("{0} Predictability: {1}", ticker, string.Join("; ", predictability.Details));
            Logger.Info("{0} Valuation: {1}", ticker, string.Join("; ", valuation.Details));

            var totalScore = moatStrength.Score + managementQuality.Score + valuation.Score + predictability.Score + valuation.Score;
            const int maxScore = 15;

            if(TryGenerateOutput(state, ticker, moatStrength, managementQuality, predictability, valuation, totalScore, maxScore, out TradeSignal tradeSignal))
                signals.Add(tradeSignal);

            // TODO what if it is not generated
        }

        return signals;
    }

    /// <summary>
    /// Assess the predictability of the business - Munger strongly prefers businesses
    /// whose future operations and cashflows are relatively easy to predict.
    /// </summary>
    /// <param name="metrics"></param>
    /// <param name="lineItems"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private FinancialAnalysisResult AnalyzePredictability(IEnumerable<FinancialMetrics> metrics, IEnumerable<FinancialLineItem> lineItems)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Evaluate management quality using Munger's criteria:
    /// - Capital allocation wisdom
    /// - Insider ownership and transactions
    /// - Cash management efficiency
    /// - Candor and transparency
    /// - Long-term focus
    /// </summary>
    /// <param name="metrics"></param>
    /// <param name="insiderTrades"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private FinancialAnalysisResult AnalyzeManagementQuality(IEnumerable<FinancialMetrics> metrics, object insiderTrades)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Analyze the business's competitive advantage using Munger's approach:
    /// - Consistent high returns on capital(ROIC)
    /// - Pricing power(stable/improving gross margins)
    /// - Low capital requirements
    /// - Network effects and intangible assets(R&D investments, goodwill)
    /// </summary>
    /// <param name="metrics"></param>
    /// <param name="lineItems"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private FinancialAnalysisResult AnalyzeMoatStrength(IEnumerable<FinancialMetrics> metrics, IEnumerable<FinancialLineItem> lineItems)
    {
        var maxScore = 10;
        var score = 0;
        var details = new List<string>();

        if (!metrics.Any() || !lineItems.Any())
        {
            return new FinancialAnalysisResult(0, new[] { "Insufficient data to analyze moat strength" }, maxScore);
        }

        var ordered = metrics.OrderBy(m => m.Period).ToList();

        // 1. ROIC Analysis
        var roicValues = ordered.Where(li => li.ReturnOnInvestedCapital.HasValue).Select(m => m.ReturnOnInvestedCapital).ToList();

        if (roicValues.Any())
        {
            var highRoicCount = roicValues.Count(roic => roic > 0.15m);
            var total = roicValues.Count;
            if (highRoicCount >= total * 0.8m)
            {
                score += 3;
                details.Add($"Excellent ROIC: >15% in {highRoicCount}/{total} periods");
            }
            else if (highRoicCount >= total * 0.5m)
            {
                score += 2;
                details.Add($"Good ROIC: >15% in {highRoicCount}/{total} periods");
            }
            else if (highRoicCount > 0)
            {
                score += 1;
                details.Add($"Mixed ROIC: >15% in {highRoicCount}/{total} periods");
            }
            else
            {
                details.Add("Poor ROIC: Never exceeds 15% threshold");
            }
        }
        else
        {
            details.Add("No ROIC data available");
        }

        // 2. Gross Margin / Pricing Power
        var grossMargins = ordered.Select(m => m.GrossMargin).Where(x => x.HasValue).Select(x => x.Value).ToList();

        if (grossMargins.Count >= 2)
        {
            var improvingTrend = 0;
            for (int i = 1; i < grossMargins.Count; i++)
            {
                if (grossMargins[i] >= grossMargins[i - 1]) improvingTrend++;
            }

            if (improvingTrend >= grossMargins.Count * 0.7)
            {
                score += 2;
                details.Add("Strong pricing power: Gross margins consistently improving");
            }
            else if (grossMargins.Average() > 0.3m)
            {
                score += 1;
                details.Add($"Good pricing power: Average gross margin {grossMargins.Average():P1}");
            }
            else
            {
                details.Add("Limited pricing power: Low or declining gross margins");
            }
        }
        else
        {
            details.Add("Insufficient gross margin data");
        }

        // 3. Capital Requirements (CapEx to Revenue)
        var capexRatios = new List<decimal>();
        foreach (var li in lineItems)
        {
            if (li.Extras.TryGetValue("CapitalExpenditures", out var capexObj) && capexObj is decimal capex &&
                li.Extras.TryGetValue("TotalRevenue", out var revenueObj) && revenueObj is decimal revenue && revenue > 0)
            {
                var capexRatio = Math.Abs(capex) / revenue;
                capexRatios.Add(capexRatio);
            }
        }

        if (capexRatios.Any())
        {
            var avgCapex = capexRatios.Average();
            if (avgCapex < 0.05m)
            {
                score += 2;
                details.Add($"Low capital requirements: Avg capex {avgCapex:P1} of revenue");
            }
            else if (avgCapex < 0.1m)
            {
                score += 1;
                details.Add($"Moderate capital requirements: Avg capex {avgCapex:P1} of revenue");
            }
            else
            {
                details.Add($"High capital requirements: Avg capex {avgCapex:P1} of revenue");
            }
        }
        else
        {
            details.Add("No capital expenditure data available");
        }

        // 4. Intangibles / IP (R&D and Goodwill)
        var rAndDTotal = lineItems
            .Where(li => li.Extras.TryGetValue("ResearchAndDevelopment", out var rObj) && rObj is decimal r)
            .Sum(li => (decimal)li.Extras["ResearchAndDevelopment"]);

        var goodwillExists = ordered.Select(m => m.GoodwillAndIntangibleAssets).Any(x => x.HasValue);

        if (rAndDTotal > 0)
        {
            score += 1;
            details.Add("Invests in R&D, building intellectual property");
        }

        if (goodwillExists)
        {
            score += 1;
            details.Add("Significant goodwill/intangibles, suggesting brand value or IP");
        }

        var finalScore = Math.Min(maxScore, score * maxScore / 9);

        return new FinancialAnalysisResult(finalScore, details, maxScore);
    }

    private static FinancialAnalysisResult AnalyzeValuation(IEnumerable<FinancialMetrics> metrics, decimal? marketCap)
    {
        throw new NotImplementedException();
    }

    private bool TryGenerateOutput(TradingWorkflowState state, string ticker,
        FinancialAnalysisResult financialAnalysisResult, FinancialAnalysisResult businessQuality,
        FinancialAnalysisResult financialDiscipline, FinancialAnalysisResult valuation, int totalScore, int maxScore,
        out TradeSignal tradeSignal)
    {
        throw new NotImplementedException();
    }

    private bool TryExtractJson(string content, out string json)
    {
        json = string.Empty;

        if (content.StartsWith("```"))
        {
            int start = content.IndexOf("{");
            int end = content.LastIndexOf("}");
            if (start >= 0 && end > start)
            {
                json = content[start..(end + 1)];
                return true;
            }
        }

        var match = Regex.Match(content, "\\{[\\s\\S]*?\\}");
        if (match.Success)
        {
            json = match.Value;
            return true;
        }

        if (content.Trim().StartsWith("{") && content.Trim().EndsWith("}"))
        {
            json = content;
            return true;
        }

        return false;
    }
}
