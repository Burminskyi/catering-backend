using System.Text.Json;
using System.Text.Json.Serialization;
using CateringSaaS.Modules.Assistant.Contracts;
using CateringSaaS.Modules.Assistant.Domain;
using CateringSaaS.Shared.Data;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;

namespace CateringSaaS.Modules.Assistant.Services;

public readonly record struct ConversationKey(Guid WorkspaceId, Guid UserId, Guid ConversationId);

public interface IAssistantConversationStore
{
    Task<IReadOnlyList<ChatMessage>> GetOrCreateAsync(ConversationKey key, CancellationToken cancellationToken = default);

    Task SaveAsync(
        ConversationKey key,
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<AssistantArtifact>? artifacts = null,
        string? firstUserMessageForTitle = null,
        string? generatedTitle = null,
        CancellationToken cancellationToken = default);
}

public interface IAssistantConversationHistory
{
    Task<IReadOnlyList<AssistantConversationSummary>> ListAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<AssistantConversationDetail?> GetAsync(
        Guid workspaceId,
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid workspaceId,
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken = default);
}

public sealed record AssistantConversationSummary(
    Guid Id,
    string Title,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<string> Sources);

public sealed record AssistantConversationDetail(
    Guid Id,
    string Title,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<AssistantHistoryMessage> Messages,
    IReadOnlyList<AssistantArtifact> Artifacts);

public sealed record AssistantHistoryMessage(
    string Role,
    string Content,
    DateTime CreatedAtUtc);

internal sealed record PersistedChatMessage(
    string Role,
    string Content,
    string? ToolCallId = null);

public sealed class EfAssistantConversationStore : IAssistantConversationStore, IAssistantConversationHistory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly AppDbContext _db;

    public EfAssistantConversationStore(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ChatMessage>> GetOrCreateAsync(
        ConversationKey key,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.Set<AssistantConversation>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == key.ConversationId
                     && c.WorkspaceId == key.WorkspaceId
                     && c.UserId == key.UserId,
                cancellationToken);

        if (entity is null)
        {
            return [];
        }

        return DeserializeMessages(entity.MessagesJson);
    }

    public async Task SaveAsync(
        ConversationKey key,
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<AssistantArtifact>? artifacts = null,
        string? firstUserMessageForTitle = null,
        string? generatedTitle = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var persisted = SerializeMessages(messages);
        var messagesJson = JsonSerializer.Serialize(persisted, JsonOptions);
        var artifactsJson = artifacts is { Count: > 0 }
            ? JsonSerializer.Serialize(artifacts, JsonOptions)
            : null;
        var turnSources = ConversationSources.Detect(messages, artifacts);
        var userFallback = firstUserMessageForTitle ?? FindFirstUserText(persisted);
        var llmTitle = ConversationTitle.Normalize(generatedTitle);

        var entity = await _db.Set<AssistantConversation>()
            .FirstOrDefaultAsync(
                c => c.Id == key.ConversationId
                     && c.WorkspaceId == key.WorkspaceId
                     && c.UserId == key.UserId,
                cancellationToken);

        if (entity is null)
        {
            entity = new AssistantConversation
            {
                Id = key.ConversationId,
                WorkspaceId = key.WorkspaceId,
                UserId = key.UserId,
                Title = llmTitle ?? ConversationTitle.FromFirstMessage(userFallback),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                MessagesJson = messagesJson,
                LastArtifactsJson = artifactsJson,
                SourcesJson = ConversationSources.MergeJson(null, turnSources)
            };
            _db.Set<AssistantConversation>().Add(entity);
        }
        else
        {
            entity.MessagesJson = messagesJson;
            entity.UpdatedAtUtc = now;
            entity.SourcesJson = ConversationSources.MergeJson(entity.SourcesJson, turnSources);
            if (artifactsJson is not null)
            {
                entity.LastArtifactsJson = artifactsJson;
            }

            if (llmTitle is not null
                && (string.IsNullOrWhiteSpace(entity.Title)
                    || ConversationTitle.LooksLikeCopiedUserMessage(entity.Title, userFallback)))
            {
                entity.Title = llmTitle;
            }
            else if (string.IsNullOrWhiteSpace(entity.Title))
            {
                entity.Title = ConversationTitle.FromFirstMessage(userFallback);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AssistantConversationSummary>> ListAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.Set<AssistantConversation>()
            .AsNoTracking()
            .Where(c => c.WorkspaceId == workspaceId && c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAtUtc)
            .Select(c => new { c.Id, c.Title, c.CreatedAtUtc, c.UpdatedAtUtc, c.SourcesJson, c.LastArtifactsJson })
            .ToListAsync(cancellationToken);

        return rows.Select(c =>
        {
            var sources = ConversationSources.Parse(c.SourcesJson);
            if (sources.Count == 0)
            {
                sources = ConversationSources.InferFromArtifactsJson(c.LastArtifactsJson);
            }

            return new AssistantConversationSummary(
                c.Id,
                c.Title,
                c.CreatedAtUtc,
                c.UpdatedAtUtc,
                sources);
        }).ToList();
    }

    public async Task<AssistantConversationDetail?> GetAsync(
        Guid workspaceId,
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.Set<AssistantConversation>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == conversationId && c.WorkspaceId == workspaceId && c.UserId == userId,
                cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var persisted = JsonSerializer.Deserialize<List<PersistedChatMessage>>(entity.MessagesJson, JsonOptions)
                        ?? [];

        var uiMessages = persisted
            .Where(m => m.Role is "user" or "assistant")
            .Where(m => !string.IsNullOrWhiteSpace(m.Content))
            .Select(m => new AssistantHistoryMessage(m.Role, m.Content, entity.UpdatedAtUtc))
            .ToList();

        IReadOnlyList<AssistantArtifact> artifacts = string.IsNullOrWhiteSpace(entity.LastArtifactsJson)
            ? []
            : JsonSerializer.Deserialize<List<AssistantArtifact>>(entity.LastArtifactsJson, JsonOptions)
              ?? [];

        return new AssistantConversationDetail(
            entity.Id,
            entity.Title,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            uiMessages,
            artifacts);
    }

    public async Task<bool> DeleteAsync(
        Guid workspaceId,
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.Set<AssistantConversation>()
            .FirstOrDefaultAsync(
                c => c.Id == conversationId && c.WorkspaceId == workspaceId && c.UserId == userId,
                cancellationToken);

        if (entity is null)
        {
            return false;
        }

        _db.Set<AssistantConversation>().Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static List<PersistedChatMessage> SerializeMessages(IReadOnlyList<ChatMessage> messages)
    {
        var list = new List<PersistedChatMessage>(messages.Count);
        foreach (var message in messages)
        {
            if (message is SystemChatMessage)
            {
                continue;
            }

            if (message is UserChatMessage user)
            {
                var text = string.Concat(user.Content.Select(p => p.Text));
                if (!string.IsNullOrWhiteSpace(text))
                {
                    list.Add(new PersistedChatMessage("user", text.Trim()));
                }

                continue;
            }

            if (message is AssistantChatMessage assistant)
            {
                var text = string.Concat(assistant.Content.Select(p => p.Text));
                if (!string.IsNullOrWhiteSpace(text))
                {
                    list.Add(new PersistedChatMessage("assistant", text.Trim()));
                }

                continue;
            }

            if (message is ToolChatMessage tool)
            {
                var text = string.Concat(tool.Content.Select(p => p.Text));
                list.Add(new PersistedChatMessage("tool", text, tool.ToolCallId));
            }
        }

        return list;
    }

    private static List<ChatMessage> DeserializeMessages(string json)
    {
        var persisted = JsonSerializer.Deserialize<List<PersistedChatMessage>>(json, JsonOptions) ?? [];
        var result = new List<ChatMessage>(persisted.Count);

        foreach (var item in persisted)
        {
            // Skip tool rows for LLM reload — final assistant answers already summarize tool output.
            if (item.Role == "user" && !string.IsNullOrWhiteSpace(item.Content))
            {
                result.Add(new UserChatMessage(item.Content));
            }
            else if (item.Role == "assistant" && !string.IsNullOrWhiteSpace(item.Content))
            {
                result.Add(new AssistantChatMessage([ChatMessageContentPart.CreateTextPart(item.Content)]));
            }
        }

        return result;
    }

    private static string FindFirstUserText(IReadOnlyList<PersistedChatMessage> messages) =>
        messages.FirstOrDefault(m => m.Role == "user" && !string.IsNullOrWhiteSpace(m.Content))?.Content
        ?? string.Empty;
}
