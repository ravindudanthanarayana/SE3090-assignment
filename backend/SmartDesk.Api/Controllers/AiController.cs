using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDesk.Api.Infrastructure;
using SmartDesk.Application.Common;
using SmartDesk.Application.Dtos;
using SmartDesk.Application.Services;
using SmartDesk.Domain.Common;

namespace SmartDesk.Api.Controllers;

/// <summary>
/// The Agentic AI subsystem's public surface (spec section 5.6): start a workflow, review its status
/// and execution summary, and decide the human approvals it raises.
///
/// Note there is no endpoint that lets a client talk to the model or to a tool directly. The
/// orchestrator runs inside this process, which is how spec section 2's "clients must only talk to
/// ASP.NET Core" rule is satisfied structurally rather than by convention.
/// </summary>
[ApiController]
[Route("api/ai")]
[Authorize]
[Produces("application/json")]
public sealed class AiController(
    WorkflowService workflows,
    ApprovalService approvals,
    WorkflowRunner runner) : ControllerBase
{
    /// <summary>Starts an agent workflow for a ticket and returns immediately with its id.</summary>
    [HttpPost("workflows")]
    [ProducesResponseType(typeof(WorkflowDetailDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkflowDetailDto>> Start(StartWorkflowRequest request, CancellationToken ct)
    {
        var id = await workflows.StartAsync(request, ct);
        runner.StartInBackground(id);

        // 202: the work has been accepted and is running; poll the detail endpoint for progress.
        var detail = await workflows.GetAsync(id, ct);
        return AcceptedAtAction(nameof(GetWorkflow), new { id }, detail);
    }

    [HttpGet("workflows")]
    [ProducesResponseType(typeof(PagedResult<WorkflowListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<WorkflowListItemDto>>> ListWorkflows(
        [FromQuery] WorkflowQuery query, CancellationToken ct)
        => Ok(await workflows.QueryAsync(query, ct));

    /// <summary>
    /// The full auditable execution summary: plan, every agent step with timing, retries and
    /// validation result, every tool call, the approval decisions and the final outcome.
    /// </summary>
    [HttpGet("workflows/{id:int}")]
    [ProducesResponseType(typeof(WorkflowDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowDetailDto>> GetWorkflow(int id, CancellationToken ct)
        => Ok(await workflows.GetAsync(id, ct));

    /// <summary>Approval queue. Managers and administrators use this as their review feed.</summary>
    [HttpGet("approvals")]
    [Authorize(Roles = RoleNames.Staff)]
    [ProducesResponseType(typeof(PagedResult<ApprovalDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ApprovalDto>>> ListApprovals(
        [FromQuery] ApprovalQuery query, CancellationToken ct)
        => Ok(await approvals.QueryAsync(query, ct));

    [HttpGet("approvals/{id:int}")]
    [Authorize(Roles = RoleNames.Staff)]
    [ProducesResponseType(typeof(ApprovalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApprovalDto>> GetApproval(int id, CancellationToken ct)
        => Ok(await approvals.GetAsync(id, ct));

    /// <summary>
    /// The human-in-the-loop gate. Approve, reject or request revision.
    ///
    /// The role restriction is declared here AND re-checked inside ApprovalService, because the
    /// attribute alone would be the only barrier if this action were ever called from elsewhere.
    /// Only "Approved" causes anything to be executed.
    /// </summary>
    [HttpPost("approvals/{id:int}/decision")]
    [Authorize(Roles = RoleNames.ManagerOrAdmin)]
    [ProducesResponseType(typeof(ApprovalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApprovalDto>> Decide(int id, ApprovalDecisionRequest request, CancellationToken ct)
        => Ok(await approvals.DecideAsync(id, request, ct));

    /// <summary>
    /// The tool allow-list, for demonstration and observability. ExecuteApprovedAction appears with
    /// an empty agent list, because no agent is permitted to call it.
    /// </summary>
    [HttpGet("tools")]
    [Authorize(Roles = RoleNames.Staff)]
    [ProducesResponseType(typeof(IReadOnlyList<AgentToolInfoDto>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<AgentToolInfoDto>> Tools() => Ok(workflows.GetTools());
}
