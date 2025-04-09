﻿using AiHedgeFund.Contracts;
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
        var result = new FinancialAnalysisResult();

        if (metrics == null || !metrics.Any())
        {
            result.AddDetail("Insufficient data to analyze business predictability");
            return result;
        }

        var ordered = metrics.OrderBy(m => m.Period).ToList();
        if (ordered.Count < 5)
        {
            result.AddDetail("Insufficient data to analyze business predictability (need 5+ years)");
            return result;
        }

        var items = lineItems?.ToList();
        if (items == null || items.Count < 5)
        {
            result.AddDetail("Insufficient data to analyze business predictability (need 5+ years)");
            return result;
        }

        // 1. Revenue stability and growth
        var revenues = ordered.Where(x => x.TotalRevenue.HasValue).Select(m => m.TotalRevenue).ToList();

        if (revenues.Count >= 5)
        {
            var growthRates = new List<decimal>();
            for (int i = 0; i < revenues.Count - 1; i++)
            {
                if (revenues[i + 1] == 0) continue;
                growthRates.Add(revenues[i].Value / revenues[i + 1].Value - 1);
            }

            if (growthRates.Count > 0)
            {
                var avgGrowth = growthRates.Average();
                var growthVolatility = growthRates.Average(r => Math.Abs(r - avgGrowth));

                if (avgGrowth > 0.05m && growthVolatility < 0.10m)
                {
                    result.AddDetail($"Highly predictable revenue: {avgGrowth:P1} avg growth with low volatility");
                    result.IncreaseScore(3);
                }
                else if (avgGrowth > 0 && growthVolatility < 0.20m)
                {
                    result.AddDetail($"Moderately predictable revenue: {avgGrowth:P1} avg growth with some volatility");
                    result.IncreaseScore(2);
                }
                else if (avgGrowth > 0)
                {
                    result.AddDetail($"Growing but less predictable revenue: {avgGrowth:P1} avg growth with high volatility");
                    result.IncreaseScore(1);
                }
                else
                {
                    result.AddDetail($"Declining or highly unpredictable revenue: {avgGrowth:P1} avg growth");
                }
            }
        }
        else
        {
            result.AddDetail("Insufficient revenue history for predictability analysis");
        }

        // 2. Operating income stability
        var operatingIncomes = items
            .Where(x => x.Extras.TryGetValue("OperatingIncome", out var oi) && oi is decimal)
            .Select(x => (decimal)x.Extras["OperatingIncome"])
            .ToList();

        var oiTotalPeriods = operatingIncomes.Count;
        var oiPositivePeriods = operatingIncomes.Count(i => i > 0);

        if (oiTotalPeriods >= 5)
        {
            if (oiPositivePeriods == oiTotalPeriods)
            {
                result.AddDetail("Highly predictable operations: Operating income positive in all periods");
                result.IncreaseScore(3);
            }
            else if (oiPositivePeriods >= oiTotalPeriods * 0.8)
            {
                result.AddDetail($"Predictable operations: Operating income positive in {oiPositivePeriods}/{oiTotalPeriods} periods");
                result.IncreaseScore(2);
            }
            else if (oiPositivePeriods >= oiTotalPeriods * 0.6)
            {
                result.AddDetail($"Somewhat predictable operations: Operating income positive in {oiPositivePeriods}/{oiTotalPeriods} periods");
                result.IncreaseScore(1);
            }
            else
            {
                result.AddDetail($"Unpredictable operations: Operating income positive in only {oiPositivePeriods}/{oiTotalPeriods} periods");
            }
        }
        else if (oiTotalPeriods >= 3)
        {
            result.AddDetail("Limited history (3–4 periods): Predictability assessment may be less reliable");

            if (oiPositivePeriods == oiTotalPeriods)
            {
                result.AddDetail($"All periods show positive operating income ({oiPositivePeriods}/{oiTotalPeriods})");
                result.IncreaseScore(2);
            }
            else if (oiPositivePeriods >= oiTotalPeriods * 0.66)
            {
                result.AddDetail($"Mostly positive operating income in {oiPositivePeriods}/{oiTotalPeriods} periods");
                result.IncreaseScore(1);
            }
            else
            {
                result.AddDetail($"Inconsistent operating income: {oiPositivePeriods}/{oiTotalPeriods} periods positive");
            }
        }
        else if (oiTotalPeriods >= 2)
        {
            result.AddDetail("Very limited history (2 periods): Treating with caution");

            if (oiPositivePeriods == oiTotalPeriods)
            {
                result.AddDetail("Both periods show positive operating income");
                result.IncreaseScore(1); // minimal confidence
            }
            else if (oiPositivePeriods == 1)
            {
                result.AddDetail("Only 1 out of 2 periods show positive operating income");
            }
            else
            {
                result.AddDetail("No positive operating income in 2-period view");
            }
        }
        else
        {
            result.AddDetail("Insufficient operating income history (less than 2 periods)");
        }

        // 3. Margin consistency
        var opMargins = items
            .Where(x => x.Extras.TryGetValue("OperatingMargin", out var om) && om is decimal)
            .Select(x => (decimal)x.Extras["OperatingMargin"])
            .ToList();

        var totalMargins = opMargins.Count;

        if (totalMargins >= 5)
        {
            var avgMargin = opMargins.Average();
            var marginVolatility = opMargins.Average(m => Math.Abs(m - avgMargin));

            if (marginVolatility < 0.03m)
            {
                result.AddDetail($"Highly predictable margins: {avgMargin:P1} avg with minimal volatility");
                result.IncreaseScore(2);
            }
            else if (marginVolatility < 0.07m)
            {
                result.AddDetail($"Moderately predictable margins: {avgMargin:P1} avg with some volatility");
                result.IncreaseScore(1);
            }
            else
            {
                result.AddDetail($"Unpredictable margins: {avgMargin:P1} avg with high volatility ({marginVolatility:P1})");
            }
        }
        else if (totalMargins >= 3)
        {
            var avgMargin = opMargins.Average();
            var marginVolatility = opMargins.Average(m => Math.Abs(m - avgMargin));

            result.AddDetail("Limited margin history (3–4 periods): Predictability assessment may be less reliable");

            if (marginVolatility < 0.04m)
            {
                result.AddDetail($"Fairly predictable margins: {avgMargin:P1} avg with low volatility");
                result.IncreaseScore(1);
            }
            else
            {
                result.AddDetail($"Volatile margins: {avgMargin:P1} avg with high volatility ({marginVolatility:P1})");
            }
        }
        else if (totalMargins >= 2)
        {
            var avgMargin = opMargins.Average();
            var marginVolatility = opMargins.Average(m => Math.Abs(m - avgMargin));

            result.AddDetail("Very limited margin history (2 periods): Treating with caution");

            if (marginVolatility < 0.05m)
            {
                result.AddDetail($"Potentially stable margins: {avgMargin:P1} avg with low short-term variation");
                result.IncreaseScore(1); // cautious score
            }
            else
            {
                result.AddDetail($"Potentially unstable margins: {avgMargin:P1} avg with notable short-term variation");
            }
        }
        else
        {
            result.AddDetail("Insufficient operating margin history (less than 2 periods)");
        }

        // 4. Cash generation reliability
        var fcfs = ordered.Where(x => x.FreeCashFlow > 0).Select(x => x.FreeCashFlow).ToList();

        if (fcfs.Count >= 5)
        {
            var positivePeriods = fcfs.Count(f => f > 0);
            var totalPeriods = fcfs.Count;

            if (positivePeriods == totalPeriods)
            {
                result.AddDetail("Highly predictable cash generation: Positive FCF in all periods");
                result.IncreaseScore(2);
            }
            else if (positivePeriods >= totalPeriods * 0.8)
            {
                result.AddDetail($"Predictable cash generation: Positive FCF in {positivePeriods}/{totalPeriods} periods");
                result.IncreaseScore(1);
            }
            else
            {
                result.AddDetail($"Unpredictable cash generation: Positive FCF in only {positivePeriods}/{totalPeriods} periods");
            }
        }
        else
        {
            result.AddDetail("Insufficient free cash flow history");
        }

        return result;
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
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private FinancialAnalysisResult AnalyzeManagementQuality(IEnumerable<FinancialMetrics> metrics, IEnumerable<FinancialLineItem> lineItems)
    {
        var result = new FinancialAnalysisResult(0, new List<string>(), 12);
        var items = lineItems?.ToList() ?? new List<FinancialLineItem>();
        //var trades = insiderTrades?.ToList() ?? new List<InsiderTrade>();
        int score = 0;

        if (items.Count == 0)
        {
            result.AddDetail("Insufficient data to analyze management quality");
            return result;
        }

        if (metrics == null || !metrics.Any())
        {
            result.AddDetail("Insufficient data to analyze business predictability");
            return result;
        }

        var ordered = metrics.OrderBy(m => m.Period).ToList();
        if (ordered.Count < 5)
        {
            result.AddDetail("Insufficient data to analyze business predictability (need 5+ years)");
            return result;
        }

        // 1. Capital Allocation - FCF/Net Income Ratio
        var fcfs = ordered.Where(x => x.FreeCashFlow > 0).Select(x => x.FreeCashFlow).ToList();

        var netIncomes = items.Where(x => x.Extras.TryGetValue("net_income", out var n) && n is decimal)
                              .Select(x => (decimal)x.Extras["net_income"])
                              .ToList();

        if (fcfs.Count > 0 && fcfs.Count == netIncomes.Count)
        {
            var ratios = new List<decimal>();
            for (int i = 0; i < fcfs.Count; i++)
            {
                if (netIncomes[i] > 0)
                    ratios.Add(fcfs[i] / netIncomes[i]);
            }

            if (ratios.Count > 0)
            {
                var avg = ratios.Average();
                if (avg > 1.1m)
                {
                    score += 3;
                    result.AddDetail($"Excellent cash conversion: FCF/NI ratio of {avg:F2}");
                }
                else if (avg > 0.9m)
                {
                    score += 2;
                    result.AddDetail($"Good cash conversion: FCF/NI ratio of {avg:F2}");
                }
                else if (avg > 0.7m)
                {
                    score += 1;
                    result.AddDetail($"Moderate cash conversion: FCF/NI ratio of {avg:F2}");
                }
                else
                {
                    result.AddDetail($"Poor cash conversion: FCF/NI ratio of only {avg:F2}");
                }
            }
            else
            {
                result.AddDetail("Could not calculate FCF to Net Income ratios");
            }
        }
        else
        {
            result.AddDetail("Missing FCF or Net Income data");
        }

        // 2. Debt Management - D/E Ratio
        var debts = items.Where(x => x.Extras.TryGetValue("total_debt", out var d) && d is decimal)
                         .Select(x => (decimal)x.Extras["total_debt"])
                         .ToList();

        var equities = items.Where(x => x.Extras.TryGetValue("shareholders_equity", out var e) && e is decimal)
                            .Select(x => (decimal)x.Extras["shareholders_equity"])
                            .ToList();

        if (debts.Count > 0 && equities.Count > 0)
        {
            var ratio = equities[0] > 0 ? debts[0] / equities[0] : decimal.MaxValue;

            if (ratio < 0.3m)
            {
                score += 3;
                result.AddDetail($"Conservative debt management: D/E ratio of {ratio:F2}");
            }
            else if (ratio < 0.7m)
            {
                score += 2;
                result.AddDetail($"Prudent debt management: D/E ratio of {ratio:F2}");
            }
            else if (ratio < 1.5m)
            {
                score += 1;
                result.AddDetail($"Moderate debt level: D/E ratio of {ratio:F2}");
            }
            else
            {
                result.AddDetail($"High debt level: D/E ratio of {ratio:F2}");
            }
        }
        else
        {
            result.AddDetail("Missing debt or equity data");
        }

        // 3. Cash Management
        var cashes = items.Where(x => x.Extras.TryGetValue("cash_and_equivalents", out var c) && c is decimal)
                          .Select(x => (decimal)x.Extras["cash_and_equivalents"])
                          .ToList();

        var revenues = items.Where(x => x.Extras.TryGetValue("revenue", out var r) && r is decimal)
                            .Select(x => (decimal)x.Extras["revenue"])
                            .ToList();

        if (cashes.Count > 0 && revenues.Count > 0 && revenues[0] > 0)
        {
            var cashToRev = cashes[0] / revenues[0];
            if (cashToRev >= 0.1m && cashToRev <= 0.25m)
            {
                score += 2;
                result.AddDetail($"Prudent cash management: Cash/Revenue ratio of {cashToRev:F2}");
            }
            else if ((cashToRev >= 0.05m && cashToRev < 0.1m) || (cashToRev > 0.25m && cashToRev <= 0.4m))
            {
                score += 1;
                result.AddDetail($"Acceptable cash position: Cash/Revenue ratio of {cashToRev:F2}");
            }
            else if (cashToRev > 0.4m)
            {
                result.AddDetail($"Excess cash reserves: Cash/Revenue ratio of {cashToRev:F2}");
            }
            else
            {
                result.AddDetail($"Low cash reserves: Cash/Revenue ratio of {cashToRev:F2}");
            }
        }
        else
        {
            result.AddDetail("Insufficient cash or revenue data");
        }

        // 4. Insider Activity - Skipped due to missing data source
        result.AddDetail("Insider activity analysis skipped (data not available from financial data provider)");
        //if (trades.Count > 0)
        //{
        //    var buys = trades.Count(t => t.TransactionType?.ToLower() is "buy" or "purchase");
        //    var sells = trades.Count(t => t.TransactionType?.ToLower() is "sell" or "sale");
        //    var total = buys + sells;

        //    if (total > 0)
        //    {
        //        var buyRatio = (decimal)buys / total;

        //        if (buyRatio > 0.7m)
        //        {
        //            score += 2;
        //            result.AddDetail($"Strong insider buying: {buys}/{total} transactions are purchases");
        //        }
        //        else if (buyRatio > 0.4m)
        //        {
        //            score += 1;
        //            result.AddDetail($"Balanced insider trading: {buys}/{total} transactions are purchases");
        //        }
        //        else if (buyRatio < 0.1m && sells > 5)
        //        {
        //            score -= 1;
        //            result.AddDetail($"Concerning insider selling: {sells}/{total} transactions are sales");
        //        }
        //        else
        //        {
        //            result.AddDetail($"Mixed insider activity: {buys}/{total} transactions are purchases");
        //        }
        //    }
        //    else
        //    {
        //        result.AddDetail("No recorded insider transactions");
        //    }
        //}
        //else
        //{
        //    result.AddDetail("No insider trading data available");
        //}

        // 5. Share Count Consistency
        var shares = items.Where(x => x.Extras.TryGetValue("outstanding_shares", out var s) && s is decimal)
                          .Select(x => (decimal)x.Extras["outstanding_shares"])
                          .ToList();

        if (shares.Count >= 3)
        {
            var start = shares[^1];
            var end = shares[0];

            if (end < start * 0.95m)
            {
                score += 2;
                result.AddDetail("Shareholder-friendly: Reducing share count over time");
            }
            else if (end < start * 1.05m)
            {
                score += 1;
                result.AddDetail("Stable share count: Limited dilution");
            }
            else if (end > start * 1.2m)
            {
                score -= 1;
                result.AddDetail("Concerning dilution: Share count increased significantly");
            }
            else
            {
                result.AddDetail("Moderate share count increase over time");
            }
        }
        else
        {
            result.AddDetail("Insufficient share count data");
        }

        // Final Score Scaled to 0–10
        var finalScore = Math.Clamp(score * 10 / result.MaxScore, 0, 10);
        result.SetScore(finalScore);

        return result;
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
