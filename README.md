[![Build, Test, and Publish NuGet Packages](https://github.com/riccardone/ai-hedge-fund-net/actions/workflows/release.yml/badge.svg)](https://github.com/riccardone/ai-hedge-fund-net/actions/workflows/release.yml)

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

This is an example of output for NVidia using warren_buffett and cathie_wood agents ![image](https://github.com/user-attachments/assets/a56c89b4-a86c-4299-8645-2d10177f2dc9)

---
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

| Flag | Default | Description |
|---|---|---|
| `--agent` | _(required)_ | One or more agent names (snake_case) |
| `--tickers` | _(required)_ | One or more stock symbols |
| `--start-date` | 3 months ago | Price data window start |
| `--end-date` | Today | Price data window end |
| `--risk-level` | `medium` | `low`, `medium`, or `high` |
| `--model` | `gpt-4o-mini` | OpenAI model (Tier 1 keys support `gpt-4o-mini` and `gpt-4o`) |

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

---

## Cutting a Release

Releases are published automatically by GitHub Actions when a version tag is pushed. The workflow builds the solution, runs all tests, packs and publishes the NuGet packages (`AiHedgeFund.Contracts`, `AiHedgeFund.Agents`, `AiHedgeFund.Data`), and attaches self-contained console binaries (Linux, Windows, macOS) to a GitHub Release.

Use the release script from Git Bash:

```bash
# Bump patch automatically (e.g. v0.2.11 → v0.2.12)
./scripts/release.sh

# Or supply an explicit version
./scripts/release.sh v0.3.0
```

The script will:
1. Determine the next patch version from the latest semver tag (or use the version you pass).
2. Refuse to proceed if the working tree is dirty or the tag already exists.
3. Create an annotated tag and push it — which triggers the CI release pipeline.

> **Prerequisites:** `NUGET_API_KEY` must be set as a GitHub Actions secret in the repository before the first publish.

---

## Credits

This .NET project is loosely inspired by the [ai-hedge-fund](https://github.com/virattt/ai-hedge-fund) project written in Python.
