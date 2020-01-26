using Amazon.S3;
using Amazon.S3.Transfer;
using Document.Analyzer.Services.Infrastructure.Configuration;
using Document.Analyzer.Services.Services;
using DocumentAnalyzer.Api.Common.Validation;
using FluentValidation.AspNetCore;
using FluentValidation.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.CognitiveServices.FormRecognizer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Serialization;
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

            services
               .AddMvcCore(options =>
               {
                   options.Filters.Add(new ValidateRequestFilter(new AttributedValidatorFactory()));
                   options.Filters.Add(new ValidateModelStateFilter());
                   options.AllowEmptyInputInBodyModelBinding = true;
               })
               .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Startup>())
               .AddNewtonsoftJson(options =>
               {
                   options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                   options.SerializerSettings.ContractResolver = new DefaultContractResolver
                   {
                       NamingStrategy = new SnakeCaseNamingStrategy()
                   };
               });

            var azureSettings = Configuration.GetSection("AzureSettings").Get<AzureSettings>();

            services.AddSingleton<IFormRecognizerClient>(
                    new FormRecognizerClient(new ApiKeyServiceClientCredentials(azureSettings.FormRecognizerSubscriptionKey))
                    {
                        Endpoint = azureSettings.FormRecognizerEndpoint
                    });
            services.AddSingleton<IDocumentAnalyzerService, DocumentAnalyzerService>();
            services.AddSingleton<IResultAnalyzer, ResultAnalyzer>();
            services.AddSingleton<IMlModelService, MlModelService>();

            var s3Settings = Configuration.GetSection("S3Settings").Get<S3Settings>();

            StorageCredentials storageCredentials = new StorageCredentials(azureSettings.StorageName, azureSettings.StorageCredentialKeyvalue);
            CloudStorageAccount storageAccount = new CloudStorageAccount(storageCredentials, true);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            services.AddSingleton(blobClient);
            services.AddSingleton(azureSettings);
            services.AddSingleton(s3Settings);

            services.AddDefaultAWSOptions(Configuration.GetAWSOptions());
            services.AddAWSService<IAmazonS3>();
            services.AddSingleton<ITransferUtility, TransferUtility>();
            services.AddSingleton<IS3FileService, S3FileService>();
            services.AddSingleton<IFileBuilder, FileBuilder>();

#if DEBUG
            //new Container(services).AssertConfigurationIsValid();
#endif
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Map("/_system/ping", configuration =>
            {
                configuration.Run(async context =>
                {
                    await context.Response.WriteAsync("IPMONITOROK");
                });
            });

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
