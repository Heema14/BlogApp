namespace SyncSyntax.Models.IServices
{
    public interface IUploadFileService
    {
        Task<string> UploadFileToFolderAsync(IFormFile file);
    }
}
