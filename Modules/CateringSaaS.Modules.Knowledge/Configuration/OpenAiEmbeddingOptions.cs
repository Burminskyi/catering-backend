namespace CateringSaaS.Modules.Knowledge.Configuration;

public sealed class OpenAiEmbeddingOptions
{
    public const string SectionName = "OpenAIEmbeddings";

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Default: text-embedding-3-small (supports custom dimensions).</summary>
    public string Model { get; set; } = "text-embedding-3-small";

    /// <summary>Must match pgvector column size (vector(1024)).</summary>
    public int Dimensions { get; set; } = 1024;

    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
}
