# ai-hedge-fund-net
.Net version of the ai-hedge-fund pyton project https://github.com/virattt/ai-hedge-fund

# Configuration
I decided to use Alpha Vantage as financial data provider. I don't have any connection with them or sponsorship. It's just happen that I created there my free api key and I stick with them. Once I get through the initial phase of porting/development I will consider implement the IDataReader interface integrating with other providers. To get you free api key go here https://www.alphavantage.co/support/#api-key Once you have it, make sure to add the key in the appsettings.json. For the LLM reaoning OpenApi is the one used so make sure to get your api key from here https://platform.openai.com/account/api-keys and write it in the appsettings.json.
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
