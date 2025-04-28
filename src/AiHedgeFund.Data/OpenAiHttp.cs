using System.Text;
using AiHedgeFund.Contracts;
using Microsoft.Extensions.Logging;

namespace AiHedgeFund.Data;

public class OpenAiHttp : IHttpLib
{
    private readonly ILogger<OpenAiHttp> _logger;
    private readonly HttpClient _client;

    public OpenAiHttp(IHttpClientFactory clientFactory, ILogger<OpenAiHttp> logger)
    {
        _logger = logger;
        _client = clientFactory.CreateClient("OpenAI");
    }

    public bool TryPost(string path, string payload, out string response)
    {
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var responseMessage = _client.PostAsync(path, content).Result;
        response = responseMessage.Content.ReadAsStringAsync().Result;

        if (responseMessage.IsSuccessStatusCode)
            return true;
        _logger.LogError($"Request failed for {path}: {responseMessage.StatusCode} - {response} ");
        return false;
    }
}