namespace CateringSaaS.Modules.Assistant.Configuration;

public sealed class GroqOptions
{
    public const string SectionName = "Groq";

    public const string HttpClientName = "Groq";

    public string ApiKey { get; set; } = string.Empty;

    // 70B often returns model_not_found on free/dev keys; 8B is the reliable default.
    public string Model { get; set; } = "llama-3.1-8b-instant";

    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
}
