namespace ai_hedge_fund_net.Data.AlphaVantage;

public class AlphaVantageAuthHandler : DelegatingHandler
{
    private readonly string _apiKey;

    public AlphaVantageAuthHandler(string apiKey)
    {
        _apiKey = apiKey;
    }

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