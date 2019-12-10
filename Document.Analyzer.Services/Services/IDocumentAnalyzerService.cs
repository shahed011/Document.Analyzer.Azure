using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Document.Analyzer.Services.Services
{
    public interface IDocumentAnalyzerService
    {
        Task<Dictionary<string, double>> RunFormRecognizerClient(IFormFile file);
    }
}
