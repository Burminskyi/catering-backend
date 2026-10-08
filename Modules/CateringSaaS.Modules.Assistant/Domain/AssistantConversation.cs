namespace CateringSaaS.Modules.Assistant.Domain;

public sealed class AssistantConversation
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>JSON array of persisted chat turns (user/assistant/tool).</summary>
    public string MessagesJson { get; set; } = "[]";

    /// <summary>JSON of the latest artifacts canvas payload, if any.</summary>
    public string? LastArtifactsJson { get; set; }
}
