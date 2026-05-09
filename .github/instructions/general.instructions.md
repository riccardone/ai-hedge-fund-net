# Copilot Instructions for ai-hedge-fund-net

## Overview

**ai-hedge-fund-net** is a .NET algorithmic program that generates trading signals by analysing stocks using multiple AI agents. Each agent applies a distinct investment philosophy (value investing, growth, macro, activism, etc.) to produce a **bullish / bearish / neutral** signal for each ticker, together with a confidence score and human-readable reasoning powered by an LLM.

---

## Solution Structure

```
src/
  AiHedgeFund.Agents/      — Agent implementations + shared services (LLM generation, portfolio management, registry)
  AiHedgeFund.Api/         — ASP.NET Core project (stub; future REST API surface)
  AiHedgeFund.Console/     — Entry point: CLI host, DI wiring, ConsoleOutputFormatter
  AiHedgeFund.Contracts/   — Shared interfaces, domain models (TradeSignal, TradingWorkflowState, IDataReader, etc.)
  AiHedgeFund.Data/        — Data fetching: AlphaVantage client, file + memory cache, mock provider
  AiHedgeFund.Tests/       — NUnit unit tests; Fakes for IHttpLib and FinancialMetrics
```

---

## Request / Workflow Flow

```
CLI args (--agent, --tickers, --risk-level, --start-date, --end-date)
  → AppArguments (parsed in Program.cs)
  → TradingInitializer.InitializeAsync()
      → IDataReader.TryGet* (financial metrics, line items, prices, news) per ticker
      → populates TradingWorkflowState
  → PortfolioManager.Evaluate(agentName, state) per selected agent
      → AgentRegistry.TryGet(agentName) → agent.Run(state)
      → agent scores financial data → LlmTradeSignalGenerator.TryGenerateSignal()
      → state.AddOrUpdateAgentReport<TAgent>(tradeSignal, analysisResults)
  → RiskManagerAgent.Run(state)
      → computes RiskAssessment per ticker and attaches to each TradeSignal
  → ConsoleOutputFormatter.PrintAgentReport() per agent/ticker
```

---

## Agents

| Class | Registry key | Philosophy |
|---|---|---|
| `BenGrahamAgent` | `ben_graham` | Deep value, margin of safety, balance sheet strength |
| `CathieWoodAgent` | `cathie_wood` | Innovation, tech disruption, high growth |
| `BillAckmanAgent` | `bill_ackman` | Activist investing, concentrated positions |
| `CharlieMungerAgent` | `charlie_munger` | Quality companies, management judgment, ROIC |
| `StanleyDruckenmillerAgent` | `stanley_druckenmiller` | Macro, momentum, market sentiment |
| `WarrenBuffettAgent` | `warren_buffett` | Value investing, economic moat, owner earnings |
| `RiskManagerAgent` | _(not in registry)_ | Portfolio-level risk assessment; always runs last |

### Agent conventions

- Every analyst agent exposes a single `Run(TradingWorkflowState state)` method.
- Agents compute one or more `FinancialAnalysisResult` objects (score + maxScore + string details list).
- After scoring, agents call `LlmTradeSignalGenerator.TryGenerateSignal(...)` to get the LLM-generated signal.
- On success, call `state.AddOrUpdateAgentReport<TAgentClass>(tradeSignal, analysisResults)` — the generic type parameter drives the snake_case registry key and the display name.
- `RiskManagerAgent` does **not** extend the same pattern — it iterates `state.Tickers`, reads `state.Portfolio`, and writes `state.RiskAssessments`.

### Adding a new agent

1. Create `<Name>Agent.cs` in `AiHedgeFund.Agents/` implementing `Run(TradingWorkflowState)`.
2. Register it in `AgentBootstrapper.StartAsync()` using `_registry.Register(nameof(<Name>Agent).ToSnakeCase(), _<name>.Run)`.
3. Inject it into `AgentBootstrapper` and `Program.cs` via DI (`services.AddSingleton<TAgent>()`).
4. Add the snake_case name to `AppArguments.AvailableAgents`.

---

## Key Types (Contracts)

| Type | Purpose |
|---|---|
| `TradingWorkflowState` | Central state bag flowing through the entire pipeline; holds tickers, dates, data, signals, risk assessments |
| `TradeSignal` | Signal, confidence, reasoning and optional RiskAssessment for one ticker |
| `FinancialAnalysisResult` | Named scoring category: score, maxScore, list of detail strings |
| `FinancialMetrics` | Per-ticker, per-period financial ratios (ROE, D/E, operating margin, etc.) |
| `FinancialLineItem` | Flexible key-value financial data per ticker/period (uses `Dictionary<string, dynamic>`) |
| `RiskAssessment` | Max position size, current price, portfolio reasoning |
| `IDataReader` | Interface for all financial data retrieval — implement to add new data providers |
| `IAgentRegistry` | Registry for agent lookup by name |
| `IHttpLib` | Abstraction for HTTP calls to LLMs — implement `FakeHttpLib` in tests |

---

## Data Layer

- **AlphaVantage** is the default `IDataReader` implementation.
- `DataFetcher` provides a two-level cache: file cache (`FileDataManager`, stored in `data/` folder next to the binary) and in-memory `ConcurrentDictionary`. Data is fetched from Alpha Vantage once, serialized to disk, and served from cache on subsequent runs.
- To force a full data refresh, delete the `data/` folder.
- `AlphaVantageAuthHandler` injects the API key as a query parameter on every outbound request.
- `IPriceVolumeProvider` is a seam for price volume data; currently satisfied by `FakePriceVolumeProvider`.
- To add a new data provider, implement `IDataReader` and register it in `Program.cs`.

---

## LLM Integration

- `IHttpLib` / `OpenAiHttp` wraps synchronous HTTP calls to the OpenAI Chat Completions endpoint.
- `LlmTradeSignalGenerator.TryGenerateSignal(...)` is the single shared utility used by all agents:
  - Accepts a `systemMessage` (agent persona and rules) and `analysisData` (anonymous object serialised as JSON).
  - Sends a request to `gpt-4` with `temperature = 0.2`.
  - Parses the JSON response (`signal`, `confidence`, `reasoning`).
  - Returns `false` and a neutral fallback `TradeSignal` on any failure — agents should log errors but continue.
- Never call the LLM directly in an agent; always go through `LlmTradeSignalGenerator`.

---

## Configuration

`appsettings.json` (and `appsettings.{env}.json` for overrides):

```json
{
  "OpenAI": { "ApiKey": "..." },
  "AlphaVantage": { "ApiKey": "..." }
}
```

Environment is resolved from `ASPNETCORE_ENVIRONMENT` (defaults to `"dev"`). Never commit real API keys — use `appsettings.dev.json` (gitignored) or environment variables.

---

## CLI Usage

```bash
AiHedgeFund.Console --agent warren_buffett cathie_wood --tickers MSFT AAPL --risk-level medium
AiHedgeFund.Console --agent ben_graham --tickers NVDA --start-date 2024-01-01 --end-date 2024-12-31 --risk-level low
AiHedgeFund.Console --help
```

| Flag | Default | Notes |
|---|---|---|
| `--agent` | _(required)_ | One or more snake_case agent names |
| `--tickers` | _(required)_ | One or more stock symbols |
| `--start-date` | 3 months ago | Price data window start |
| `--end-date` | Today | Price data window end |
| `--risk-level` | `medium` | `low`, `medium`, or `high` — affects scoring thresholds and DCF assumptions |

---

## Build & Test

```sh
# Build
dotnet build src/AiHedgeFund.sln

# Run all tests
dotnet test src/AiHedgeFund.sln

# Run a specific test
dotnet test src/AiHedgeFund.sln --filter "FullyQualifiedName~CharlieMungerAgentTests.RunPositiveInput"

# Run the console
dotnet run --project src/AiHedgeFund.Console -- --agent warren_buffett --tickers AAPL
```

Tests use **NUnit**. Fakes (`FakeHttpLib`, `FinancialMetricsFactory`) live in `AiHedgeFund.Tests/Fakes/`. Tests must not call real external APIs.

---

## Best Practices

- **Agent scoring is pure C#** — no I/O, no LLM calls inside scoring methods. LLM is only invoked in `TryGenerateOutput` / `TryGenerateSignal`.
- **RiskLevel** (`Low` / `Medium` / `High`) adjusts scoring thresholds and DCF parameters within each agent — always respect it.
- **`FinancialAnalysisResult`** must always carry a descriptive name, a non-negative score, and a maxScore ≥ score. The details list should explain every scoring decision.
- **`TradingWorkflowState` is the single source of truth** across the pipeline — never pass partial state between agents.
- **Agent registry keys are snake_case** — derived from class name via `ToSnakeCase()`. CLI args must match exactly.
- **Do not add `async`/`await` to agent `Run()` methods** — agents are designed as synchronous compute; if a future agent needs async I/O, update the full call chain.
- **Null-safe financial data access** — financial metrics may have `null` values; always use `.HasValue` checks and log warnings rather than throwing.
- **Cache invalidation** is manual (delete `data/` folder) — document any new cached keys added in `AlphaVantageDataReader`.
