using Microsoft.AspNetCore.Hosting;
using SyncSyntax.Models.IServices;

namespace SyncSyntax.Models
{
    public class UploadFileService : IUploadFileService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _config;
        private readonly ILogger<UploadFileService> _logger;

        public UploadFileService(IWebHostEnvironment webHostEnvironment, IConfiguration config, ILogger<UploadFileService> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _config = config;
            _logger = logger;
        }


        public async Task<string> UploadFileToFolderAsync(IFormFile file)
        {
            var folderName = _config.GetValue<string>("uploading:FolderPath") ?? "images/uploadImgs";
            var wwwRootPath = _webHostEnvironment.WebRootPath;
            var savePath = Path.Combine(wwwRootPath, folderName);

            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            var extension = Path.GetExtension(file.FileName).ToLower();
            var fileName = Guid.NewGuid().ToString() + extension;
            var fullPath = Path.Combine(savePath, fileName);

            try
            {
                await using var stream = new FileStream(fullPath, FileMode.Create);
                await file.CopyToAsync(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error uploading file: {Message}", ex.Message);
                throw;
            }

            return $"/{folderName}/{fileName}";
        }
    }
}
