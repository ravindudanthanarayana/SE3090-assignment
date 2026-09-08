using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Agents.Contracts;
using SmartDesk.Application.Agents.Prompts;

namespace SmartDesk.Application.Agents;

/// <summary>
/// Agent 0 - Planner / Coordinator. Group-owned; the evidence for the group "Agent Orchestration"
/// criterion. Responsibility: turn the objective into an ordered, validated delegation plan.
///
/// It is not an IWorkflowAgent because it never appears as a step in its own plan and it has no
/// tools at all - planning needs no system access, which is itself a least-privilege decision.
/// </summary>
public sealed class PlannerAgent(ILlmClient llm)
{
    public string Name => AgentNames.Planner;
    public IReadOnlyCollection<string> AllowedTools { get; } = [];

    /// <summary>
    /// Produces the plan. If the model's plan fails validation the orchestrator falls back to the
    /// default plan rather than failing the whole workflow - a safe degradation, recorded as a violation.
    /// </summary>
    public async Task<PlanResult> PlanAsync(string objective, string ticketSummary, CancellationToken ct = default)
    {
        var userContent = $"""
            Objective: {PromptSanitizer.Sanitize(objective)}

            {PromptSanitizer.WrapUntrusted("ticketSummary", ticketSummary)}

            Produce the delegation plan.
            """;

        var raw = await llm.CompleteJsonAsync(SystemPrompts.Planner, userContent, ct);
        var parsed = AgentOutputValidator.Parse<PlanResult>(raw, Name);
        return AgentOutputValidator.ValidatePlan(parsed);
    }

    /// <summary>
    /// The deterministic fallback plan. Used when the model is unavailable or returns an invalid plan,
    /// so the workflow still runs its four specialists in a correct dependency order.
    /// </summary>
    public static PlanResult DefaultPlan() => new()
    {
        Rationale = "Default plan: classify, then research and route in parallel order, then validate.",
        Steps =
        [
            new PlanStep { Order = 1, Agent = AgentNames.Triage, Purpose = "Classify category, priority and urgency", ExpectedOutput = "TriageResult" },
            new PlanStep { Order = 2, Agent = AgentNames.Solution, Purpose = "Find knowledge articles and troubleshooting steps", ExpectedOutput = "SolutionResult" },
            new PlanStep { Order = 3, Agent = AgentNames.Assignment, Purpose = "Recommend a support agent by skill and workload", ExpectedOutput = "AssignmentResult" },
            new PlanStep { Order = 4, Agent = AgentNames.Validation, Purpose = "Validate results and decide escalation", ExpectedOutput = "ValidationResult" }
        ]
    };
}
