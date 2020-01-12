using Document.Analyzer.Services.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.CognitiveServices.FormRecognizer;
using Microsoft.Azure.CognitiveServices.FormRecognizer.Models;
using Microsoft.WindowsAzure.Storage.Blob;
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
        private readonly CloudBlobClient _cloudBlobClient;
        private readonly AzureStorageSettings _azureStorageSettings;
        private readonly ILogger _logger;

        private const string TempFileName = "file{0}";
        private readonly List<string> ColumnTextsToCheck = new List<string> {"amount", "transaction", "refund", "commission", "chargeback"};

        public DocumentAnalyzerService(IFormRecognizerClient formRecognizerClient, CloudBlobClient cloudBlobClient, AzureStorageSettings azureStorageSettings, ILogger logger)
        {
            _formRecognizerClient = formRecognizerClient;
            _cloudBlobClient = cloudBlobClient;
            _azureStorageSettings = azureStorageSettings;
            _logger = logger;
        }

        public async Task<Guid> TrainFormRecognizerModel()
        {
            _logger.Information("Train Model with training data...");

            var uri = GetTrainingContainerUri();
            return await TrainModelAsync(uri);
        }

        public async Task<Dictionary<string, double>> RunFormRecognizerClient(IFormFile file, string modelId = "")
        {
            _logger.Information("Get list of trained models ...");
            var modelIds = await GetListOfModels();

            //await _formRecognizerClient.DeleteCustomModelAsync(modelIds.First());

            if (!modelIds.Any())
            {
                var uri = GetTrainingContainerUri();

                _logger.Information("Train Model with training data...");
                modelIds.Add(await TrainModelAsync(uri));
            }

            _logger.Information("Analyze file...");

            var modelIdGuid = modelIds.First();

            if (!string.IsNullOrEmpty(modelId) && Guid.TryParse(modelId, out var guidResult) && modelIds.Contains(guidResult))
            {
                modelIdGuid = guidResult;
            }

            return await AnalyzePdfForm(modelIdGuid, file);

            //_logger.Information("Delete Model...");
            //await DeleteModel(modelId);
        }

        private async Task<Dictionary<string, double>> AnalyzePdfForm(Guid modelId, IFormFile  file)
        {
            if (file.Length <= 0)
            {
                _logger.Error("Invalid formFile.");
                //return string.Empty;
                return new Dictionary<string, double>();
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
                AnalyzeResult result = await _formRecognizerClient.AnalyzeWithCustomModelAsync(modelId, fileStream, file.ContentType);

                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                _logger.Information($"Analyzed file {file.FileName}:");
                //return DisplayAnalyzeResult(result);
                //var val = DisplayAnalyzeResult(result);
                return GetAllAmountsAndValues(result);
            }
            catch (ErrorResponseException responseEx)
            {
                _logger.Error(responseEx, "Error while analyzing file");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error while analyzing file");
            }

            //return string.Empty;
            return new Dictionary<string, double>();
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

        private Dictionary<string, double> GetAllAmountsAndValues(AnalyzeResult analysisResult)
        {
            var result = new Dictionary<string, double>();

            foreach (var page in analysisResult.Pages)
            {
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
                    foreach (var column in table.Columns)
                    {
                        if (column.Header.Any(x => ColumnTextsToCheck.Any(y => x.Text.Contains(y, StringComparison.OrdinalIgnoreCase))))
                        {
                            var sum = column.Entries.SelectMany(x => x.Where(y => double.TryParse(y.Text, out _))).Select(x => double.Parse(x.Text)).Sum();

                            var key = string.Join(" ", column.Header.Select(x => x.Text));
                            if (result.ContainsKey(key))
                            {
                                result[key] += sum;
                            }
                            else if (result.Keys.Any(x => x.Contains(key)))
                            {
                                key = result.Keys.SingleOrDefault(x => x.Contains(key));
                                if (key != null)
                                {
                                    result[key] += sum;
                                }
                            }
                            else
                            {
                                result.Add(key, sum);
                            }
                        }
                    }
                }
            }

            return result;
        }

        private string GetTrainingContainerUri()
        {
            //// Create storagecredentials object by reading the values from the configuration (appsettings.json)
            //StorageCredentials storageCredentials = new StorageCredentials("documentanalyzerstorage", "dDTZvB5bd6Ln2sReoxFabxtxdo5K4wN8II9nLSBnX2bfqnhMz40zMLSm8wNyCi6KRHvGcueQLg4O9fCPSAfuyw==");

            //// Create cloudstorage account by passing the storagecredentials
            //CloudStorageAccount storageAccount = new CloudStorageAccount(storageCredentials, true);

            //// Create the blob client.
            //CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Get reference to the blob container by passing the name by reading the value from the configuration (appsettings.json)
            var container = _cloudBlobClient.GetContainerReference(_azureStorageSettings.TrainingContainerName);

            string sasContainerToken;

            // If no stored policy is specified, create a new access policy and define its constraints.
            //if (storedPolicyName == null)
            //{
            // Note that the SharedAccessBlobPolicy class is used both to define the parameters of an ad hoc SAS, and
            // to construct a shared access policy that is saved to the container's shared access policies.
            var adHocPolicy = new SharedAccessBlobPolicy()
            {
                // When the start time for the SAS is omitted, the start time is assumed to be the time when the storage service receives the request.
                // Omitting the start time for a SAS that is effective immediately helps to avoid clock skew.
                SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(10),
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.List
            };

            // Generate the shared access signature on the container, setting the constraints directly on the signature.
            sasContainerToken = container.GetSharedAccessSignature(adHocPolicy, null);
            //}
            //else
            //{
            //    // Generate the shared access signature on the container. In this case, all of the constraints for the
            //    // shared access signature are specified on the stored access policy, which is provided by name.
            //    // It is also possible to specify some constraints on an ad hoc SAS and others on the stored access policy.
            //    sasContainerToken = container.GetSharedAccessSignature(null, storedPolicyName);

            //    Console.WriteLine("SAS for blob container (stored access policy): {0}", sasContainerToken);
            //    Console.WriteLine();
            //}

            // Return the URI string for the container, including the SAS token.
            return container.Uri + sasContainerToken;
        }

        private async Task<List<Guid>> GetListOfModels()
        {
            try
            {
                var models = await _formRecognizerClient.GetCustomModelsAsync();
                return models.ModelsProperty.Select(x => x.ModelId).ToList();
            }
            catch (ErrorResponseException ex)
            {
                _logger.Error(ex, "Error getting list of trained models ...");
                throw ex;
            }
        }

        private async Task<Guid> TrainModelAsync(string uri)
        {
            if (!Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            {
                _logger.Error("\nInvalid trainingDataUrl:\n{0} \n", uri);
                return Guid.Empty;
            }

            try
            {
                TrainResult result = await _formRecognizerClient.TrainCustomModelAsync(new TrainRequest(uri));
                ModelResult model = await _formRecognizerClient.GetCustomModelAsync(result.ModelId);

                _logger.Information($"Id: {model.ModelId}, Status: {model.Status}");
                return result.ModelId;
            }
            catch (ErrorResponseException ex)
            {
                _logger.Error(ex, "Error training Model");
                return Guid.Empty;
            }
        }
    }
}
