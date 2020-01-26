using Document.Analyzer.Services.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Document.Analyzer.Services.Services
{
    public interface IDocumentAnalyzerService
    {
        Task<AnalyzerResponse> RunFormRecognizerClient(IFormFile file, string modelId = "");
    }
}
