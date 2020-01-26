using System.IO;
using System.Threading.Tasks;

namespace Document.Analyzer.Services.Services
{
    public interface IS3FileService
    {
        public Task<string> UploadFileAsync(Stream fileStream);
    }
}
