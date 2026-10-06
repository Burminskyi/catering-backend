using System.Text.Json.Nodes;

namespace CateringSaaS.Modules.Assistant.Contracts;

public interface IAssistantTool
{
    string Name { get; }

    /// <summary>English description for the LLM tool catalog.</summary>
    string Description { get; }

    JsonObject ParametersSchema { get; }

    Task<ToolResult> ExecuteAsync(JsonObject args, AssistantScope scope, CancellationToken ct);
}
