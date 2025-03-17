using System.Text;

namespace ai_hedge_fund_net.Agents;

public class HttpService : IHttpService
{
    private readonly HttpClient _client;

    public HttpService(HttpClient client)
    {
        _client = client;
    }

    public bool TryPost(string path, string payload, out string response)
    {
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var responseMessage = _client.PostAsync(path, content).Result;
        response = responseMessage.Content.ReadAsStringAsync().Result;

        if (responseMessage.IsSuccessStatusCode)
            return true;
        throw new Exception($"Request failed: {responseMessage.StatusCode} - {response} ");
    }
}