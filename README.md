# ai-hedge-fund-net
.Net version of the ai-hedge-fund pyton project https://github.com/virattt/ai-hedge-fund
This program can be use to analyze tickers for shares and provide trading signals. There are trading agents applying different priciples to decide if a stock is a buy, an hold or a sell. The agents will also provide the reasoning behind and the metrics and rules that follow. Currently these are the configured agents: "charlie_munger", "stanley_druckenmiller", "ben_graham", "cathie_wood", "bill_ackman", "warren_buffett". 

# Configuration
I decided to use Alpha Vantage as financial data provider. I don't have any connection with them or sponsorship. It's just happen that I created there my free api key and I stick with them. Once I get through the initial phase of porting/development I will consider implement the IDataReader interface integrating with other providers. To get you free api key go here https://www.alphavantage.co/support/#api-key Once you have it, make sure to add the key in the appsettings.json. For the LLM reasoning OpenApi is the one used so make sure to get your api key from here https://platform.openai.com/account/api-keys and write it in the appsettings.json.
```
{
  "OpenAI": { "ApiKey": "your-api-key" },  
  "AlphaVantage": { "ApiKey": "your-api-key" }  
}
```
# Run the program
Go on Releases and download the latest binary. 
Run the program and pass --help or -h to print instructions
```
> AiHedgeFund.Console --help
```
You can run the program specifying one or more agent and one or more stock.
```
> AiHedgeFund.Console --agent cathie_wood ben_graham --tickers MSFT AAPL
```
# Cache
When you run the program please remember that all the financial data are only retrieved from the remote api once. After that all the data are serialized and saved on file and cached in memory. Next time you re-run the program for the same tickers the data will be read from disk. To force a refresh you have to manually delete the data folder that is automatically created in the same folder of the program. I will add soon a parameter to force a refresh
