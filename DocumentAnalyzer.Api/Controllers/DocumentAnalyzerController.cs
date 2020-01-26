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
        private readonly IMlModelService _mlModelService;
        private readonly IResultAnalyzer _resultAnalyzer;
        private readonly ILogger<DocumentAnalyzerController> _logger;

        public DocumentAnalyzerController(IDocumentAnalyzerService documentAnalyzerService, IMlModelService mlModelService, IResultAnalyzer resultAnalyzer, ILogger<DocumentAnalyzerController> logger)
        {
            _documentAnalyzerService = documentAnalyzerService;
            _mlModelService = mlModelService;
            _resultAnalyzer = resultAnalyzer;
            _logger = logger;
        }

        [HttpPost]
        [Route("analyze")]
        public async Task<IActionResult> AnalyzeDocument(IFormFile file, [FromForm] string modelId)
        {
            try
            {
                //var fileKey = await _fileService.UploadFileAsync(file);
                var docAnalyzerResponse = await _documentAnalyzerService.RunFormRecognizerClient(file, modelId);
                _resultAnalyzer.AnalyzerResult(docAnalyzerResponse);
                return Ok(docAnalyzerResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing document");
            }

            return NoContent();
        }

        [HttpPost]
        [Route("train")]
        public async Task<IActionResult> TrainModel(IFormFile[] files)
        {
            try
            {
                var response = await _mlModelService.TrainFormRecognizerModel(files);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training model");
            }

            return NoContent();
        }

        [HttpGet]
        [Route("models")]
        public async Task<IActionResult> GetAllModels(IFormFile[] files)
        {
            try
            {
                var response = await _mlModelService.GetAllTrainedModelIds();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trained model");
            }

            return NoContent();
        }

        [HttpPost]
        [Route("delete")]
        public async Task<IActionResult> DeleteModel([FromForm] string modelId)
        {
            try
            {
                await _mlModelService.DeleteTrainedModel(modelId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting model ");
            }

            return NoContent();
        }
    }
}