using System.Text.Json.Serialization;

namespace CateringSaaS.Modules.Assistant.Contracts;

public sealed record ToolResult(
    object Data,
    IReadOnlyList<AssistantArtifact>? Artifacts = null);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(MetricArtifact), "metric")]
[JsonDerivedType(typeof(TableArtifact), "table")]
[JsonDerivedType(typeof(OverviewArtifact), "overview")]
[JsonDerivedType(typeof(KnowledgeSourcesArtifact), "knowledgeSources")]
public abstract record AssistantArtifact;

public sealed record MetricArtifact(
    string Label,
    string Value,
    string? Title = null) : AssistantArtifact;

public sealed record TableArtifact(
    string Title,
    IReadOnlyList<ArtifactColumn> Columns,
    IReadOnlyList<Dictionary<string, object?>> Rows) : AssistantArtifact;

/// <summary>Detailed markdown for the right-hand Overview panel (not shown in chat).</summary>
public sealed record OverviewArtifact(string Markdown) : AssistantArtifact;

/// <summary>Document sources for RAG answers (right panel, no charts).</summary>
public sealed record KnowledgeSourcesArtifact(
    string Title,
    IReadOnlyList<KnowledgeSourceItem> Sources) : AssistantArtifact;

public sealed record KnowledgeSourceItem(
    string DocumentTitle,
    string FileName,
    string Excerpt,
    string? DownloadUrl = null);

public sealed record ArtifactColumn(string Key, string Label);

public sealed record AssistantChatRequest(
    string Message,
    Guid? ConversationId = null);

public sealed record AssistantChatResponse(
    Guid ConversationId,
    string Text,
    IReadOnlyList<AssistantArtifact> Artifacts,
    string? Title = null);

public abstract record AssistantStreamEvent;

/// <summary>Live pipeline stage for the chat UI (thinking, querying, analyzing, finalizing).</summary>
public sealed record AssistantStatusEvent(string Stage) : AssistantStreamEvent;

public sealed record AssistantTokenEvent(string Text) : AssistantStreamEvent;

public sealed record AssistantArtifactsEvent(IReadOnlyList<AssistantArtifact> Artifacts) : AssistantStreamEvent;

public sealed record AssistantDoneEvent(Guid ConversationId, string? Title = null) : AssistantStreamEvent;
