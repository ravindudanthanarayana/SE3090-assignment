using System.Text.Json;

namespace SmartDesk.Application.Agents.Tools;

/// <summary>
/// Scope handed to every tool call. A tool can only ever act inside this scope - it cannot widen it,
/// and it cannot see any user or ticket other than the ones the orchestrator put here.
/// </summary>
public sealed record ToolContext(int WorkflowId, int TicketId, int? ActingUserId);

public sealed record ToolResult(bool Success, object? Data, string? Error)
{
    public static ToolResult Ok(object data) => new(true, data, null);
    public static ToolResult Fail(string error) => new(false, null, error);
}

/// <summary>
/// Every capability an agent has. There is no other path from an agent to the database or the network:
/// no raw SQL, no arbitrary HTTP, no file system, no shell (spec section 9.5).
/// </summary>
public interface IAgentTool
{
    string Name { get; }
    string Description { get; }

    /// <summary>Executes the tool. Implementations must validate their own input and never throw.</summary>
    Task<ToolResult> ExecuteAsync(ToolContext context, JsonElement input, CancellationToken ct = default);
}
