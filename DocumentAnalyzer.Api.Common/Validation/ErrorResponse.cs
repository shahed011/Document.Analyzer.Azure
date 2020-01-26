using Newtonsoft.Json;
using System.Collections.Generic;

namespace DocumentAnalyzer.Api.Common.Validation
{
    public class ErrorResponse
    {
        public ErrorResponse(string requestId, string errorType, IEnumerable<string> errorCodes = null)
        {
            RequestId = requestId;
            ErrorType = errorType;
            ErrorCodes = errorCodes;
        }

        [JsonProperty("request_id")]
        public string RequestId { get; }
        [JsonProperty("error_type")]
        public string ErrorType { get; }
        [JsonProperty("error_codes")]
        public IEnumerable<string> ErrorCodes { get; }
    }
}
