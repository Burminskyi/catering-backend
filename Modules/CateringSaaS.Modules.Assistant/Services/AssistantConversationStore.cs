using System.Collections.Concurrent;
using OpenAI.Chat;

namespace CateringSaaS.Modules.Assistant.Services;

public readonly record struct ConversationKey(Guid WorkspaceId, Guid UserId, Guid ConversationId);

/// <summary>
/// In-memory conversation history scoped to workspace and user so ids cannot cross tenants.
/// </summary>
public interface IAssistantConversationStore
{
    IReadOnlyList<ChatMessage> GetOrCreate(ConversationKey key);

    void Save(ConversationKey key, IReadOnlyList<ChatMessage> messages);
}

public sealed class InMemoryAssistantConversationStore : IAssistantConversationStore
{
    private static readonly ConcurrentDictionary<ConversationKey, List<ChatMessage>> Store = new();

    public IReadOnlyList<ChatMessage> GetOrCreate(ConversationKey key) =>
        Store.GetOrAdd(key, _ => []);

    public void Save(ConversationKey key, IReadOnlyList<ChatMessage> messages) =>
        Store[key] = messages.Where(message => message is not SystemChatMessage).ToList();
}
