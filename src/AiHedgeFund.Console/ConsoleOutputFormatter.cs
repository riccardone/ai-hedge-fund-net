using System.Text;
using AiHedgeFund.Contracts;
using AiHedgeFund.Contracts.Model;

namespace AiHedgeFund.Console;

public static class ConsoleOutputFormatter
{
    public static string FormatAgentReport(string agentKey, string modelName, string riskLevel, DateTime start, DateTime end, List<AgentReport> reports)
    {
        var displayName = agentKey.ToDisplayName();

        var sb = new StringBuilder();
        sb.AppendLine("==================================================");
        sb.AppendLine($"Analyst: {displayName}");
        sb.AppendLine($"Date Range: {start:dd/MM/yyyy} - {end:dd/MM/yyyy}");
        sb.AppendLine($"Model: {modelName} | Risk Level: {riskLevel}");
        sb.AppendLine("==================================================");

        foreach (var report in reports)
        {
            sb.AppendLine();
            sb.AppendLine($"[Analysis for {report.TradeSignal.Ticker}]");
            sb.AppendLine("--------------------------------------------------");
            sb.AppendLine($"Signal      : {report.TradeSignal.Signal} (Confidence: {report.TradeSignal.Confidence})");
            sb.AppendLine($"Reasoning   : {report.TradeSignal.Reasoning}");
            sb.AppendLine();
        }

        sb.AppendLine("==================================================");
        return sb.ToString();
    }


    public static void ExportReportsToCsv(string filePath, List<AgentReport> reports)
    {
        using var writer = new StreamWriter(filePath);
        writer.WriteLine("AgentName,Title,Value,Signal,Confidence,Reasoning");
        foreach (var report in reports)
        {
            foreach (var section in report.FinancialAnalysisResults)
            {
                writer.WriteLine(
                    $"{Escape(report.AgentName)},{Escape(section.Title)},{Escape($"{section.Score}/{section.MaxScore}")},{Escape(report.TradeSignal.Signal)},{report.Confidence},{Escape(report.Reasoning)}");
            }
        }
    }

    private static string Escape(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        return $"\"{input.Replace("\"", "\"\"")}\"";
    }
}