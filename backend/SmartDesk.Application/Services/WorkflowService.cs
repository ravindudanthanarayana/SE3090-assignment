using Microsoft.EntityFrameworkCore;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Agents;
using SmartDesk.Application.Agents.Tools;
using SmartDesk.Application.Common;
using SmartDesk.Application.Dtos;
using SmartDesk.Domain.Common;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Application.Services;

/// <summary>
/// Read side of the agent subsystem plus workflow start. The execution itself lives in
/// WorkflowOrchestrator; this service is what the API exposes (spec section 5.6).
/// </summary>
public sealed class WorkflowService(
    IAppDbContext db,
    ICurrentUser currentUser,
    WorkflowOrchestrator orchestrator,
    ToolRegistry toolRegistry,
    IEnumerable<IWorkflowAgent> agents)
{
    /// <summary>Creates the workflow row and returns it. Execution is started separately by the caller.</summary>
    public async Task<int> StartAsync(StartWorkflowRequest request, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == request.TicketId, ct)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        // An employee may only start a workflow on their own ticket.
        if (currentUser.IsInRole(RoleNames.Employee) && ticket.CreatedByUserId != currentUser.UserId)
            throw new ForbiddenException("You may only start a workflow for your own ticket.");

        var active = await db.AgentWorkflows.AnyAsync(
            w => w.TicketId == ticket.Id &&
                 (w.Status == WorkflowStatus.Running || w.Status == WorkflowStatus.AwaitingApproval), ct);
        if (active)
            throw new ConflictException("A workflow is already running or awaiting approval for this ticket.");

        var objective = string.IsNullOrWhiteSpace(request.Objective)
            ? $"Triage, research and route ticket {ticket.TicketNumber}"
            : PromptSanitizer.Sanitize(request.Objective);

        var workflow = await orchestrator.CreateAsync(ticket.Id, objective, currentUser.UserId, ct);
        return workflow.Id;
    }

    public async Task<PagedResult<WorkflowListItemDto>> QueryAsync(WorkflowQuery query, CancellationToken ct = default)
    {
        var q = db.AgentWorkflows.AsNoTracking().Include(w => w.Ticket).AsQueryable();

        if (currentUser.IsInRole(RoleNames.Employee))
        {
            var me = currentUser.UserId ?? throw new ForbiddenException("Not authenticated.");
            q = q.Where(w => w.Ticket.CreatedByUserId == me);
        }

        if (query.Status.HasValue) q = q.Where(w => w.Status == query.Status);
        if (query.TicketId.HasValue) q = q.Where(w => w.TicketId == query.TicketId);

        q = query.Descending ? q.OrderByDescending(w => w.StartedAt) : q.OrderBy(w => w.StartedAt);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(w => new WorkflowListItemDto(
                w.Id, w.TicketId, w.Ticket.TicketNumber, w.Objective, w.Status, w.CurrentStep,
                w.Steps.Count,
                w.Approvals.Count(a => a.Status == ApprovalStatus.Pending),
                w.StartedAt, w.CompletedAt))
            .ToListAsync(ct);

        return new PagedResult<WorkflowListItemDto>(items, query.Page, query.PageSize, total);
    }

    /// <summary>The full execution summary rendered by the React workflow timeline.</summary>
    public async Task<WorkflowDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var w = await db.AgentWorkflows.AsNoTracking()
            .Include(x => x.Ticket)
            .Include(x => x.Steps)
            .Include(x => x.ToolCalls)
            .Include(x => x.Approvals).ThenInclude(a => a.DecidedByUser)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("AgentWorkflow", id);

        if (currentUser.IsInRole(RoleNames.Employee) && w.Ticket.CreatedByUserId != currentUser.UserId)
            throw new ForbiddenException("You may only view workflows for your own tickets.");

        // Tool calls made outside any agent step - the orchestrator's own RequestHumanApproval calls -
        // carry a null StepId, so they are grouped separately rather than dropped.
        var toolsByStep = w.ToolCalls
            .Where(c => c.StepId.HasValue)
            .GroupBy(c => c.StepId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var steps = w.Steps
            .OrderBy(s => s.StepOrder).ThenBy(s => s.Id)
            .Select(s => new AgentStepDto(
                s.Id, s.StepOrder, s.AgentName, s.Purpose, s.Status,
                s.InputJson, s.OutputJson, s.ValidationJson, s.ErrorMessage,
                s.RetryCount, s.DurationMs, s.StartedAt, s.CompletedAt,
                (toolsByStep.TryGetValue(s.Id, out var calls) ? calls : [])
                    .OrderBy(c => c.CreatedAt)
                    .Select(c => new ToolCallDto(c.Id, c.ToolName, c.InputJson, c.OutputJson,
                        c.Success, c.ErrorMessage, c.DurationMs, c.CreatedAt))
                    .ToList()))
            .ToList();

        var approvals = w.Approvals
            .OrderByDescending(a => a.RequestedAt)
            .Select(a => new ApprovalDto(
                a.Id, a.WorkflowId, a.TicketId, w.Ticket.TicketNumber, w.Ticket.Title,
                a.ActionType, a.ProposedActionJson, a.Reason, a.RiskLevel, a.Status,
                a.RequestedAt, a.DecidedByUser?.FullName, a.DecidedAt, a.DecisionNote))
            .ToList();

        return new WorkflowDetailDto(
            w.Id, w.TicketId, w.Ticket.TicketNumber, w.Ticket.Title, w.Objective,
            w.Status, w.CurrentStep, w.PlanJson, w.FinalOutcomeJson, w.ErrorMessage,
            w.StartedAt, w.CompletedAt, w.Steps.Sum(s => s.DurationMs), steps, approvals,
            w.ToolCalls
                .Where(c => c.StepId is null)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new ToolCallDto(c.Id, c.ToolName, c.InputJson, c.OutputJson,
                    c.Success, c.ErrorMessage, c.DurationMs, c.CreatedAt))
                .ToList());
    }

    /// <summary>
    /// Exposes the tool allow-list for demonstration and observability. Shows every registered tool
    /// and which agents may call it - ExecuteApprovedAction appears with an empty list, because no
    /// agent is permitted to call it.
    /// </summary>
    public IReadOnlyList<AgentToolInfoDto> GetTools()
    {
        var agentList = agents.ToList();
        return toolRegistry.All
            .OrderBy(t => t.Name)
            .Select(t => new AgentToolInfoDto(
                t.Name,
                t.Description,
                agentList.Where(a => a.AllowedTools.Contains(t.Name)).Select(a => a.Name).ToList()))
            .ToList();
    }
}
