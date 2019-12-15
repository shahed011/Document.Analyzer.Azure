using System;
using System.Threading.Tasks;
using Document.Analyzer.Services.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Document.Analyzer.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DocumentAnalyzerController : ControllerBase
    {
        private readonly IDocumentAnalyzerService _documentAnalyzerService;
        private readonly ILogger<DocumentAnalyzerController> _logger;

        public DocumentAnalyzerController(IDocumentAnalyzerService documentAnalyzerService, ILogger<DocumentAnalyzerController> logger)
        {
            _documentAnalyzerService = documentAnalyzerService;
            _logger = logger;
        }

        [HttpPost]
        [Route("analyze")]
        public async Task<IActionResult> AnalyzeDocument(IFormFile file, [FromForm] string modelId)
        {
            try
            {
                //var fileKey = await _fileService.UploadFileAsync(file);
                var response = await _documentAnalyzerService.RunFormRecognizerClient(file, modelId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Error analyzing document");
            }

            return NoContent();
        }

        [HttpPost]
        [Route("Train")]
        public async Task<IActionResult> TrainModel()
        {
            try
            {
                //var fileKey = await _fileService.UploadFileAsync(file);
                var response = await _documentAnalyzerService.TrainFormRecognizerModel();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Error training model");
            }

            return NoContent();
        }
    }
}