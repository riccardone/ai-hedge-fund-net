## Download and Run the Program

1. Go to the **Releases** section and download the latest release.
2. Run the program with `--help` or `-h` to see usage instructions:

```bash
> AiHedgeFund.Console --help
```

3. Run the program by specifying one or more agents and one or more stock tickers:

```bash
> AiHedgeFund.Console --agent cathie_wood ben_graham --tickers MSFT AAPL
```

---

# ai-hedge-fund-net

**ai-hedge-fund-net** is a .NET alghoritmic program that provides trading signals by analyzing stocks using multiple AI agents. Each agent applies a different investment philosophy to decide whether a stock is a **buy**, **hold**, or **sell**. Agents also provide their **reasoning**, **confidence score**, **key metrics**, and **specific rules** behind each decision.

Currently, the following agents are implemented:

- `charlie_munger` (quality + management judgment)
- `stanley_druckenmiller` (macro, momentum, sentiment)
- `ben_graham`  (deep value, margin of safety, balance sheet strength)
- `cathie_wood` (innovation, tech disruption)
- `bill_ackman` (activist investing, risk arbitrage)
- `warren_buffett` (value investing, moat, long-term)

Each agent integrates with an LLM (Large Language Model) trained for financial reasoning to generate the insights behind its signals.

This .NET project is inspired by the [ai-hedge-fund](https://github.com/virattt/ai-hedge-fund) project written in Python.

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

---

## Cache

All financial data is fetched from the remote API **once**, then serialized to disk and cached in memory. On subsequent runs with the same tickers, the program will use the cached data instead of re-fetching it.

To **force a data refresh**, manually delete the `data` folder located in the same directory as the program. A command-line parameter to automate this will be added soon.
