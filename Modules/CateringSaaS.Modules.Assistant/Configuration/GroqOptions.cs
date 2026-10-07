namespace CateringSaaS.Modules.Assistant.Configuration;

public sealed class GroqOptions
{
    public const string SectionName = "Groq";

    public const string HttpClientName = "Groq";

    public string ApiKey { get; set; } = string.Empty;

    // Prefer 20b on free tier (higher TPM headroom than 120b). Must be in GET /openai/v1/models.
    public string Model { get; set; } = "openai/gpt-oss-20b";

    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
}
