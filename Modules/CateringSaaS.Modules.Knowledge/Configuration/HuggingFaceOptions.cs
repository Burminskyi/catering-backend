namespace CateringSaaS.Modules.Knowledge.Configuration;

public sealed class HuggingFaceOptions
{
    public const string SectionName = "HuggingFace";

    /// <summary>HF access token (Settings → Access Tokens). Required for Inference API.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Embedding model id. Default: BAAI/bge-m3 (1024 dims).</summary>
    public string Model { get; set; } = "BAAI/bge-m3";

    /// <summary>HF Router base URL for serverless inference.</summary>
    public string BaseUrl { get; set; } = "https://router.huggingface.co";

    public int Dimensions { get; set; } = 1024;
}
