using System.Collections.Generic;

namespace Document.Analyzer.Services.Models
{
    public class AnalyzerResponse
    {
        public int NumberOfPagesAnalyzed { get; set; }
        public List<AnalyzedPageDetials>? Pages { get; set; }
        public Dictionary<string, double>? ColumnValuePair { get; set; }
        public double RefundPercentage { get; set; }
        public string? RecommandationOnRefund { get; set; }
        public double ChargebackPercentage { get; set; }
        public string? RecommandationOnChargeback { get; set; }
        public string? AnalzyerResultBuildFileId { get; set; }
    }
}
