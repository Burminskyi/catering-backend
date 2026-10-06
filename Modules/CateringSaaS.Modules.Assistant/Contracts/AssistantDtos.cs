using System.Text.Json.Serialization;

namespace CateringSaaS.Modules.Assistant.Contracts;

public sealed record ToolResult(
    object Data,
    IReadOnlyList<AssistantArtifact>? Artifacts = null);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(MetricArtifact), "metric")]
[JsonDerivedType(typeof(TableArtifact), "table")]
public abstract record AssistantArtifact;

public sealed record MetricArtifact(
    string Label,
    string Value,
    string? Title = null) : AssistantArtifact;

public sealed record TableArtifact(
    string Title,
    IReadOnlyList<ArtifactColumn> Columns,
    IReadOnlyList<Dictionary<string, object?>> Rows) : AssistantArtifact;

public sealed record ArtifactColumn(string Key, string Label);

public sealed record AssistantChatRequest(
    string Message,
    Guid? ConversationId = null);

public sealed record AssistantChatResponse(
    Guid ConversationId,
    string Text,
    IReadOnlyList<AssistantArtifact> Artifacts);
