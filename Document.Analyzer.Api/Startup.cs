using System;
using Document.Analyzer.Services.Infrastructure.Configuration;
using Document.Analyzer.Services.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.CognitiveServices.FormRecognizer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Serilog;

namespace Document.Analyzer.Azure
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSingleton(Log.Logger);


            var subscriptionKey = Environment.GetEnvironmentVariable("FORM_RECOGNIZER_SUBSCRIPTION_KEY");
            var formRecognizerEndpoint = Environment.GetEnvironmentVariable("FORM_RECOGNIZER_ENDPOINT");

            services.AddSingleton<IFormRecognizerClient>(
                    new FormRecognizerClient(new ApiKeyServiceClientCredentials(subscriptionKey))
                    {
                        Endpoint = formRecognizerEndpoint
                    });
            services.AddSingleton<IDocumentAnalyzerService, DocumentAnalyzerService>();

            var azureStorageSettings = Configuration.GetSection("AzureStorageSettings").Get<AzureStorageSettings>();

            var storageCredentialKeyValue = Environment.GetEnvironmentVariable("STORAGE_CREDENTIAL_KEYVALUE");
            StorageCredentials storageCredentials = new StorageCredentials(azureStorageSettings.StorageName, storageCredentialKeyValue);
            CloudStorageAccount storageAccount = new CloudStorageAccount(storageCredentials, true);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            services.AddSingleton(blobClient);
            services.AddSingleton(azureStorageSettings);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
