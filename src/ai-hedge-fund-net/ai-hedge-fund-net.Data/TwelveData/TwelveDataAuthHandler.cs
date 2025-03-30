using Microsoft.Extensions.Configuration;

namespace ai_hedge_fund_net.Data.TwelveData;

public class TwelveDataAuthHandler : DelegatingHandler
{
    private readonly string _apiKey;

    public TwelveDataAuthHandler(string apiKey)
    {
        _apiKey = apiKey;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uriBuilder = new UriBuilder(request.RequestUri!);
        var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
        query["apikey"] = _apiKey;
        uriBuilder.Query = query.ToString();
        request.RequestUri = uriBuilder.Uri;

        return base.SendAsync(request, cancellationToken);
    }
}