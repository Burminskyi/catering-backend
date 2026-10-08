namespace CateringSaaS.Modules.Assistant.Configuration;

/// <summary>
/// RAG context budget. Tight defaults are for free-tier Groq TPM (~8k);
/// production should raise these (see appsettings) for answer quality.
/// </summary>
public sealed class AssistantRagOptions
{
    public const string SectionName = "Assistant:Rag";

    /// <summary>Max chunks returned to the LLM (1–10).</summary>
    public int MaxTopK { get; set; } = 5;

    public int DefaultTopK { get; set; } = 3;

    /// <summary>Max characters per chunk in the LLM payload. Use 8000+ in production for full chunks.</summary>
    public int MaxCharsPerChunk { get; set; } = 8000;

    /// <summary>Hard cap on total documentChunks body size.</summary>
    public int MaxTotalContextChars { get; set; } = 12_000;

    /// <summary>
    /// When true, the post-RAG answer turn uses a short system prompt and only user+chunks
    /// (needed for free Groq TPM). Production should set false.
    /// </summary>
    public bool UseLeanKnowledgeWindow { get; set; } = false;
}
