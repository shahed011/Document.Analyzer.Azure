using Document.Analyzer.Services.Models;
using System.Collections.Generic;

namespace Document.Analyzer.Services.Services
{
    public interface IResultAnalyzer
    {
        AnalyzerResponse AnalyzerResult(Dictionary<string, double> extractedValues);
    }
}
