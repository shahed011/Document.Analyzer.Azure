using Document.Analyzer.Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Document.Analyzer.Services.Services
{
    public class ResultAnalyzer : IResultAnalyzer
    {
        private const string Transaction = "Transaction";
        private const string Refund = "Refund";
        private const string Chargeback = "Chargeback";

        public void AnalyzerResult(AnalyzerResponse analyzerResponse)
        {
            var extractedColumnValuePair = analyzerResponse.ColumnValuePair ?? new Dictionary<string, double>();

            if (!extractedColumnValuePair.Keys.Any(x => x.Contains(Transaction, StringComparison.OrdinalIgnoreCase)))
                return;

            var transactoinKey = extractedColumnValuePair.Keys.Single(x => x.Contains(Transaction, StringComparison.OrdinalIgnoreCase));
            var transactionTotal = extractedColumnValuePair[transactoinKey];

            if (extractedColumnValuePair.Keys.Any(x => x.Contains(Refund, StringComparison.OrdinalIgnoreCase)))
            {
                var key = extractedColumnValuePair.Keys.Single(x => x.Contains(Refund, StringComparison.OrdinalIgnoreCase));
                var totalRefund = extractedColumnValuePair[key];
                analyzerResponse.RefundPercentage = (totalRefund / transactionTotal) * 100;

                analyzerResponse.RecommandationOnRefund = analyzerResponse.RefundPercentage > 20 ? "Consider reject" : "Consider accept";
            }

            if (extractedColumnValuePair.Keys.Any(x => x.Contains(Chargeback, StringComparison.OrdinalIgnoreCase)))
            {
                var key = extractedColumnValuePair.Keys.Single(x => x.Contains(Chargeback, StringComparison.OrdinalIgnoreCase));
                var totalChargeback = extractedColumnValuePair[key];
                analyzerResponse.ChargebackPercentage = (totalChargeback / transactionTotal) * 100;

                analyzerResponse.RecommandationOnChargeback = analyzerResponse.ChargebackPercentage > 20 ? "Consider reject" : "Consider accept";
            }
        }
    }
}
