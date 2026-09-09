using SmartDesk.Application.Agents.Contracts;
using SmartDesk.Application.Agents.Tools;
using SmartDesk.Domain.Entities;

namespace SmartDesk.Application.Agents;

/// <summary>
/// Mutable results shared between the agents in one workflow run. Each agent reads what earlier
/// agents produced and writes its own typed result. This is the in-memory view of the state that
/// the orchestrator also persists to AgentSteps.
/// </summary>
public sealed class WorkflowScratchpad
{
    public TriageResult? Triage { get; set; }
    public SolutionResult? Solution { get; set; }
    public AssignmentResult? Assignment { get; set; }
    public ValidationResult? Validation { get; set; }

    /// <summary>Article ids the SearchKnowledgeBase tool actually returned, used to validate Solution output.</summary>
    public List<int> RetrievedArticleIds { get; } = [];

    /// <summary>Candidate user ids the scoring tool actually returned, used to validate Assignment output.</summary>
    public List<int> ScoredCandidateUserIds { get; } = [];
}

/// <summary>Everything an agent is allowed to see for one execution.</summary>
public sealed class AgentRunContext
{
    public required ToolContext Tool { get; init; }
    public required Ticket Ticket { get; init; }
    public required IReadOnlyList<string> CategoryNames { get; init; }
    public required WorkflowScratchpad Scratchpad { get; init; }

    /// <summary>Set by the orchestrator so tool calls can be attributed to the right AgentStep row.</summary>
    public int? StepId { get; set; }
}

/// <summary>
/// One agent. Distinctness under spec section 9.2 comes from the combination of Name (identifiable
/// responsibility), AllowedTools (controlled permissions), a dedicated system prompt, and a dedicated
/// input/output contract - not from the class name alone.
/// </summary>
public interface IWorkflowAgent
{
    string Name { get; }

    /// <summary>This agent's tool allow-list. Enforced by ToolRegistry before every call.</summary>
    IReadOnlyCollection<string> AllowedTools { get; }

    /// <summary>Returns (inputSummary, validatedOutput) for persistence. Throws on validation failure.</summary>
    Task<(object Input, object Output)> ExecuteAsync(AgentRunContext ctx, CancellationToken ct = default);
}
