using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDesk.Application.Dtos;
using SmartDesk.Application.Services;
using SmartDesk.Domain.Common;

namespace SmartDesk.Api.Controllers;

/// <summary>Component B - Support Agent and Assignment Management (Student 2).</summary>
[ApiController]
[Route("api")]
[Authorize]
[Produces("application/json")]
public sealed class SupportAgentsController(AssignmentService assignments) : ControllerBase
{
    /// <summary>Active support agents with their skills and live workload.</summary>
    [HttpGet("support-agents")]
    [Authorize(Roles = RoleNames.Staff)]
    [ProducesResponseType(typeof(IReadOnlyList<SupportAgentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SupportAgentDto>>> List(CancellationToken ct)
        => Ok(await assignments.GetSupportAgentsAsync(ct));

    [HttpGet("support-agents/{id:int}/skills")]
    [ProducesResponseType(typeof(IReadOnlyList<AgentSkillDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AgentSkillDto>>> Skills(int id, CancellationToken ct)
        => Ok(await assignments.GetSkillsAsync(id, ct));

    /// <summary>Creates or updates an agent's proficiency in one category.</summary>
    [HttpPut("support-agents/{id:int}/skills")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(typeof(AgentSkillDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AgentSkillDto>> UpsertSkill(int id, UpsertSkillRequest request, CancellationToken ct)
        => Ok(await assignments.UpsertSkillAsync(id, request, ct));

    [HttpDelete("support-agents/{id:int}/skills/{skillId:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSkill(int id, int skillId, CancellationToken ct)
    {
        await assignments.DeleteSkillAsync(id, skillId, ct);
        return NoContent();
    }

    /// <summary>Per-agent open, at-risk and breached ticket counts.</summary>
    [HttpGet("assignments/workload")]
    [Authorize(Roles = RoleNames.Staff)]
    [ProducesResponseType(typeof(IReadOnlyList<AgentWorkloadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AgentWorkloadDto>>> Workload(CancellationToken ct)
        => Ok(await assignments.GetWorkloadAsync(ct));

    /// <summary>
    /// Business operation: the deterministic skill-versus-workload ranking for one ticket.
    /// The Assignment agent's tool runs the same scorer, so the AI and the manager always agree.
    /// </summary>
    [HttpGet("assignments/recommendation/{ticketId:int}")]
    [Authorize(Roles = RoleNames.ManagerOrAdmin)]
    [ProducesResponseType(typeof(AssignmentRecommendationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignmentRecommendationDto>> Recommend(int ticketId, CancellationToken ct)
        => Ok(await assignments.RecommendAsync(ticketId, ct));
}
