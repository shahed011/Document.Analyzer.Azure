using Microsoft.Azure.CognitiveServices.FormRecognizer.Models;
using System.IO;

namespace Document.Analyzer.Services.Services
{
    public interface IFileBuilder
    {
        public Stream BuildFileFromAnalyzeResult(AnalyzeResult analyzeResult);
    }
}
