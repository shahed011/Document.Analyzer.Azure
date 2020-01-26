using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Document.Analyzer.Services.Services
{
    public interface IMlModelService
    {
        Task<Guid> TrainFormRecognizerModel(IFormFile[] files);
        Task<List<Guid>> GetAllTrainedModelIds();
        Task DeleteTrainedModel(string modelIdString);
    }
}
