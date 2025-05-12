# ai-hedge-fund-net

**AiHedgeFund.Agents** is a .NET alghoritmic library that provides trading signals by analyzing stocks using multiple AI agents. Each agent applies a different investment philosophy to decide whether a stock is a **bullish**, **neutral**, or **bearish**. Agents also provide their **reasoning**, **confidence score**, **key metrics**, and **specific rules** behind each decision.

Currently, the following agents are implemented:

- `charlie_munger` (quality + management judgment)
- `stanley_druckenmiller` (macro, momentum, sentiment)
- `ben_graham`  (deep value, margin of safety, balance sheet strength)
- `cathie_wood` (innovation, tech disruption)
- `bill_ackman` (activist investing, risk arbitrage)
- `warren_buffett` (value investing, moat, long-term)

Each agent integrates with an LLM (Large Language Model) trained for financial reasoning to generate the insights behind its signals.

This is an example of output for NVidia using warren_buffett and cathie_wood agents ![image](https://github.com/user-attachments/assets/a56c89b4-a86c-4299-8645-2d10177f2dc9)

---
## install this nuget into your program

1. in your client program install the package with the agents

```bash
PM> Install-Package AiHedgeFund.Agents   
```

2. install the package with the service reading financial data and dealing with an AI LLM

```bash
PM> Install-Package AiHedgeFund.Data 
```

---
## Configuration

This project uses **Alpha Vantage** as the financial data provider. I have no affiliation or sponsorship with them—it simply happened that I created a free API key there and stuck with it. Once the initial porting and development phase is complete, I plan to support additional providers by implementing the `IDataReader` interface.

To use Alpha Vantage:

1. Get your free API key from: [https://www.alphavantage.co/support/#api-key](https://www.alphavantage.co/support/#api-key)
2. Add the key to your `appsettings.json` file.

For LLM-based reasoning, **OpenAI** is used:

1. Get your API key from: [https://platform.openai.com/account/api-keys](https://platform.openai.com/account/api-keys)
2. Add it to `appsettings.json`.

Example `appsettings.json`:

```json
{
  "OpenAI": {
    "ApiKey": "your-openai-api-key"
  },
  "AlphaVantage": {
    "ApiKey": "your-alpha-vantage-api-key"
  }
}
```