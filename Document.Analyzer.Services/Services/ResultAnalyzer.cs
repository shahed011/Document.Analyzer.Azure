using Document.Analyzer.Services.Models;
using System.Collections.Generic;
using System.Linq;

namespace Document.Analyzer.Services.Services
{
    public class ResultAnalyzer : IResultAnalyzer
    {
        private const string Transaction = "Transaction";
        private const string Refund = "Refund";
        private const string Chargeback = "Chargeback";

        public AnalyzerResponse AnalyzerResult(Dictionary<string, double> extractedValues)
        {
            var response = new AnalyzerResponse { AnalyzerResult = extractedValues, RecommandationOnRefund = "N/A", RecommandationOnChargeback = "N/A" };

            if (!extractedValues.Keys.Any(x => x.Contains(Transaction)))
                return response;

            var transactoinKey = extractedValues.Keys.Single(x => x.Contains(Transaction));
            var transactionTotal = extractedValues[transactoinKey];

            if (extractedValues.Keys.Any(x => x.Contains(Refund)))
            {
                var key = extractedValues.Keys.Single(x => x.Contains(Refund));
                var totalRefund = extractedValues[key];
                response.RefundPercentage = (totalRefund / transactionTotal) * 100;

                response.RecommandationOnRefund = response.RefundPercentage > 20 ? "Consider reject" : "Consider accept";
            }

            if (extractedValues.Keys.Any(x => x.Contains(Chargeback)))
            {
                var key = extractedValues.Keys.Single(x => x.Contains(Chargeback));
                var totalChargeback = extractedValues[key];
                response.ChargebackPercentage = (totalChargeback / transactionTotal) * 100;

                response.RecommandationOnChargeback = response.ChargebackPercentage > 20 ? "Consider reject" : "Consider accept";
            }

            return response;
        }
    }
}
