# ai-hedge-fund-net
This is a program that provide trading signals analysing stocks. There are trading agents applying different principles to decide if a stock is a buy, hold or sell. The agents will also provide the reasoning behind, the confidence, the metrics and the specific rules.  
Currently these are the configured agents: "charlie_munger", "stanley_druckenmiller", "ben_graham", "cathie_wood", "bill_ackman", "warren_buffett". As part of the analysis, each agent will integrate with an LLM trained for trading providing the reasoning behind signals.
This .Net project is inspired by the ai-hedge-fund project written in pyton https://github.com/virattt/ai-hedge-fund

# Configuration
I decided to use Alpha Vantage as the financial data provider. I have no affiliation or sponsorship with them—it just happened that I created my free API key there and decided to stick with them. Once I get through the initial phase of porting and development, I will consider implementing the IDataReader interface to integrate with other providers.

To get your free API key, go here: https://www.alphavantage.co/support/#api-key. Once you have it, make sure to add the key to appsettings.json.

For LLM reasoning, OpenAI is used, so make sure to get your API key from: https://platform.openai.com/account/api-keys and add it to appsettings.json as well.
```
{
  "OpenAI": { "ApiKey": "your-api-key" },  
  "AlphaVantage": { "ApiKey": "your-api-key" }  
}
```
# Download and Run the program
Go on Releases and download the latest release. 
Run the program and pass --help or -h to print instructions
```
> AiHedgeFund.Console --help
```
You can run the program specifying one or more agent and one or more stock.
```
> AiHedgeFund.Console --agent cathie_wood ben_graham --tickers MSFT AAPL
```
# Cache
When you run the program, please remember that all the financial data are only retrieved from the remote API once. After that all the data are serialized and saved on file and cached in memory. Next time you re-run the program for the same tickers the data will be read from disk. To force a refresh, you must manually delete the data folder that is automatically created in the same folder of the program. I will add soon a parameter to force a refresh
