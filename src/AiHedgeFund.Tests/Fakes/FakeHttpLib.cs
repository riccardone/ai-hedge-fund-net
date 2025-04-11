using AiHedgeFund.Contracts;

namespace AiHedgeFund.Tests.Fakes;

public class FakeHttpLib : IHttpLib
{
    public bool TryPost(string path, string payload, out string response)
    {
        response = @"
{
  ""id"": ""chatcmpl-abc123"",
  ""object"": ""chat.completion"",
  ""created"": 1680000000,
  ""model"": ""gpt-4"",
  ""choices"": [
    {
      ""index"": 0,
      ""message"": {
        ""role"": ""assistant"",
        ""content"": ""{\""Ticker\"":\""AAPL\"",\""Signal\"":\""buy\"",\""Confidence\"":85,\""Reasoning\"":\""Strong earnings growth, high ROE, and discounted valuation.\""}""
      },
      ""finish_reason"": ""stop""
    }
  ],
  ""usage"": {
    ""prompt_tokens"": 50,
    ""completion_tokens"": 50,
    ""total_tokens"": 100
  }
}";

        return true;
    }
}