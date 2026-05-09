using AiHedgeFund.Agents;
using AiHedgeFund.Agents.Services;
using AiHedgeFund.Contracts;
using AiHedgeFund.Contracts.Model;
using AiHedgeFund.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace AiHedgeFund.Tests;

/// <summary>
/// Reproduces the observed runtime behaviour where prices and line items are missing
/// from TradingWorkflowState after initialization, causing agents to produce all-zero scores.
/// </summary>
public class TradingInitializerTests
{
    private const string Ticker = "NVDA";

    // ---------------------------------------------------------------------------
    // Bug 1: if TryGetFinancialLineItems returns false, prices are never added to
    // state even though TryGetPrices would succeed.  RiskManagerAgent then warns
    // "No price data found" and no risk assessment is produced.
    // ---------------------------------------------------------------------------
    [Test]
    public async Task InitializeAsync_WhenLineItemsFail_PricesAreStillAddedToState()
    {
        var args = new AppArguments { Tickers = [Ticker], AgentNames = ["ben_graham"] };
        var dataReader = new FakeDataReader
        {
            MetricsSucceeds = true,
            LineItemsSucceeds = false,   // ← line items fail
            PricesSucceeds = true,
            NewsSucceeds = true
        };
        var sut = new TradingInitializer(args, dataReader, NullLogger<TradingInitializer>.Instance);

        var state = await sut.InitializeAsync();

        Assert.That(state.Prices.ContainsKey(Ticker), Is.True,
            "Prices should be in state even when line items cannot be fetched");
    }

    // ---------------------------------------------------------------------------
    // Bug 2: if TryGetCompanyNews returns false, prices should still be in state.
    // (Regression guard for the previous fix.)
    // ---------------------------------------------------------------------------
    [Test]
    public async Task InitializeAsync_WhenNewsFails_PricesAreStillAddedToState()
    {
        var args = new AppArguments { Tickers = [Ticker], AgentNames = ["ben_graham"] };
        var dataReader = new FakeDataReader
        {
            MetricsSucceeds = true,
            LineItemsSucceeds = true,
            PricesSucceeds = true,
            NewsSucceeds = false        // ← news fails
        };
        var sut = new TradingInitializer(args, dataReader, NullLogger<TradingInitializer>.Instance);

        var state = await sut.InitializeAsync();

        Assert.That(state.Prices.ContainsKey(Ticker), Is.True,
            "Prices should be in state even when company news cannot be fetched");
    }

    // ---------------------------------------------------------------------------
    // Bug 3: BenGrahamAgent produces all-zero scores when FinancialLineItems are
    // absent from state (because the initializer skipped them after a failure).
    // ---------------------------------------------------------------------------
    [Test]
    public void BenGrahamAgent_WhenLineItemsMissingFromState_ScoresAreNotAllZero()
    {
        var metrics = new[]
        {
            FinancialMetricsFactory.CreateTestMetrics(Ticker, 2022, 10000, 12000, 2000, 500, 1_000_000_000),
            FinancialMetricsFactory.CreateTestMetrics(Ticker, 2023, 11000, 13000, 2100, 510, 1_100_000_000),
            FinancialMetricsFactory.CreateTestMetrics(Ticker, 2024, 12000, 14000, 2200, 520, 1_200_000_000),
        };

        var state = new TradingWorkflowState
        {
            Tickers = [Ticker],
            FinancialMetrics = new Dictionary<string, IEnumerable<FinancialMetrics>> { { Ticker, metrics } },
            // FinancialLineItems intentionally absent — mirrors what happens when the
            // initializer skips line items due to a fetch failure
        };

        var sut = new BenGrahamAgent(new FakeHttpLib(), NullLogger<BenGrahamAgent>.Instance);
        sut.Run(state);

        state.AnalystSignals.TryGetValue("ben_graham", out var agentSignals);
        AgentReport? report = null;
        agentSignals?.TryGetValue(Ticker, out report);

        // EarningsStability should score > 0 because EPS data IS present in metrics
        Assert.That(report, Is.Not.Null, "Agent should produce a report");
        Assert.That(report!.Confidence, Is.GreaterThan(0),
            "Agent should produce a non-zero confidence when metrics are available");
    }
}

// ---------------------------------------------------------------------------
// Configurable fake IDataReader for initializer tests
// ---------------------------------------------------------------------------
internal class FakeDataReader : IDataReader
{
    public bool MetricsSucceeds { get; init; } = true;
    public bool LineItemsSucceeds { get; init; } = true;
    public bool PricesSucceeds { get; init; } = true;
    public bool NewsSucceeds { get; init; } = true;

    public bool TryGetFinancialMetrics(string ticker, DateTime endDate, string period, int limit,
        out IEnumerable<FinancialMetrics>? metrics)
    {
        metrics = MetricsSucceeds
            ? [FinancialMetricsFactory.CreateTestMetrics(ticker, 2024, 12000, 14000, 2200, 520, 1_200_000_000)]
            : null;
        return MetricsSucceeds;
    }

    public bool TryGetFinancialLineItems(string ticker, DateTime endDate, string period, int limit,
        out IEnumerable<FinancialLineItem>? financialLineItems)
    {
        financialLineItems = LineItemsSucceeds
            ? [new FinancialLineItem(ticker, new DateTime(2024, 12, 31), period, "USD",
                new Dictionary<string, dynamic>
                {
                    ["TotalAssets"] = 100000m,
                    ["TotalLiabilities"] = 40000m,
                    ["TotalCurrentAssets"] = 30000m,
                    ["TotalCurrentLiabilities"] = 10000m,
                })]
            : null;
        return LineItemsSucceeds;
    }

    public bool TryGetPrices(string ticker, DateTime startDate, DateTime endDate,
        out IEnumerable<Price>? prices)
    {
        prices = PricesSucceeds
            ? [new Price { Date = DateTime.Today, Open = 100, High = 110, Low = 90, Close = 105, Volume = 1_000_000 }]
            : null;
        return PricesSucceeds;
    }

    public bool TryGetCompanyNews(string ticker, out IEnumerable<NewsSentiment>? newsSentiments)
    {
        newsSentiments = NewsSucceeds ? [] : null;
        return NewsSucceeds;
    }
}
