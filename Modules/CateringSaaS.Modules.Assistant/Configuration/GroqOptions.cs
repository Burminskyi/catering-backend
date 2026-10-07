namespace CateringSaaS.Modules.Assistant.Configuration;

public sealed class GroqOptions
{
    public const string SectionName = "Groq";

    public const string HttpClientName = "Groq";

    public string ApiKey { get; set; } = string.Empty;

    // Use an id from GET /openai/v1/models for this key — Llama ids are often unavailable.
    public string Model { get; set; } = "openai/gpt-oss-120b";

    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
}
