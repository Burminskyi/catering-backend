namespace CateringSaaS.Modules.Knowledge.Services;

public interface IDocumentParser
{
    bool CanParse(string fileName, string? contentType);

    Task<string> ExtractTextAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
}
