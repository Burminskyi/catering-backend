namespace CateringSaaS.Modules.Assistant.Configuration;

public sealed class GroqOptions
{
    public const string SectionName = "Groq";

    public const string HttpClientName = "Groq";

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>OpenAI-compatible chat model id (e.g. gpt-4o-mini).</summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>OpenAI-compatible Chat Completions base URL (…/v1).</summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
}
