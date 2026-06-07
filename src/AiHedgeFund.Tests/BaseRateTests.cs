using System.Net;
using AiHedgeFund.Contracts.Model;
using AiHedgeFund.Data.ChartLibrary;
using AiHedgeFund.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace AiHedgeFund.Tests;

/// <summary>
/// Covers the optional base-rate grounding feature end to end: the pure <see cref="BaseRate"/>
/// model (context line + conflict advisory), the defensive <see cref="BaseRateParser"/>, and the
/// degrade-safe <see cref="ChartLibraryBaseRateProvider"/>. The provider must NEVER throw and must
/// be a clean no-op unless explicitly enabled with a key — that contract is what keeps grounding
/// from ever blocking the trading flow.
/// </summary>
[TestFixture]
public class BaseRateTests
{
    private static readonly DateTime Day = new(2026, 5, 9);
    private const string DayStr = "2026-05-09";

    // A realistic-shape cohort_analyze envelope: raw + conformally calibrated bands per horizon,
    // a cohort_size at the root, and a calibration block on the 5-day horizon.
    private const string SyntheticPayload = """
        {
          "anchor": { "symbol": "NVDA", "date": "2026-05-09" },
          "cohort_size": 348,
          "horizons": {
            "1": {
              "return_pct": { "p10": -2.1, "p50": 0.3, "p90": 2.7 },
              "calibrated_return_pct": { "p10": -2.6, "p50": 0.3, "p90": 3.2 }
            },
            "5": {
              "return_pct": { "p10": -5.4, "p50": 1.2, "p90": 7.1 },
              "calibrated_return_pct": { "p10": -6.8, "p50": 1.4, "p90": 8.9 },
              "calibration": { "empirical_coverage": 0.808, "sample_size": 302880 }
            },
            "10": {
              "return_pct": { "p10": -8.0, "p50": 2.0, "p90": 11.0 },
              "calibrated_return_pct": { "p10": -9.5, "p50": 2.2, "p90": 12.5 }
            }
          }
        }
        """;

    // Only horizons 1 and 10 — used to prove horizon 1 does not bleed into the "10" path segment.
    private const string HorizonCollisionPayload = """
        {
          "cohort_size": 50,
          "horizons": {
            "1":  { "calibrated_return_pct": { "p10": -1.0, "p50": 0.5, "p90": 1.5 } },
            "10": { "calibrated_return_pct": { "p10": -9.0, "p50": 5.0, "p90": 14.0 } }
          }
        }
        """;

    // ---- helpers -------------------------------------------------------

    private static GroundingOptions ActiveOptions(int horizon = 5) => new()
    {
        Enabled = true,
        ApiKey = "test-key",
        Timeframe = "rth",
        Horizons = new[] { 1, 5, 10 },
        OverfitHorizon = horizon,
        ExcludeSameSymbolDays = true
    };

    private static ChartLibraryBaseRateProvider NewProvider(GroundingOptions options, HttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://chartlibrary.test/") };
        var factory = new FakeHttpClientFactory(client);
        return new ChartLibraryBaseRateProvider(factory, options, NullLogger<ChartLibraryBaseRateProvider>.Instance);
    }

    private static BaseRate Rate(double median, int? n = null) => new()
    {
        Asset = "T",
        AsOf = DayStr,
        Horizon = 5,
        MedianPct = median,
        P10Pct = median - 3,
        P90Pct = median + 3,
        N = n,
        Calibrated = true
    };

    /// <summary>Records what the provider actually sent and returns a canned response.</summary>
    private sealed class CapturingHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }
        public string? LastAuthorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastRequest = request;
            LastAuthorization = request.Headers.TryGetValues("Authorization", out var v) ? string.Join(",", v) : null;
            LastBody = request.Content?.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }

    /// <summary>Always throws — exercises the degrade-safe path.</summary>
    private sealed class ThrowingHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            throw new HttpRequestException("boom");
        }
    }

    // ==== BaseRate model ===============================================

    [Test]
    public void ToContextLine_Calibrated_IncludesBandCohortAndCoverage()
    {
        var r = new BaseRate
        {
            Asset = "NVDA", AsOf = DayStr, Horizon = 5,
            MedianPct = 1.4, P10Pct = -6.8, P90Pct = 8.9,
            N = 348, Coverage = 0.808, CoverageN = 302880, Calibrated = true
        };

        var line = r.ToContextLine();

        Assert.That(line, Does.Contain("NVDA"));
        Assert.That(line, Does.Contain("5-day horizon"));
        Assert.That(line, Does.Contain("median +1.4%"));
        Assert.That(line, Does.Contain("calibrated 80% band [-6.8%, +8.9%]"));
        Assert.That(line, Does.Contain("from 348 analogous setups"));
        Assert.That(line, Does.Contain("calibration held 80.8% across 302,880 cases"));
        Assert.That(line, Does.Contain("Context only, not a forecast."));
    }

    [Test]
    public void ToContextLine_RawWithoutCoverageOrCohort_OmitsThoseClauses()
    {
        var r = new BaseRate
        {
            Asset = "AAPL", AsOf = "2026-01-02", Horizon = 10,
            MedianPct = -0.3, P10Pct = -5.0, P90Pct = 4.0,
            Calibrated = false
        };

        var line = r.ToContextLine();

        Assert.That(line, Does.Contain("raw 80% band [-5.0%, +4.0%]"));
        Assert.That(line, Does.Contain("median -0.3%"));
        Assert.That(line, Does.Not.Contain("analogous setups"));
        Assert.That(line, Does.Not.Contain("calibration held"));
    }

    [Test]
    public void AssessConflict_BullishAgainstNegativeMedian_ReturnsNoteWithCohort()
    {
        var note = Rate(median: -2.0, n: 100).AssessConflict("bullish");

        Assert.That(note, Is.Not.Null);
        Assert.That(note, Does.Contain("runs counter"));
        Assert.That(note, Does.Contain("this bullish call"));
        Assert.That(note, Does.Contain("across 100 analogous setups"));
        Assert.That(note, Does.Contain("the signal above is unchanged"));
    }

    [Test]
    public void AssessConflict_BearishAgainstPositiveMedian_ReturnsNote()
    {
        var note = Rate(median: 2.0).AssessConflict("SELL");

        Assert.That(note, Is.Not.Null);
        Assert.That(note, Does.Contain("this sell call"));
    }

    [Test]
    public void AssessConflict_SignalAgreesWithBaseRate_ReturnsNull()
    {
        Assert.That(Rate(median: 2.0).AssessConflict("bullish"), Is.Null);
        Assert.That(Rate(median: -2.0).AssessConflict("bearish"), Is.Null);
    }

    [Test]
    public void AssessConflict_FlatMedian_ReturnsNull()
    {
        Assert.That(Rate(median: 0.05).AssessConflict("bearish"), Is.Null);
        Assert.That(Rate(median: -0.09).AssessConflict("bullish"), Is.Null);
    }

    [Test]
    public void AssessConflict_NonDirectionalOrMissingSignal_ReturnsNull()
    {
        Assert.That(Rate(median: -5.0).AssessConflict("hold"), Is.Null);
        Assert.That(Rate(median: -5.0).AssessConflict(null), Is.Null);
        Assert.That(Rate(median: -5.0).AssessConflict(""), Is.Null);
        Assert.That(Rate(median: -5.0).AssessConflict("neutral"), Is.Null);
    }

    [Test]
    public void AssessConflict_SignalSynonyms_AreCaseInsensitive()
    {
        Assert.That(Rate(median: -3.0).AssessConflict("LONG"), Is.Not.Null);
        Assert.That(Rate(median: -3.0).AssessConflict("Buy"), Is.Not.Null);
        Assert.That(Rate(median: 3.0).AssessConflict("short"), Is.Not.Null);
    }

    // ==== BaseRateParser ===============================================

    [Test]
    public void Parse_PrefersCalibratedBand_MatchesHorizon_AndPullsCohortAndCoverage()
    {
        var r = BaseRateParser.Parse(SyntheticPayload, "NVDA", DayStr, horizon: 5);

        Assert.That(r, Is.Not.Null);
        Assert.That(r!.Asset, Is.EqualTo("NVDA"));
        Assert.That(r.AsOf, Is.EqualTo(DayStr));
        Assert.That(r.Horizon, Is.EqualTo(5));
        Assert.That(r.Calibrated, Is.True);
        // Calibrated 5-day p50 = 1.4 (not the raw 1.2, not the 1-day or 10-day band).
        Assert.That(r.MedianPct, Is.EqualTo(1.4).Within(1e-9));
        Assert.That(r.P10Pct, Is.EqualTo(-6.8).Within(1e-9));
        Assert.That(r.P90Pct, Is.EqualTo(8.9).Within(1e-9));
        Assert.That(r.N, Is.EqualTo(348));
        Assert.That(r.Coverage, Is.EqualTo(0.808).Within(1e-9));
        Assert.That(r.CoverageN, Is.EqualTo(302880));
    }

    [Test]
    public void Parse_HorizonOne_DoesNotMatchTheTenSegment()
    {
        var r = BaseRateParser.Parse(HorizonCollisionPayload, "NVDA", DayStr, horizon: 1);

        Assert.That(r, Is.Not.Null);
        // Must pick the "1" block (median 0.5), never the "10" block (median 5.0).
        Assert.That(r!.MedianPct, Is.EqualTo(0.5).Within(1e-9));
    }

    [Test]
    public void Parse_RawOnlyPayload_ReportsNotCalibrated()
    {
        const string rawOnly = """
            { "cohort_size": 20, "horizons": { "5": { "return_pct": { "p10": -4.0, "p50": 0.8, "p90": 5.0 } } } }
            """;

        var r = BaseRateParser.Parse(rawOnly, "MSFT", DayStr, horizon: 5);

        Assert.That(r, Is.Not.Null);
        Assert.That(r!.Calibrated, Is.False);
        Assert.That(r.MedianPct, Is.EqualTo(0.8).Within(1e-9));
        Assert.That(r.N, Is.EqualTo(20));
    }

    [Test]
    public void Parse_DistributionWithMeanButNoMedian_UsesMeanAsCenter()
    {
        const string meanOnly = """
            { "horizons": { "5": { "calibrated_return_pct": { "p10": -2.0, "mean": 0.9, "p90": 3.0 } } } }
            """;

        var r = BaseRateParser.Parse(meanOnly, "TSLA", DayStr, horizon: 5);

        Assert.That(r, Is.Not.Null);
        Assert.That(r!.MedianPct, Is.EqualTo(0.9).Within(1e-9));
    }

    [Test]
    public void Parse_EmptyOrWhitespaceJson_ReturnsNull()
    {
        Assert.That(BaseRateParser.Parse("", "NVDA", DayStr, 5), Is.Null);
        Assert.That(BaseRateParser.Parse("   ", "NVDA", DayStr, 5), Is.Null);
    }

    [Test]
    public void Parse_MalformedJson_ReturnsNull()
    {
        Assert.That(BaseRateParser.Parse("{not valid json", "NVDA", DayStr, 5), Is.Null);
    }

    [Test]
    public void Parse_NoDistributionPresent_ReturnsNull()
    {
        Assert.That(BaseRateParser.Parse("""{ "foo": { "bar": 1 } }""", "NVDA", DayStr, 5), Is.Null);
    }

    // ==== ChartLibraryBaseRateProvider =================================

    [Test]
    public void Provider_WhenDisabled_IsNoOp_AndNeverCallsHttp()
    {
        var options = new GroundingOptions { Enabled = false, ApiKey = "test-key" }; // Active == false
        var handler = new CapturingHandler(HttpStatusCode.OK, SyntheticPayload);
        var provider = NewProvider(options, handler);

        Assert.That(provider.Enabled, Is.False);

        var ok = provider.TryGetBaseRate("NVDA", Day, out var br);

        Assert.That(ok, Is.False);
        Assert.That(br, Is.Null);
        Assert.That(handler.Calls, Is.EqualTo(0));
    }

    [Test]
    public void Provider_WhenEnabledWithoutKey_IsNoOp()
    {
        var options = new GroundingOptions { Enabled = true, ApiKey = null }; // HasKey == false
        var handler = new CapturingHandler(HttpStatusCode.OK, SyntheticPayload);
        var provider = NewProvider(options, handler);

        Assert.That(provider.Enabled, Is.False);
        Assert.That(provider.TryGetBaseRate("NVDA", Day, out _), Is.False);
        Assert.That(handler.Calls, Is.EqualTo(0));
    }

    [Test]
    public void Provider_EmptySymbol_ReturnsFalse_WithoutHttp()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, SyntheticPayload);
        var provider = NewProvider(ActiveOptions(), handler);

        var ok = provider.TryGetBaseRate("   ", Day, out var br);

        Assert.That(ok, Is.False);
        Assert.That(br, Is.Null);
        Assert.That(handler.Calls, Is.EqualTo(0));
    }

    [Test]
    public void Provider_GoodResponse_ReturnsParsedBaseRate_AndUppercasesSymbol()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, SyntheticPayload);
        var provider = NewProvider(ActiveOptions(horizon: 5), handler);

        var ok = provider.TryGetBaseRate("nvda", Day, out var br);

        Assert.That(ok, Is.True);
        Assert.That(br, Is.Not.Null);
        Assert.That(br!.Asset, Is.EqualTo("NVDA"));
        Assert.That(br.AsOf, Is.EqualTo(DayStr));
        Assert.That(br.Horizon, Is.EqualTo(5));
        Assert.That(br.MedianPct, Is.EqualTo(1.4).Within(1e-9));
        Assert.That(br.Calibrated, Is.True);
        Assert.That(br.N, Is.EqualTo(348));
        Assert.That(handler.Calls, Is.EqualTo(1));
    }

    [Test]
    public void Provider_SendsBearerKeyAndPostsCohortAnalyzeBody()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, SyntheticPayload);
        var provider = NewProvider(ActiveOptions(), handler);

        provider.TryGetBaseRate("NVDA", Day, out _);

        Assert.That(handler.LastAuthorization, Is.EqualTo("Bearer test-key"));
        Assert.That(handler.LastRequest!.Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(handler.LastRequest.RequestUri!.AbsolutePath, Does.EndWith("/api/v1/cohort_analyze"));
        Assert.That(handler.LastBody, Does.Contain("\"symbol\":\"NVDA\""));
        Assert.That(handler.LastBody, Does.Contain("\"date\":\"2026-05-09\""));
        Assert.That(handler.LastBody, Does.Contain("\"timeframe\":\"rth\""));
        Assert.That(handler.LastBody, Does.Contain("\"horizons\":[1,5,10]"));
        Assert.That(handler.LastBody, Does.Contain("\"exclude_same_symbol_days\":true"));
    }

    [Test]
    public void Provider_NonSuccessStatus_DegradesToFalseNull()
    {
        var handler = new CapturingHandler(HttpStatusCode.InternalServerError, "{}");
        var provider = NewProvider(ActiveOptions(), handler);

        var ok = provider.TryGetBaseRate("NVDA", Day, out var br);

        Assert.That(ok, Is.False);
        Assert.That(br, Is.Null);
        Assert.That(handler.Calls, Is.EqualTo(1));
    }

    [Test]
    public void Provider_WhenHttpThrows_NeverThrows_AndReturnsFalseNull()
    {
        var handler = new ThrowingHandler();
        var provider = NewProvider(ActiveOptions(), handler);

        var ok = true;
        BaseRate? br = null;
        Assert.DoesNotThrow(() => ok = provider.TryGetBaseRate("NVDA", Day, out br));

        Assert.That(ok, Is.False);
        Assert.That(br, Is.Null);
        Assert.That(handler.Calls, Is.EqualTo(1));
    }

    [Test]
    public void Provider_CachesPositiveResult_HitsEndpointAtMostOnce()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, SyntheticPayload);
        var provider = NewProvider(ActiveOptions(), handler);

        provider.TryGetBaseRate("NVDA", Day, out var first);
        provider.TryGetBaseRate("NVDA", Day, out var second);

        Assert.That(handler.Calls, Is.EqualTo(1));
        Assert.That(second, Is.SameAs(first));
    }

    [Test]
    public void Provider_CachesNegativeResult_DoesNotRetryWithinRun()
    {
        var handler = new CapturingHandler(HttpStatusCode.InternalServerError, "{}");
        var provider = NewProvider(ActiveOptions(), handler);

        provider.TryGetBaseRate("NVDA", Day, out _);
        provider.TryGetBaseRate("NVDA", Day, out _);

        Assert.That(handler.Calls, Is.EqualTo(1));
    }
}
