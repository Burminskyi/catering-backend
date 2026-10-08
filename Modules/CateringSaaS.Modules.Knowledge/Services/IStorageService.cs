namespace CateringSaaS.Modules.Knowledge.Services;

public interface IStorageService
{
    /// <summary>
    /// Uploads a file and returns a stable storage key/URL for later download or delete.
    /// </summary>
    Task<string> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task DeleteFileAsync(string fileUrlOrKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a browser-usable download URL (presigned GET) for a stored object.
    /// </summary>
    string GetDownloadUrl(string fileUrlOrKey, TimeSpan? lifetime = null);
}
