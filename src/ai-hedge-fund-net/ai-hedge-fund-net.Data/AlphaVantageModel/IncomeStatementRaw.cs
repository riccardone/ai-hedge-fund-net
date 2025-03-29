namespace ai_hedge_fund_net.Data.AlphaVantageModel;

public class IncomeStatementRaw
{
    public string Symbol { get; set; }
    public List<IncomeStatementReportRaw> AnnualReports { get; set; }
    public List<IncomeStatementReportRaw> QuarterlyReports { get; set; }
}

public class IncomeStatementReportRaw
{
    public string FiscalDateEnding { get; set; }
    public string ReportedCurrency { get; set; }
    public string GrossProfit { get; set; }
    public string TotalRevenue { get; set; }
    public string CostOfRevenue { get; set; }
    public string CostOfGoodsAndServicesSold { get; set; }
    public string OperatingIncome { get; set; }
    public string SellingGeneralAndAdministrative { get; set; }
    public string ResearchAndDevelopment { get; set; }
    public string OperatingExpenses { get; set; }
    public string InvestmentIncomeNet { get; set; }
    public string NetInterestIncome { get; set; }
    public string InterestIncome { get; set; }
    public string InterestExpense { get; set; }
    public string NonInterestIncome { get; set; }
    public string OtherNonOperatingIncome { get; set; }
    public string Depreciation { get; set; }
    public string DepreciationAndAmortization { get; set; }
    public string IncomeBeforeTax { get; set; }
    public string IncomeTaxExpense { get; set; }
    public string InterestAndDebtExpense { get; set; }
    public string NetIncomeFromContinuingOperations { get; set; }
    public string ComprehensiveIncomeNetOfTax { get; set; }
    public string EBIT { get; set; }
    public string EBITDA { get; set; }
    public string NetIncome { get; set; }
}
