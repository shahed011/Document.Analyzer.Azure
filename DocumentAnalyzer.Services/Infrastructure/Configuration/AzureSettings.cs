namespace Document.Analyzer.Services.Infrastructure.Configuration
{
    public class AzureSettings
    {
        public string? FormRecognizerSubscriptionKey { get; set; }
        public string? FormRecognizerEndpoint { get; set; }
        public string? StorageName { get; set; }
        public string? TrainingContainerName { get; set; }
        public string? StorageCredentialKeyvalue { get; set; }
    }
}
