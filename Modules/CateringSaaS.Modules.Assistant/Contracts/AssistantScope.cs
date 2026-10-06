namespace CateringSaaS.Modules.Assistant.Contracts;

/// <summary>
/// Tenant/user scope bound from JWT. Tools must never accept WorkspaceId from the LLM.
/// </summary>
public sealed record AssistantScope(Guid WorkspaceId, Guid UserId);
