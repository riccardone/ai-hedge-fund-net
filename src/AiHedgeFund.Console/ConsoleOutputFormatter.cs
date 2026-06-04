using AiHedgeFund.Contracts;
using Spectre.Console;

namespace AiHedgeFund.Console;

public static class ConsoleOutputFormatter
{
    public static void PrintAgentReport(string agentKey, string agentDisplayName, string aiProvider, string modelName, string riskLevel, DateTime start, DateTime end, List<AgentResult> results)
    {
        string Colorize(string text, string color)
        {
            return $"[{color}]{EscapeMarkup(text)}[/]";
        }

        string EscapeMarkup(string text)
        {
            return text.Replace("[", "[[").Replace("]", "]]");
        }

        AnsiConsole.MarkupLine($"{Colorize("==== ANALYSIS REPORT", "bold gray")} =================================================");
        AnsiConsole.MarkupLine($"Analyst: {Colorize(agentDisplayName, "cyan")}");
        AnsiConsole.MarkupLine($"Date Range: {Colorize(start.ToString("dd/MM/yyyy"), "cyan")} - {Colorize(end.ToString("dd/MM/yyyy"), "cyan")}");
        AnsiConsole.MarkupLine($"Model: {Colorize(aiProvider, "cyan")} {Colorize(modelName, "cyan")} | Risk Level: {Colorize(riskLevel, "cyan")}");
        AnsiConsole.MarkupLine($"{Colorize("======================================================================", "gray")}");

        foreach (var result in results)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[[Ticker: {Colorize(result.Ticker, "green")}]]");
            AnsiConsole.MarkupLine($"{Colorize("------------------------------------------------------------------", "gray")}");
            AnsiConsole.MarkupLine($"Signal    : {Colorize(result.Signal.Signal, "white")} (Confidence: {result.Signal.Confidence})");
            AnsiConsole.MarkupLine($"Reasoning : {Colorize(result.Signal.Reasoning, "white")}");
            AnsiConsole.WriteLine();

            if (result.Scores != null && result.Scores.Any())
            {
                AnsiConsole.MarkupLine($">> {Colorize("Financial Analysis Results:", "yellow")}");
                foreach (var analysis in result.Scores)
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