# ai-hedge-fund-net
This is a program that provide trading signals analysing stocks. There are trading agents applying different principles to decide if a stock is a buy, hold or sell. The agents will also provide the reasoning behind, the confidence, the metrics and the specific rules.  
Currently these are the configured agents: "charlie_munger", "stanley_druckenmiller", "ben_graham", "cathie_wood", "bill_ackman", "warren_buffett". As part of the analysis, each agent will integrate with an LLM trained for trading providing the reasoning behind signals.
This .Net project is inspired by the ai-hedge-fund project written in pyton https://github.com/virattt/ai-hedge-fund

# Configuration
I decided to use Alpha Vantage as financial data provider. I don't have any connection with them or sponsorship. It's just happened that I created there my free API key and stick with them. Once I get through the initial phase of porting/development I will consider implement the IDataReader interface integrating with other providers. To get you free API key go here https://www.alphavantage.co/support/#api-key Once you have it, make sure to add the key in the appsettings.json. For the LLM reasoning OpenApi is the one used so make sure to get your api key from here https://platform.openai.com/account/api-keys and write it in the appsettings.json.
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
