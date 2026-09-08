using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartDesk.Application.Abstractions;
using SmartDesk.Domain.Entities;

namespace SmartDesk.Application.Agents.Tools;

/// <summary>
/// The allow-list (spec section 9.5). A tool is callable only if it is registered here AND the calling
/// agent's own allow-list names it. Everything else is refused - never fuzzy-matched, never guessed.
/// Every call is timed and persisted to AgentToolCalls for observability (spec section 9.9).
/// </summary>
public sealed class ToolRegistry(
    IEnumerable<IAgentTool> tools,
    IAppDbContext db,
    ILogger<ToolRegistry> logger)
{
    private readonly Dictionary<string, IAgentTool> _tools =
        tools.ToDictionary(t => t.Name, StringComparer.Ordinal);

    public IReadOnlyCollection<IAgentTool> All => _tools.Values;

    public bool Exists(string name) => _tools.ContainsKey(name);

    /// <summary>
    /// Invokes a tool on behalf of a named agent, enforcing that agent's allow-list first.
    /// Records the call (input, output, success, duration) whether it succeeds or fails.
    /// </summary>
    public async Task<ToolResult> InvokeAsync(
        string agentName,
        IReadOnlyCollection<string> agentAllowList,
        string toolName,
        object input,
        ToolContext context,
        int? stepId,
        CancellationToken ct = default)
    {
        // Least privilege: the agent's own allow-list is checked before the global registry.
        if (!agentAllowList.Contains(toolName, StringComparer.Ordinal))
        {
            var error = $"Tool '{toolName}' is not in the allow-list for {agentName}.";
            logger.LogWarning("Blocked tool call: {Agent} -> {Tool}", agentName, toolName);
            await RecordAsync(context, stepId, toolName, input, null, false, error, 0, ct);
            return ToolResult.Fail(error);
        }

        if (!_tools.TryGetValue(toolName, out var tool))
        {
            var error = $"Tool '{toolName}' is not registered.";
            await RecordAsync(context, stepId, toolName, input, null, false, error, 0, ct);
            return ToolResult.Fail(error);
        }

        var sw = Stopwatch.StartNew();
        ToolResult result;
        try
        {
            var element = JsonSerializer.SerializeToElement(input);
            result = await tool.ExecuteAsync(context, element, ct);
        }
        catch (Exception ex)
        {
            // Tools must fail safely: an exception becomes a structured failure, never an unhandled crash.
            logger.LogError(ex, "Tool {Tool} threw for workflow {WorkflowId}", toolName, context.WorkflowId);
            result = ToolResult.Fail($"Tool '{toolName}' failed: {ex.Message}");
        }
        sw.Stop();

        await RecordAsync(context, stepId, toolName, input, result.Data, result.Success, result.Error,
            (int)sw.ElapsedMilliseconds, ct);
        return result;
    }

    private async Task RecordAsync(
        ToolContext context, int? stepId, string toolName, object? input, object? output,
        bool success, string? error, int durationMs, CancellationToken ct)
    {
        db.AgentToolCalls.Add(new AgentToolCall
        {
            WorkflowId = context.WorkflowId,
            StepId = stepId,
            ToolName = toolName,
            InputJson = input is null ? null : JsonSerializer.Serialize(input),
            OutputJson = output is null ? null : JsonSerializer.Serialize(output),
            Success = success,
            ErrorMessage = error,
            DurationMs = durationMs,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }
}
