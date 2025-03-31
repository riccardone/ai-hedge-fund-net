namespace AiHedgeFund.Contracts
{
    public class TradeDecision
    {
        public string Action { get; set; } = "hold"; // Default to "hold"
        public int Quantity { get; set; } = 0; // Number of shares to trade
        public double Confidence { get; set; } = 0.0; // Confidence in the decision (0-100%)
        public string Reasoning { get; set; } = "No decision made."; // Explanation

        public override string ToString()
        {
            return $"Action: {Action}, Quantity: {Quantity}, Confidence: {Confidence}%, Reasoning: {Reasoning}";
        }
    }
}