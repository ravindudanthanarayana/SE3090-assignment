using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Agents.Contracts;
using SmartDesk.Application.Agents.Tools;
using SmartDesk.Application.BusinessRules;
using SmartDesk.Application.Common;
using SmartDesk.Domain.Entities;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Application.Agents;

/// <summary>
/// Runs one agentic workflow end to end (spec section 9.1).
///
/// The orchestrator is a plain deterministic loop. It asks the Planner for a plan, executes each planned
/// step through the named agent, validates every output, persists every step, applies the deterministic
/// business rules, and pauses for human approval on high-impact actions. The language model never
/// controls the loop, never chooses permissions and never writes to the database.
/// </summary>
public sealed class WorkflowOrchestrator(
    IAppDbContext db,
    PlannerAgent planner,
    IEnumerable<IWorkflowAgent> agents,
    ToolRegistry tools,
    IAuditService audit,
    IClock clock,
    IOptions<AgentOptions> options,
    ILogger<WorkflowOrchestrator> logger)
{
    private readonly Dictionary<string, IWorkflowAgent> _agents =
        agents.ToDictionary(a => a.Name, StringComparer.Ordinal);
    private readonly AgentOptions _options = options.Value;

    /// <summary>Creates the workflow row up front so state exists even if the run fails immediately.</summary>
    public async Task<AgentWorkflow> CreateAsync(int ticketId, string objective, int? startedByUserId, CancellationToken ct = default)
    {
        var workflow = new AgentWorkflow
        {
            TicketId = ticketId,
            Objective = objective,
            Status = WorkflowStatus.Planned,
            StartedByUserId = startedByUserId,
            StartedAt = clock.UtcNow,
            CreatedAt = clock.UtcNow,
            UpdatedAt = clock.UtcNow
        };
        db.AgentWorkflows.Add(workflow);
        await db.SaveChangesAsync(ct);

        await audit.LogAsync("AgentWorkflow", workflow.Id, "WorkflowCreated", ActorType.System, startedByUserId,
            new { ticketId, objective }, ct);

        return workflow;
    }

    /// <summary>
    /// Executes the workflow. Never throws: every failure path ends in a persisted Failed state
    /// with a recorded reason, which is the "safe, clearly recorded failure" spec section 9.1 requires.
    /// </summary>
    public async Task RunAsync(int workflowId, CancellationToken ct = default)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct);
        budget.CancelAfter(TimeSpan.FromSeconds(_options.WorkflowTimeoutSeconds));
        var token = budget.Token;

        var workflow = await db.AgentWorkflows.FirstOrDefaultAsync(w => w.Id == workflowId, ct);
        if (workflow is null)
        {
            logger.LogWarning("Workflow {WorkflowId} not found", workflowId);
            return;
        }

        try
        {
            await RunInternalAsync(workflow, token);
        }
        catch (OperationCanceledException)
        {
            await FailAsync(workflow, $"Workflow exceeded its {_options.WorkflowTimeoutSeconds}s budget.", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Workflow {WorkflowId} failed", workflowId);
            await FailAsync(workflow, ex.Message, ct);
        }
    }

    private async Task RunInternalAsync(AgentWorkflow workflow, CancellationToken ct)
    {
        var ticket = await db.Tickets
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == workflow.TicketId, ct)
            ?? throw new NotFoundException("Ticket", workflow.TicketId);

        var categoryNames = await db.TicketCategories
            .Where(c => c.IsActive)
            .Select(c => c.Name)
            .ToListAsync(ct);

        // ---- Step 0: plan -------------------------------------------------------------
        workflow.Status = WorkflowStatus.Running;
        var plan = await BuildPlanAsync(workflow, ticket, ct);
        workflow.PlanJson = JsonSerializer.Serialize(plan);
        workflow.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync("AgentWorkflow", workflow.Id, "PlanCreated", ActorType.Agent, null,
            new { steps = plan.Steps.Select(s => s.Agent) }, ct);

        // ---- Steps 1..n: delegate to the named specialist agents ----------------------
        var scratchpad = new WorkflowScratchpad();
        var toolContext = new ToolContext(workflow.Id, ticket.Id, workflow.StartedByUserId);

        foreach (var planStep in plan.Steps)
        {
            ct.ThrowIfCancellationRequested();

            if (!_agents.TryGetValue(planStep.Agent, out var agent))
            {
                // Cannot happen after plan validation, but a plan that names a missing agent
                // must degrade rather than crash.
                await RecordSkippedStepAsync(workflow, planStep, $"Agent '{planStep.Agent}' is not registered.", ct);
                continue;
            }

            var step = await BeginStepAsync(workflow, planStep, ct);
            var runContext = new AgentRunContext
            {
                Tool = toolContext,
                Ticket = ticket,
                CategoryNames = categoryNames,
                Scratchpad = scratchpad,
                StepId = step.Id
            };

            var ok = await ExecuteStepWithRetriesAsync(agent, step, runContext, ct);
            if (!ok)
            {
                // The Validation agent is the safety gate; without it we will not act on anything.
                if (agent.Name == AgentNames.Validation)
                {
                    await FailAsync(workflow, $"{agent.Name} failed: {step.ErrorMessage}", ct);
                    return;
                }
                logger.LogWarning("Step {Agent} failed but the workflow continues: {Error}", agent.Name, step.ErrorMessage);
            }
        }

        // ---- Deterministic business rules decide what actually happens ----------------
        await ApplyDecisionAsync(workflow, ticket, scratchpad, ct);
    }

    private async Task<PlanResult> BuildPlanAsync(AgentWorkflow workflow, Ticket ticket, CancellationToken ct)
    {
        var summary = $"{ticket.TicketNumber}: {ticket.Title}\n{ticket.Description}";
        var sw = Stopwatch.StartNew();

        var step = new AgentStep
        {
            WorkflowId = workflow.Id,
            StepOrder = 0,
            AgentName = AgentNames.Planner,
            Purpose = "Create the delegation plan",
            Status = AgentStepStatus.Running,
            InputJson = JsonSerializer.Serialize(new { objective = workflow.Objective, ticketId = ticket.Id }),
            StartedAt = clock.UtcNow
        };
        db.AgentSteps.Add(step);
        await db.SaveChangesAsync(ct);

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(_options.LlmTimeoutSeconds));

            var plan = await planner.PlanAsync(workflow.Objective, summary, timeout.Token);
            sw.Stop();

            step.Status = AgentStepStatus.Succeeded;
            step.OutputJson = JsonSerializer.Serialize(plan);
            step.ValidationJson = JsonSerializer.Serialize(new { schemaValid = true, source = "model" });
            step.DurationMs = (int)sw.ElapsedMilliseconds;
            step.CompletedAt = clock.UtcNow;
            await db.SaveChangesAsync(ct);
            return plan;
        }
        catch (Exception ex)
        {
            // Safe degradation: a bad or unavailable planner must not stop the specialists running.
            sw.Stop();
            var fallback = PlannerAgent.DefaultPlan();
            step.Status = AgentStepStatus.Succeeded;
            step.OutputJson = JsonSerializer.Serialize(fallback);
            step.ValidationJson = JsonSerializer.Serialize(new
            {
                schemaValid = false,
                source = "fallback",
                reason = ex.Message
            });
            step.ErrorMessage = $"Planner unavailable, used the default plan: {ex.Message}";
            step.DurationMs = (int)sw.ElapsedMilliseconds;
            step.CompletedAt = clock.UtcNow;
            await db.SaveChangesAsync(ct);

            logger.LogWarning(ex, "Planner failed for workflow {WorkflowId}; using the default plan", workflow.Id);
            return fallback;
        }
    }

    private async Task<AgentStep> BeginStepAsync(AgentWorkflow workflow, PlanStep planStep, CancellationToken ct)
    {
        var step = new AgentStep
        {
            WorkflowId = workflow.Id,
            StepOrder = planStep.Order,
            AgentName = planStep.Agent,
            Purpose = planStep.Purpose,
            Status = AgentStepStatus.Running,
            StartedAt = clock.UtcNow
        };
        db.AgentSteps.Add(step);

        workflow.CurrentStep = planStep.Agent;
        workflow.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        return step;
    }

    /// <summary>Runs one agent with a bounded retry budget. Returns false on final failure.</summary>
    private async Task<bool> ExecuteStepWithRetriesAsync(
        IWorkflowAgent agent, AgentStep step, AgentRunContext ctx, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        for (var attempt = 0; attempt <= _options.MaxRetriesPerStep; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            step.RetryCount = attempt;

            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(TimeSpan.FromSeconds(_options.LlmTimeoutSeconds));

                var (input, output) = await agent.ExecuteAsync(ctx, timeout.Token);
                sw.Stop();

                step.Status = AgentStepStatus.Succeeded;
                step.InputJson = JsonSerializer.Serialize(input);
                step.OutputJson = JsonSerializer.Serialize(output);
                step.ValidationJson = JsonSerializer.Serialize(new { schemaValid = true, attempts = attempt + 1 });
                step.DurationMs = (int)sw.ElapsedMilliseconds;
                step.CompletedAt = clock.UtcNow;
                await db.SaveChangesAsync(ct);

                await audit.LogAsync("AgentStep", step.Id, "AgentCompleted", ActorType.Agent, null,
                    new { agent = agent.Name, attempts = attempt + 1, durationMs = step.DurationMs }, ct);
                return true;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw; // the whole-workflow budget expired; let RunAsync record it
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Agent {Agent} attempt {Attempt} failed", agent.Name, attempt + 1);
                step.ErrorMessage = ex.Message;

                if (attempt < _options.MaxRetriesPerStep)
                {
                    await Task.Delay(_options.RetryBackoffMs * (int)Math.Pow(2, attempt), ct);
                    continue;
                }

                sw.Stop();
                step.Status = AgentStepStatus.Failed;
                step.ValidationJson = JsonSerializer.Serialize(new
                {
                    schemaValid = false,
                    attempts = attempt + 1,
                    error = ex.Message
                });
                step.DurationMs = (int)sw.ElapsedMilliseconds;
                step.CompletedAt = clock.UtcNow;
                await db.SaveChangesAsync(ct);
                return false;
            }
        }

        return false;
    }

    private async Task RecordSkippedStepAsync(AgentWorkflow workflow, PlanStep planStep, string reason, CancellationToken ct)
    {
        db.AgentSteps.Add(new AgentStep
        {
            WorkflowId = workflow.Id,
            StepOrder = planStep.Order,
            AgentName = planStep.Agent,
            Purpose = planStep.Purpose,
            Status = AgentStepStatus.Skipped,
            ErrorMessage = reason,
            StartedAt = clock.UtcNow,
            CompletedAt = clock.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Turns validated agent advice into actual system behaviour. Low-impact changes are applied here;
    /// high-impact ones become Pending approval requests and the workflow parks (spec section 9.8).
    /// </summary>
    private async Task ApplyDecisionAsync(
        AgentWorkflow workflow, Ticket ticket, WorkflowScratchpad pad, CancellationToken ct)
    {
        var categories = await db.TicketCategories
            .Where(c => c.IsActive)
            .ToDictionaryAsync(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase, ct);

        var activeAgentIds = await db.Users
            .Where(u => u.IsActive && u.Role.Name == Domain.Common.RoleNames.SupportAgent)
            .Select(u => u.Id)
            .ToListAsync(ct);

        var actualSla = SlaCalculator.GetState(ticket, clock.UtcNow);

        var decision = BusinessRuleEngine.Evaluate(
            ticket, pad.Triage, pad.Assignment, pad.Validation,
            categories, activeAgentIds, actualSla);

        var applied = new List<string>();

        // --- Low-impact: category correction
        if (decision.ApplyCategoryId is int newCategoryId)
        {
            var old = ticket.Category.Name;
            ticket.CategoryId = newCategoryId;
            AddHistory(ticket, "Category", old, categories.First(c => c.Value == newCategoryId).Key,
                "Applied from AI triage");
            applied.Add($"Category set from AI triage");
        }

        // --- Low-impact: priority raise (lowering needs approval, handled as an approval action)
        if (decision.ApplyPriority is TicketPriority newPriority)
        {
            var old = ticket.Priority;
            ticket.Priority = newPriority;
            ticket.SlaDueAt = SlaCalculator.CalculateDueAt(ticket.CreatedAt, ticket.Category.DefaultSlaHours, newPriority);
            AddHistory(ticket, "Priority", old.ToString(), newPriority.ToString(), "Raised by AI triage");
            applied.Add($"Priority raised to {newPriority}");
        }

        // --- Low-impact: attach the knowledge articles the Solution agent actually retrieved
        if (pad.Solution is { MatchedArticleIds.Count: > 0 })
        {
            var existing = await db.TicketArticleLinks
                .Where(l => l.TicketId == ticket.Id)
                .Select(l => l.ArticleId)
                .ToListAsync(ct);

            foreach (var articleId in pad.Solution.MatchedArticleIds.Where(id => !existing.Contains(id)))
            {
                db.TicketArticleLinks.Add(new TicketArticleLink
                {
                    TicketId = ticket.Id,
                    ArticleId = articleId,
                    Source = ArticleLinkSource.Agent,
                    RelevanceScore = pad.Solution.Confidence,
                    CreatedAt = clock.UtcNow
                });
            }
            applied.Add($"{pad.Solution.MatchedArticleIds.Count} knowledge article(s) suggested");
        }

        ticket.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);

        // --- High-impact: request approval through the allow-listed tool. Nothing is executed here.
        var pending = new List<string>();
        var approvalTool = new List<string> { "RequestHumanApproval" };
        var toolContext = new ToolContext(workflow.Id, ticket.Id, workflow.StartedByUserId);

        foreach (var action in decision.RequiresApproval)
        {
            var result = await tools.InvokeAsync(
                AgentNames.Validation, approvalTool, "RequestHumanApproval",
                new
                {
                    actionType = action.ActionType.ToString(),
                    targetUserId = action.TargetUserId,
                    targetPriority = action.TargetPriority?.ToString(),
                    reason = action.Description
                },
                toolContext, null, ct);

            if (result.Success) pending.Add(action.Description);
            else logger.LogWarning("Approval request rejected: {Error}", result.Error);
        }

        var outcome = new WorkflowOutcome
        {
            Triage = pad.Triage,
            Solution = pad.Solution,
            Assignment = pad.Assignment,
            Validation = pad.Validation,
            AppliedActions = applied,
            PendingApprovalActions = pending,
            Summary = decision.Summary +
                      (decision.Violations.Count > 0
                          ? $" {decision.Violations.Count} rule violation(s) recorded."
                          : string.Empty)
        };

        workflow.FinalOutcomeJson = JsonSerializer.Serialize(outcome);
        workflow.Status = pending.Count > 0 ? WorkflowStatus.AwaitingApproval : WorkflowStatus.Completed;
        workflow.CurrentStep = pending.Count > 0 ? "AwaitingApproval" : "Completed";
        if (pending.Count == 0) workflow.CompletedAt = clock.UtcNow;
        workflow.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync("AgentWorkflow", workflow.Id,
            pending.Count > 0 ? "WorkflowAwaitingApproval" : "WorkflowCompleted",
            ActorType.System, null,
            new { applied, pending, violations = decision.Violations }, ct);
    }

    private void AddHistory(Ticket ticket, string field, string? oldValue, string? newValue, string note)
    {
        db.TicketHistory.Add(new TicketHistoryEntry
        {
            TicketId = ticket.Id,
            ChangedByUserId = null, // null means the change came from the system/agent, not a person
            Field = field,
            OldValue = oldValue,
            NewValue = newValue,
            Note = note,
            CreatedAt = clock.UtcNow
        });
    }

    private async Task FailAsync(AgentWorkflow workflow, string error, CancellationToken ct)
    {
        workflow.Status = WorkflowStatus.Failed;
        workflow.ErrorMessage = error.Length > 2000 ? error[..2000] : error;
        workflow.CompletedAt = clock.UtcNow;
        workflow.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);

        await audit.LogAsync("AgentWorkflow", workflow.Id, "WorkflowFailed", ActorType.System, null,
            new { error = workflow.ErrorMessage }, ct);
    }
}
