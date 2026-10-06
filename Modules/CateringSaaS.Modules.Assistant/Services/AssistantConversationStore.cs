using System.Collections.Concurrent;
using OpenAI.Chat;

namespace CateringSaaS.Modules.Assistant.Services;

/// <summary>
/// In-memory conversation history for multi-turn tool calling within a session.
/// </summary>
public interface IAssistantConversationStore
{
    IReadOnlyList<ChatMessage> GetOrCreate(Guid conversationId);

    void Save(Guid conversationId, IReadOnlyList<ChatMessage> messages);
}

public sealed class InMemoryAssistantConversationStore : IAssistantConversationStore
{
    private static readonly ConcurrentDictionary<Guid, List<ChatMessage>> Store = new();

    public IReadOnlyList<ChatMessage> GetOrCreate(Guid conversationId) =>
        Store.GetOrAdd(conversationId, _ => []);

    public void Save(Guid conversationId, IReadOnlyList<ChatMessage> messages) =>
        Store[conversationId] = messages.ToList();
}
