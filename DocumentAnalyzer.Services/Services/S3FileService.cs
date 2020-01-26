using Amazon.S3.Transfer;
using Document.Analyzer.Services.Infrastructure.Configuration;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Document.Analyzer.Services.Services
{
    public class S3FileService : IS3FileService
    {
        private readonly ITransferUtility _transferUtility;
        private readonly S3Settings _s3Settings;
        private readonly ILogger _logger;

        public S3FileService(ITransferUtility transferUtility, S3Settings s3Settings, ILogger logger)
        {
            _transferUtility = transferUtility;
            _s3Settings = s3Settings;
            _logger = logger;
        }

        public async Task<string> UploadFileAsync(Stream fileStream)
        {
            try
            {
                _logger.Information("Started uploading analzye result file");

                var fileKey = Guid.NewGuid().ToString();

                var request = new TransferUtilityUploadRequest
                {
                    BucketName = _s3Settings.S3BucketName,
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    Key = fileKey,
                    InputStream = fileStream
                };

                await _transferUtility.UploadAsync(request);

                _logger.Information("Finished uploading analzye result file");

                return fileKey;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to upload file to S3");

                return string.Empty;
            }
        }
    }
}
