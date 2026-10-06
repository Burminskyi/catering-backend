namespace CateringSaaS.Modules.Assistant.Configuration;

public sealed class GroqOptions
{
    public const string SectionName = "Groq";

    public const string HttpClientName = "Groq";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "llama-3.3-70b-versatile";

    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
}
