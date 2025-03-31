using ai_hedge_fund_net.Contracts;
using ai_hedge_fund_net.Contracts.Model;
using NLog;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace ai_hedge_fund_net.ConsoleApp.WorkflowSteps;

public class CathieWoodStep : StepBody
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public TradingWorkflowState State { get; set; }
    public IChatter Chatter { get; set; }
    public string Ticker { get; set; }

    public TradeSignal Output { get; set; }

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        Logger.Info($"[CathieWoodStep] Running agent for ticker: {Ticker}");

        var agent = new CathieWood(State, Chatter);
        Output = agent.GenerateOutput(Ticker);

        if (State.AnalystSignals == null)
            State.AnalystSignals = new();

        if (!State.AnalystSignals.ContainsKey("cathie_wood_agent"))
            State.AnalystSignals["cathie_wood_agent"] = new Dictionary<string, object>();

        State.AnalystSignals["cathie_wood_agent"][Ticker] = Output;

        Logger.Info($"[CathieWoodStep] Signal for {Ticker}: {Output.Signal}, Confidence: {Output.Confidence}, Reasoning: {Output.Reasoning}");

        return ExecutionResult.Next();
    }
}