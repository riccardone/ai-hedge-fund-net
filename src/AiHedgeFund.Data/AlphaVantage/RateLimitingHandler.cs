namespace AiHedgeFund.Data.AlphaVantage;

/// <summary>
/// Enforces the Alpha Vantage free-tier constraint of 1 request per second.
/// Requests that arrive before the minimum interval has elapsed are held until the slot opens.
/// </summary>
public class RateLimitingHandler(TimeSpan? interval = null) : DelegatingHandler
{
    private readonly TimeSpan _minInterval = interval ?? TimeSpan.FromSeconds(1);
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private DateTimeOffset _lastRequestAt = DateTimeOffset.MinValue;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var elapsed = DateTimeOffset.UtcNow - _lastRequestAt;
            if (elapsed < _minInterval)
                await Task.Delay(_minInterval - elapsed, cancellationToken);

            _lastRequestAt = DateTimeOffset.UtcNow;
        }
        finally
        {
            _semaphore.Release();
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
