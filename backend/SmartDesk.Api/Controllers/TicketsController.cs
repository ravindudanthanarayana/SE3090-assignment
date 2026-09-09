using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDesk.Api.Infrastructure;
using SmartDesk.Application.Common;
using SmartDesk.Application.Dtos;
using SmartDesk.Application.Services;
using SmartDesk.Domain.Common;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Api.Controllers;

/// <summary>
/// Component A - Ticket Management (Student 1).
/// Also hosts the per-ticket endpoints owned by the other components, so the REST resource stays
/// coherent: assignment (B), article linking (C) and escalation (D) are all actions on a ticket.
/// </summary>
[ApiController]
[Route("api/tickets")]
[Authorize]
[Produces("application/json")]
public sealed class TicketsController(
    TicketService tickets,
    AssignmentService assignments,
    KnowledgeService knowledge,
    ReportingService reporting,
    WorkflowService workflows,
    WorkflowRunner runner) : ControllerBase
{
    // ---- Component A: CRUD + query -------------------------------------------------------

    /// <summary>Lists tickets with server-side search, filtering, sorting and pagination.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TicketListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TicketListItemDto>>> List([FromQuery] TicketQuery query, CancellationToken ct)
        => Ok(await tickets.QueryAsync(query, ct));

    /// <summary>Tickets that are at risk of, or already past, their SLA deadline.</summary>
    [HttpGet("sla-at-risk")]
    [Authorize(Roles = RoleNames.Staff)]
    [ProducesResponseType(typeof(IReadOnlyList<SlaAtRiskTicketDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SlaAtRiskTicketDto>>> AtRisk(CancellationToken ct)
        => Ok(await reporting.GetAtRiskAsync(ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TicketDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketDetailDto>> Get(int id, CancellationToken ct)
        => Ok(await tickets.GetAsync(id, ct));

    /// <summary>
    /// Creates a ticket and starts the Agentic AI workflow for it.
    /// The workflow runs in the background, so a slow or failing AI provider can never
    /// prevent a user from raising a ticket.
    /// </summary>
    [HttpPost]
    // Any authenticated user may raise a ticket: managers and support agents hit IT problems too,
    // and restricting this would only push them to ask someone else to file on their behalf.
    [ProducesResponseType(typeof(TicketDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TicketDetailDto>> Create(CreateTicketRequest request, CancellationToken ct)
    {
        var ticket = await tickets.CreateAsync(request, ct);

        var workflowId = await workflows.StartAsync(new StartWorkflowRequest { TicketId = ticket.Id }, ct);
        runner.StartInBackground(workflowId);

        return CreatedAtAction(nameof(Get), new { id = ticket.Id }, ticket);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TicketDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TicketDetailDto>> Update(int id, UpdateTicketRequest request, CancellationToken ct)
        => Ok(await tickets.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await tickets.DeleteAsync(id, ct);
        return NoContent();
    }

    // ---- Component A: business operation --------------------------------------------------

    /// <summary>
    /// Business operation: moves a ticket through the status machine. Invalid transitions are
    /// rejected with 409 rather than silently accepted.
    /// </summary>
    [HttpPost("{id:int}/status")]
    [Authorize(Roles = RoleNames.Staff)]
    [ProducesResponseType(typeof(TicketDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TicketDetailDto>> ChangeStatus(int id, ChangeStatusRequest request, CancellationToken ct)
        => Ok(await tickets.ChangeStatusAsync(id, request, ct));

    [HttpGet("{id:int}/comments")]
    [ProducesResponseType(typeof(IReadOnlyList<TicketCommentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TicketCommentDto>>> Comments(int id, CancellationToken ct)
        => Ok(await tickets.GetCommentsAsync(id, ct));

    [HttpPost("{id:int}/comments")]
    [ProducesResponseType(typeof(TicketCommentDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<TicketCommentDto>> AddComment(int id, CreateCommentRequest request, CancellationToken ct)
    {
        var comment = await tickets.AddCommentAsync(id, request, ct);
        return StatusCode(StatusCodes.Status201Created, comment);
    }

    /// <summary>Field-level change history for the ticket.</summary>
    [HttpGet("{id:int}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<TicketHistoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TicketHistoryDto>>> History(int id, CancellationToken ct)
        => Ok(await tickets.GetHistoryAsync(id, ct));

    /// <summary>Uploads a screenshot/photo for a ticket. Ownership rules are identical to ticket viewing.</summary>
    [HttpPost("{id:int}/attachments")]
    [ProducesResponseType(typeof(TicketAttachmentDto), StatusCodes.Status201Created)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<TicketAttachmentDto>> AddAttachment(int id, IFormFile file, CancellationToken ct)
    {
        if (file is null) throw new ValidationException("An image file is required.");
        await using var stream = file.OpenReadStream();
        var attachment = await tickets.AddAttachmentAsync(id, file.FileName, file.ContentType, stream, file.Length, ct);
        return StatusCode(StatusCodes.Status201Created, attachment);
    }

    [HttpGet("{id:int}/attachments")]
    [ProducesResponseType(typeof(IReadOnlyList<TicketAttachmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TicketAttachmentDto>>> Attachments(int id, CancellationToken ct)
        => Ok(await tickets.GetAttachmentsAsync(id, ct));

    [HttpGet("{id:int}/attachments/{attachmentId:int}/content")]
    public async Task<IActionResult> AttachmentContent(int id, int attachmentId, CancellationToken ct)
    {
        var file = await tickets.GetAttachmentContentAsync(id, attachmentId, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    // ---- Component B: assignment ----------------------------------------------------------

    /// <summary>Business operation: assign or reassign the ticket. Managers and administrators only.</summary>
    [HttpPost("{id:int}/assign")]
    [Authorize(Roles = RoleNames.ManagerOrAdmin)]
    [ProducesResponseType(typeof(TicketAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TicketAssignmentDto>> Assign(int id, AssignTicketRequest request, CancellationToken ct)
        => Ok(await assignments.AssignAsync(id, request, AssignmentSource.Manual, ct));

    [HttpGet("{id:int}/assignments")]
    [Authorize(Roles = RoleNames.Staff)]
    [ProducesResponseType(typeof(IReadOnlyList<TicketAssignmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TicketAssignmentDto>>> Assignments(int id, CancellationToken ct)
        => Ok(await assignments.GetAssignmentHistoryAsync(id, ct));

    // ---- Component C: knowledge -----------------------------------------------------------

    /// <summary>Knowledge articles ranked for this specific ticket.</summary>
    [HttpGet("{id:int}/relevant-articles")]
    [Authorize(Roles = RoleNames.Staff)]
    [ProducesResponseType(typeof(IReadOnlyList<RelevantArticleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RelevantArticleDto>>> RelevantArticles(int id, CancellationToken ct)
        => Ok(await knowledge.GetRelevantAsync(id, 5, ct));

    /// <summary>Business operation: link an article to the ticket as a suggested solution.</summary>
    [HttpPost("{id:int}/link-article")]
    [Authorize(Roles = RoleNames.Staff)]
    [ProducesResponseType(typeof(LinkedArticleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LinkedArticleDto>> LinkArticle(int id, LinkArticleRequest request, CancellationToken ct)
    {
        var link = await knowledge.LinkToTicketAsync(id, request, ct);
        return StatusCode(StatusCodes.Status201Created, link);
    }

    // ---- Component D: escalation ----------------------------------------------------------

    /// <summary>
    /// Business operation: manual escalation by a manager. An AI-proposed escalation does not use
    /// this endpoint - it must be approved first, and is then executed transactionally by the
    /// approval service.
    /// </summary>
    [HttpPost("{id:int}/escalate")]
    [Authorize(Roles = RoleNames.ManagerOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Escalate(int id, EscalateTicketRequest request, CancellationToken ct)
    {
        await reporting.EscalateAsync(id, request, ct);
        return NoContent();
    }
}
