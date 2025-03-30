﻿namespace ai_hedge_fund_net.Contracts.Model;

public class TradeSignal
{
    public TradeSignal(string ticker, string signal, decimal confidence, string reasoning)
    {
        Ticker = ticker;
        Signal = signal;
        Confidence = confidence;
        Reasoning = reasoning;
    }

    public TradeSignal() { }

    public void SetTicker(string ticker)
    {
        Ticker = ticker;
    }

    public string Ticker { get; set; }
    public string Signal { get; set; }
    public decimal Confidence { get; set; }
    public string Reasoning { get; set; }
}