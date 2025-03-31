using System.Text;
using AiHedgeFund.Contracts;

namespace AiHedgeFund.Data;

public class OpenAiChatter : IChatter
{
    private readonly HttpClient _client;

    public OpenAiChatter(IHttpClientFactory clientFactory)
    {
        _client = clientFactory.CreateClient("OpenAI");
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