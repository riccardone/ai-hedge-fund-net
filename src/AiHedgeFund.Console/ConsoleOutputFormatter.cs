using AiHedgeFund.Contracts;
using AiHedgeFund.Contracts.Model;
using Spectre.Console;

namespace AiHedgeFund.Console;

public static class ConsoleOutputFormatter
{
    public static void PrintAgentReport(string agentKey, string modelName, string riskLevel, DateTime start, DateTime end, List<AgentReport> reports)
    {
        string Colorize(string text, string color)
        {
            return $"[{color}]{EscapeMarkup(text)}[/]";
        }

        string EscapeMarkup(string text)
        {
            return text.Replace("[", "[[").Replace("]", "]]");
        }

        var displayName = agentKey.ToDisplayName();

        AnsiConsole.MarkupLine($"{Colorize("==== ANALYSIS REPORT", "bold gray")} =================================================");
        AnsiConsole.MarkupLine($"Analyst: {Colorize(displayName, "cyan")}");
        AnsiConsole.MarkupLine($"Date Range: {Colorize(start.ToString("dd/MM/yyyy"), "cyan")} - {Colorize(end.ToString("dd/MM/yyyy"), "cyan")}");
        AnsiConsole.MarkupLine($"Model: {Colorize(modelName, "cyan")} | Risk Level: {Colorize(riskLevel, "cyan")}");
        AnsiConsole.MarkupLine($"{Colorize("======================================================================", "gray")}");

        foreach (var report in reports)
        {
            AnsiConsole.WriteLine(); // blank line
            AnsiConsole.MarkupLine($"[[Ticker: {Colorize(report.TradeSignal.Ticker, "green")}]]");
            AnsiConsole.MarkupLine($"{Colorize("------------------------------------------------------------------", "gray")}");
            AnsiConsole.MarkupLine($"Signal    : {Colorize(report.TradeSignal.Signal, "white")} (Confidence: {report.TradeSignal.Confidence})");
            AnsiConsole.MarkupLine($"Reasoning : {Colorize(report.TradeSignal.Reasoning, "white")}");
            AnsiConsole.WriteLine();

            if (report.FinancialAnalysisResults != null && report.FinancialAnalysisResults.Any())
            {
                AnsiConsole.MarkupLine($">> {Colorize("Financial Analysis Results:", "yellow")}");
                foreach (var analysis in report.FinancialAnalysisResults)
                {
                    AnsiConsole.MarkupLine($"- {Colorize($"{analysis.Title} [{analysis.Score}/{analysis.MaxScore}]", "magenta")}");
                    foreach (var detail in analysis.Details)
                    {
                        AnsiConsole.MarkupLine($"    • {Colorize(detail, "gray")}");
                    }
                    AnsiConsole.WriteLine();
                }
            }
        }
    }
}