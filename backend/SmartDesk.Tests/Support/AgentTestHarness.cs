using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Agents;
using SmartDesk.Application.Agents.Tools;
using SmartDesk.Application.Common;
using SmartDesk.Infrastructure.Ai;
using SmartDesk.Infrastructure.Persistence;

namespace SmartDesk.Tests.Support;

/// <summary>
/// Builds a complete, real agent subsystem - the same orchestrator, agents, tools, registry and
/// validators that run in production - with only the language model swapped for a scripted one.
///
/// That swap is what makes the golden cases deterministic and offline, which spec section 12 requires
/// when it says LLM-as-a-judge must not be the only evaluation method.
/// </summary>
public sealed class AgentTestHarness : IDisposable
{
    public AppDbContext Db { get; }
    public FakeClock Clock { get; }
    public RecordingAudit Audit { get; } = new();
    public ToolRegistry Tools { get; }
    public WorkflowOrchestrator Orchestrator { get; }
    public List<IWorkflowAgent> Agents { get; }

    public AgentTestHarness(
        Func<string, string, string>? llmResponder = null,
        AgentOptions? options = null)
    {
        Clock = FakeClock.Default;
        Db = TestDb.Create(Clock.UtcNow);

        var llm = llmResponder is null ? new ScriptedLlmClient() : new ScriptedLlmClient(llmResponder);

        var toolList = new List<IAgentTool>
        {
            new GetTicketTool(Db, Clock),
            new SearchKnowledgeBaseTool(Db),
            new GetSupportAgentsTool(Db),
            new GetAgentWorkloadTool(Db, Clock),
            new ScoreAssignmentCandidatesTool(Db),
            new CheckSlaTool(Db, Clock),
            new RequestHumanApprovalTool(Db, Clock),
            // Registered but in no agent's allow-list, exactly as in production.
            new ExecuteApprovedActionTool(Db, new NoOpExecutor())
        };

        Tools = new ToolRegistry(toolList, Db, NullLogger<ToolRegistry>.Instance);

        Agents =
        [
            new TriageAgent(llm, Tools),
            new SolutionAgent(llm, Tools),
            new AssignmentAgent(llm, Tools),
            new ValidationAgent(llm, Tools)
        ];

        Orchestrator = new WorkflowOrchestrator(
            Db,
            new PlannerAgent(llm),
            Agents,
            Tools,
            Audit,
            Clock,
            Options.Create(options ?? new AgentOptions { RetryBackoffMs = 1 }),
            NullLogger<WorkflowOrchestrator>.Instance);
    }

    public void Dispose() => Db.Dispose();

    private sealed class NoOpExecutor : IApprovalActionExecutor
    {
        public Task ExecuteAsync(Domain.Entities.AiApproval approval, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
