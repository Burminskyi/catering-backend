namespace CateringSaaS.Modules.Knowledge.Services;

public interface IEmbeddingService
{
    int Dimensions { get; }

    string ModelName { get; }

    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default);
}
