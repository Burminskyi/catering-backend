using System.ClientModel;
using System.Text;
using OpenAI.Chat;

namespace CateringSaaS.Modules.Assistant.Services;

/// <summary>
/// Reassembles streamed function-call fragments into complete tool calls.
/// Text from a tool-call turn is intentionally not forwarded to the client.
/// </summary>
internal sealed class StreamingToolCallAccumulator
{
    private readonly Dictionary<int, string> _ids = [];
    private readonly Dictionary<int, string> _names = [];
    private readonly Dictionary<int, StringBuilder> _arguments = [];

    public void Append(StreamingChatToolCallUpdate update)
    {
        if (!string.IsNullOrEmpty(update.ToolCallId))
        {
            _ids[update.Index] = update.ToolCallId;
        }

        if (!string.IsNullOrEmpty(update.FunctionName))
        {
            _names[update.Index] = update.FunctionName;
        }

        var chunk = update.FunctionArgumentsUpdate;
        if (chunk is null || chunk.ToMemory().IsEmpty)
        {
            return;
        }

        if (!_arguments.TryGetValue(update.Index, out var builder))
        {
            builder = new StringBuilder();
            _arguments[update.Index] = builder;
        }

        builder.Append(chunk.ToString());
    }

    public IReadOnlyList<ChatToolCall> Build()
    {
        if (_ids.Count == 0)
        {
            return [];
        }

        var calls = new List<ChatToolCall>(_ids.Count);
        foreach (var (index, id) in _ids.OrderBy(pair => pair.Key))
        {
            _names.TryGetValue(index, out var name);
            _arguments.TryGetValue(index, out var arguments);
            var json = arguments is { Length: > 0 } ? arguments.ToString() : "{}";
            calls.Add(ChatToolCall.CreateFunctionToolCall(
                id,
                name ?? string.Empty,
                BinaryData.FromString(json)));
        }

        return calls;
    }
}
