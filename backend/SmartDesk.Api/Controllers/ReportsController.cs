using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDesk.Application.Dtos;
using SmartDesk.Application.Services;
using SmartDesk.Domain.Common;

namespace SmartDesk.Api.Controllers;

/// <summary>Component D - SLA, Escalation and Reporting (Student 4).</summary>
[ApiController]
[Route("api/reports")]
[Authorize]
[Produces("application/json")]
public sealed class ReportsController(ReportingService reporting) : ControllerBase
{
    /// <summary>Dashboard KPIs. Scoped by role: an employee sees only their own tickets.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardDto>> Dashboard(CancellationToken ct)
        => Ok(await reporting.GetDashboardAsync(ct));

    /// <summary>SLA on-track, at-risk and breached counts with a per-category breakdown.</summary>
    [HttpGet("sla")]
    [Authorize(Roles = RoleNames.ManagerOrAdmin)]
    [ProducesResponseType(typeof(SlaReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SlaReportDto>> Sla(CancellationToken ct)
        => Ok(await reporting.GetSlaReportAsync(ct));

    /// <summary>Resolution volume, average resolution time and breach rate per support agent.</summary>
    [HttpGet("agent-performance")]
    [Authorize(Roles = RoleNames.ManagerOrAdmin)]
    [ProducesResponseType(typeof(IReadOnlyList<AgentPerformanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AgentPerformanceDto>>> AgentPerformance(CancellationToken ct)
        => Ok(await reporting.GetAgentPerformanceAsync(ct));
}
