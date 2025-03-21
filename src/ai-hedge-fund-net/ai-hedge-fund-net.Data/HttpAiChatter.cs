﻿using System.Text;
using ai_hedge_fund_net.Contracts;

namespace ai_hedge_fund_net.Data;

public class HttpAiChatter : IChatter
{
    private readonly HttpClient _client;

    public HttpAiChatter(HttpClient client)
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