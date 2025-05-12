namespace AiHedgeFund.Data.AlphaVantage;

public class AlphaVantageAuthHandler(string? apiKey) : DelegatingHandler
{
    private readonly string _apiKey = apiKey
                                      ?? throw new ArgumentNullException(nameof(apiKey), "Alpha Vantage API key is required");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri;

        if (uri is not null)
        {
            var uriBuilder = new UriBuilder(uri);
            var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
            query["apikey"] = _apiKey;
            uriBuilder.Query = query.ToString();
            request.RequestUri = uriBuilder.Uri;
        }

        return await base.SendAsync(request, cancellationToken);
    }
}