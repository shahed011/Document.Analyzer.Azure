using Document.Analyzer.Services.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.CognitiveServices.FormRecognizer;
using Microsoft.Azure.CognitiveServices.FormRecognizer.Models;
using Microsoft.WindowsAzure.Storage.Blob;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Document.Analyzer.Services.Services
{
    public class MlModelService : IMlModelService
    {
        private readonly IFormRecognizerClient _formRecognizerClient;
        private readonly CloudBlobClient _cloudBlobClient;
        private readonly AzureStorageSettings _azureStorageSettings;
        private readonly ILogger _logger;

        public MlModelService(IFormRecognizerClient formRecognizerClient, CloudBlobClient cloudBlobClient, AzureStorageSettings azureStorageSettings, ILogger logger)
        {
            _formRecognizerClient = formRecognizerClient;
            _cloudBlobClient = cloudBlobClient;
            _azureStorageSettings = azureStorageSettings;
            _logger = logger;
        }

        public async Task<Guid> TrainFormRecognizerModel(IFormFile[] files)
        {
            _logger.Information("Train Model with training data...");

            await UploadTrainingFile(files);

            var uri = GetTrainingContainerUri();
            var modelId = await TrainModelAsync(uri);

            await DeleteTrainingData(files.Select(x => x.FileName).ToArray());

            return modelId;
        }

        public async Task<List<Guid>> GetAllTrainedModelIds()
        {
            _logger.Information("Getting all trained models...");

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

        public async Task DeleteTrainedModel(string modelIdString)
        {
            _logger.Information($"Deleting trained model : id {modelIdString}");

            if (Guid.TryParse(modelIdString, out var result))
            {
                await _formRecognizerClient.DeleteCustomModelAsync(result);
            }
        }

        private async Task UploadTrainingFile(IFormFile[] files)
        {
            _logger.Information("Starting to upload training files...");

            var container = _cloudBlobClient.GetContainerReference(_azureStorageSettings.TrainingContainerName);

            foreach (var file in files)
            {
                _logger.Information($"Uploading file {file.FileName}");

                var blob = container.GetBlockBlobReference(file.FileName);
                blob.Properties.ContentType = file.ContentType;

                using var iFormFileStream = file.OpenReadStream();
                try
                {
                    await blob.UploadFromStreamAsync(iFormFileStream);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Failed uploading file {file.FileName}");
                }
            }
        }

        private string GetTrainingContainerUri()
        {
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
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Write
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

        private async Task<Guid> TrainModelAsync(string uri)
        {
            if (!Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            {
                _logger.Error($"Invalid trainingDataUrl:{uri}");
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

        private async Task DeleteTrainingData(string[] fileNames)
        {
            var container = _cloudBlobClient.GetContainerReference(_azureStorageSettings.TrainingContainerName);

            foreach (var fileName in fileNames)
            {
                _logger.Information($"Deleting training file {fileName}");

                try
                {
                    var blob = container.GetBlockBlobReference(fileName);
                    await blob.DeleteIfExistsAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Failed to delete file {fileName}");
                }
            }
        }
    }
}
