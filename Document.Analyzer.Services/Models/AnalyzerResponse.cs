using System.Collections.Generic;

namespace Document.Analyzer.Services.Models
{
    public class AnalyzerResponse
    {
        public Dictionary<string, double>? AnalyzerResult { get; set; }
        public double RefundPercentage { get; set; }
        public string? RecommandationOnRefund { get; set; }
        public double ChargebackPercentage { get; set; }
        public string? RecommandationOnChargeback { get; set; }
    }
}
