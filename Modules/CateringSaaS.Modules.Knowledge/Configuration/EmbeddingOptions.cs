namespace CateringSaaS.Modules.Knowledge.Configuration;

public sealed class EmbeddingOptions
{
    public const string SectionName = "Embeddings";

    /// <summary>Local (free ONNX) | HuggingFace | OpenAI</summary>
    public string Provider { get; set; } = "Local";
}
