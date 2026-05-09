using System.Net;
using AiHedgeFund.Data;
using AiHedgeFund.Data.AlphaVantage;
using AiHedgeFund.Tests.Fakes;
using Microsoft.Extensions.Logging;

namespace AiHedgeFund.Tests;

/// <summary>
/// Reproduces the stale-cache bug: when Alpha Vantage returns an error response during a
/// rate-limit event, the deserialized raw object (all-null / empty fields) was being saved
/// to disk and later read back as if it were valid data.
///
/// Fix: DataFetcher.TryLoadOrFetch accepts an optional isRawValid delegate. If the cached
/// raw object fails the validator the cache file is deleted and the data is re-fetched.
/// </summary>
[TestFixture]
public class DataFetcherCacheValidationTests
{
    private static readonly ILogger<DataFetcher> NullLogger =
        LoggerFactory.Create(_ => { }).CreateLogger<DataFetcher>();

    // ---- helpers -------------------------------------------------------

    private static FileDataManager TempFileManager(out string dir)
    {
        dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        return new FileDataManager(dir);
    }

    private static DataFetcher MakeFetcher(FileDataManager fileManager, HttpStatusCode httpStatus, string httpBody)
    {
        var factory = FakeHttpClientFactory.WithResponse(httpStatus, httpBody);
        return new DataFetcher(factory, fileManager, NullLogger);
    }

    // ---- tests ---------------------------------------------------------

    /// <summary>
    /// RED → GREEN: when the cache holds an empty TimeSeries (stale error response)
    /// and the validator rejects it, TryLoadOrFetch must ignore the cache and re-fetch.
    /// When the re-fetch also fails (503), the method must return false.
    /// </summary>
    [Test]
    public void TryLoadOrFetch_WhenCachedTimeSeriesIsEmpty_AndRefetchFails_ReturnsFalse()
    {
        var fileManager = TempFileManager(out var dir);
        try
        {
            // Seed cache with bad data: empty TimeSeries (what gets cached from an AV rate-limit response)
            fileManager.Save(new TimeSeriesDailyResponse(), "daily_NVDA");

            var fetcher = MakeFetcher(fileManager, HttpStatusCode.ServiceUnavailable, "{}");

            var result = fetcher.TryLoadOrFetch<TimeSeriesDailyResponse, List<string>>(
                "daily_NVDA",
                "query?function=TIME_SERIES_DAILY&symbol=NVDA",
                raw => raw.TimeSeries.Keys.ToList(),
                out var mapped,
                isRawValid: raw => raw.TimeSeries.Count > 0);  // validator rejects empty dict

            Assert.That(result, Is.False, "Should not treat an empty TimeSeries as valid");
            Assert.That(mapped, Is.Null);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>
    /// RED → GREEN: when the cache holds an empty TimeSeries and the re-fetch returns
    /// valid JSON, TryLoadOrFetch must return true with the freshly-fetched data.
    /// </summary>
    [Test]
    public void TryLoadOrFetch_WhenCachedTimeSeriesIsEmpty_AndRefetchSucceeds_ReturnsFreshData()
    {
        var fileManager = TempFileManager(out var dir);
        try
        {
            fileManager.Save(new TimeSeriesDailyResponse(), "daily_NVDA");

            const string validJson = """
                {"Time Series (Daily)": {
                    "2026-05-09": {"1. open":"100","2. high":"105","3. low":"98","4. close":"102"},
                    "2026-05-08": {"1. open":"99", "2. high":"103","3. low":"97","4. close":"101"}
                }}
                """;

            var fetcher = MakeFetcher(fileManager, HttpStatusCode.OK, validJson);

            var result = fetcher.TryLoadOrFetch<TimeSeriesDailyResponse, List<string>>(
                "daily_NVDA",
                "query?function=TIME_SERIES_DAILY&symbol=NVDA",
                raw => raw.TimeSeries.Keys.ToList(),
                out var dates,
                isRawValid: raw => raw.TimeSeries.Count > 0);

            Assert.That(result, Is.True, "Should succeed after re-fetching valid data");
            Assert.That(dates, Has.Count.EqualTo(2));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>
    /// RED → GREEN: a BalanceSheetRaw with null Symbol (cached AV error response) must
    /// be treated as invalid and trigger a re-fetch.
    /// </summary>
    [Test]
    public void TryLoadOrFetch_WhenCachedBalanceSheetHasNullSymbol_RejectsAndReturnsFailure()
    {
        var fileManager = TempFileManager(out var dir);
        try
        {
            fileManager.Save(new BalanceSheetRaw { Symbol = null! }, "BALANCE_SHEET_NVDA");

            var fetcher = MakeFetcher(fileManager, HttpStatusCode.ServiceUnavailable, "{}");

            var result = fetcher.TryLoadOrFetch<BalanceSheetRaw, BalanceSheet>(
                "BALANCE_SHEET_NVDA",
                "query?function=BALANCE_SHEET&symbol=NVDA",
                BalanceSheetMapper.Map,
                out _,
                isRawValid: raw => raw.Symbol != null);

            Assert.That(result, Is.False, "Null-symbol BalanceSheetRaw (from a cached error response) must be rejected");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>
    /// Regression: when the cache holds valid data the validator must pass and
    /// TryLoadOrFetch must NOT delete the file or re-fetch.
    /// </summary>
    [Test]
    public void TryLoadOrFetch_WhenCachedTimeSeriesIsValid_DoesNotRefetch()
    {
        var fileManager = TempFileManager(out var dir);
        try
        {
            var goodData = new TimeSeriesDailyResponse();
            goodData.TimeSeries["2026-05-09"] = new Dictionary<string, string>
            {
                ["1. open"] = "100", ["2. high"] = "105", ["3. low"] = "98", ["4. close"] = "102"
            };
            fileManager.Save(goodData, "daily_NVDA");

            // HTTP is broken — if it's called the test would fail anyway, but we verify it isn't
            var fetcher = MakeFetcher(fileManager, HttpStatusCode.InternalServerError, "{}");

            var result = fetcher.TryLoadOrFetch<TimeSeriesDailyResponse, List<string>>(
                "daily_NVDA",
                "query?function=TIME_SERIES_DAILY&symbol=NVDA",
                raw => raw.TimeSeries.Keys.ToList(),
                out var dates,
                isRawValid: raw => raw.TimeSeries.Count > 0);

            Assert.That(result, Is.True, "Valid cached data must be returned directly without re-fetching");
            Assert.That(dates, Has.Count.EqualTo(1));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
