using Document.Analyzer.Services.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.CognitiveServices.FormRecognizer;
using Microsoft.Azure.CognitiveServices.FormRecognizer.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Document.Analyzer.Services.Services
{
    public class DocumentAnalyzerService : IDocumentAnalyzerService
    {
        private readonly IFormRecognizerClient _formRecognizerClient;
        private readonly IMlModelService _mlModelService;
        private readonly IFileBuilder _fileBuilder;
        private readonly IS3FileService _s3FileService;
        private readonly ILogger _logger;

        private const string TempFileName = "file{0}";
        private readonly List<string> ColumnTextsToCheck = new List<string> {"transaction", "refund", "commission", "chargeback"};

        public DocumentAnalyzerService(IFormRecognizerClient formRecognizerClient, IMlModelService mlModelService, IFileBuilder fileBuilder, IS3FileService s3FileService, ILogger logger)
        {
            _formRecognizerClient = formRecognizerClient;
            _mlModelService = mlModelService;
            _fileBuilder = fileBuilder;
            _s3FileService = s3FileService;
            _logger = logger;
        }


        public async Task<AnalyzerResponse> RunFormRecognizerClient(IFormFile file, string modelId = "")
        {
            _logger.Information("Get list of trained models ...");
            var modelIds = await _mlModelService.GetAllTrainedModelIds();

            if (!modelIds.Any())
            {
                throw new InvalidOperationException("No trained model found. Can not analyzer document. Please train a model first");
            }

            _logger.Information("Analyze file...");

            var modelIdGuid = modelIds.First();

            if (!string.IsNullOrEmpty(modelId) && Guid.TryParse(modelId, out var guidResult) && modelIds.Contains(guidResult))
            {
                modelIdGuid = guidResult;
            }

            return await AnalyzePdfForm(modelIdGuid, file);
        }

        private async Task<AnalyzerResponse> AnalyzePdfForm(Guid modelId, IFormFile  file)
        {
            if (file.Length <= 0)
            {
                throw new InvalidDataException("Invalid formFile");
            }

            try
            {
                var fileName = string.Format(TempFileName, Path.GetExtension(file.FileName));

                using var iFormFileStream = file.OpenReadStream();
                using var stream = File.Create(fileName);
                iFormFileStream.Seek(0, SeekOrigin.Begin);
                iFormFileStream.CopyTo(stream);
                stream.Close();

                using var fileStream = new FileStream(fileName, FileMode.Open);
                var result = await _formRecognizerClient.AnalyzeWithCustomModelAsync(modelId, fileStream, file.ContentType);

                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                _logger.Information($"Analyzed file {file.FileName}:");

                var analyzerResponse =  GetAllAmountsAndValues(result);
                var analzyerResultBuildFileStream = _fileBuilder.BuildFileFromAnalyzeResult(result);
                analyzerResponse.AnalzyerResultBuildFileId = await _s3FileService.UploadFileAsync(analzyerResultBuildFileStream);

                return analyzerResponse;
            }
            catch (ErrorResponseException responseEx)
            {
                _logger.Error(responseEx, "Error while analyzing file");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error while analyzing file");
            }

            return new AnalyzerResponse();
        }

        private string DisplayAnalyzeResult(AnalyzeResult result)
        {
            var output = new StringBuilder();

            foreach (var page in result.Pages)
            {
                output.AppendLine("\tPage#: " + page.Number);
                output.AppendLine("\tCluster Id: " + page.ClusterId);
                foreach (var kv in page.KeyValuePairs)
                {
                    if (kv.Key.Count > 0)
                        output.Append(kv.Key[0].Text);

                    if (kv.Value.Count > 0)
                        output.Append(" - " + kv.Value[0].Text);

                    output.AppendLine();
                }

                output.AppendLine();

                foreach (var t in page.Tables)
                {
                    output.AppendLine("Table id: " + t.Id);
                    foreach (var c in t.Columns)
                    {
                        foreach (var h in c.Header)
                            output.Append(h.Text + "\t");

                        foreach (var e in c.Entries)
                        {
                            foreach (var ee in e)
                                output.Append(ee.Text + "\t");
                        }

                        output.AppendLine();
                    }

                    output.AppendLine();
                }
            }

            return output.ToString();
        }

        private AnalyzerResponse GetAllAmountsAndValues(AnalyzeResult analyzeResult)
        {
            var respose = new AnalyzerResponse
            {
                NumberOfPagesAnalyzed = analyzeResult.Pages.Count,
                Pages = new List<AnalyzedPageDetials>()
            };

            var columnValuePair = new Dictionary<string, double>();

            foreach (var page in analyzeResult.Pages)
            {
                var analyzedPage = new AnalyzedPageDetials
                {
                    PageNumber = page.Number,
                    NumberOfTables = page.Tables.Count(),
                    Tables = new List<AnalyzedTableDetails>()
                };

                //foreach (var kv in page.KeyValuePairs)
                //{
                //    if (kv.Key.Any(x => ColumnTextsToCheck.Any(y => x.Text.Contains(y, StringComparison.OrdinalIgnoreCase))))
                //    {
                //        var key = string.Join(" ", kv.Key.Select(x => x.Text));
                //        var value = kv.Value.Where(x => double.TryParse(x.Text, out _)).Select(x => double.Parse(x.Text)).Sum();
                //        result.Add(key, value);
                //    }
                //}

                foreach (var table in page.Tables)
                {
                    var analyzedTable = new AnalyzedTableDetails
                    {
                        ColumnRowCountPair = new List<Tuple<string, int>>()
                    };

                    foreach (var column in table.Columns)
                    {
                        analyzedTable.ColumnRowCountPair.Add(Tuple.Create(string.Join(" ", column.Header.Select(x => x.Text)), column.Entries.Count));

                        if (column.Header.Any(x => ColumnTextsToCheck.Any(y => x.Text.Trim().Equals(y.Trim(), StringComparison.OrdinalIgnoreCase))))
                        {
                            var sum = column.Entries.SelectMany(x => x.Where(y => double.TryParse(y.Text, out _))).Select(x => double.Parse(x.Text)).Sum();

                            var key = string.Join(" ", column.Header.Select(x => x.Text));
                            if (columnValuePair.ContainsKey(key))
                            {
                                columnValuePair[key] += sum;
                            }
                            else if (columnValuePair.Keys.Any(x => x.Contains(key)))
                            {
                                key = columnValuePair.Keys.SingleOrDefault(x => x.Contains(key));
                                if (key != null)
                                {
                                    columnValuePair[key] += sum;
                                }
                            }
                            else
                            {
                                columnValuePair.Add(key, sum);
                            }
                        }
                    }

                    analyzedPage.Tables.Add(analyzedTable);
                }

                respose.Pages.Add(analyzedPage);
            }

            respose.ColumnValuePair = columnValuePair;

            return respose;
        }
    }
}
