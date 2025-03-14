﻿using System.Text;

namespace ai_hedge_fund_net.ConsoleApp;

public static class ConsoleGraphRenderer
{
    public static void RenderGraph()
    {
        var edges = new List<(string, string)>
        {
            ("Start", "Ben Graham Analysis"),
            ("Ben Graham Analysis", "Risk Management"),
            ("Risk Management", "Portfolio Management"),
            ("Portfolio Management", "Trading Complete")
        };

        StringBuilder sb = new();
        sb.AppendLine("📊 Trading Workflow Visualization:");

        foreach (var (from, to) in edges)
        {
            sb.AppendLine($"  {from} --> {to}");
        }

        Console.WriteLine(sb.ToString());
    }
}